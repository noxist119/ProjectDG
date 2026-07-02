// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Shader_02/FX/Shader_01" 
{
    Properties 
    {
    	[Header (COLOR)]
        _MainTex ("MainTex", 2D) = "white" {}
        [HDR]_TintColor ("Color", Color) = (1,1,1,1)
        _ColorScale ("Color Scale", Float) = 1
        [Toggle(MainDistort)] _MainDistort ("Distortion ?", int) = 0
        [Toggle(UsingAlphaChannel)] _UsingAlphaChannel ("Use Alpha Channel ?", int) = 0


        [Header (MASK)]
        [KeywordEnum(None, Separate, Combine)] _Alphac ("Alpha Mode", Float) = 0
        _MaskTex ("MaskTex", 2D) = "white" {}
        [Toggle(MaskDistort)] _MaskDistort ("Distortion ?", int) = 0
		[Toggle(Erosion)] _Erosion("Erosion ?", int) = 0
        _Opacity ("Opacity", Range(0,1)) = 1
        

        [Header (FRESNEL)]
		[Toggle(Fresnel)] _Fresnel ("Fresnel ?", int) = 0
		_FresnelPower ("Fresnel Power", Range(0,50)) = 1
		_FresnelIntensity ("Fresnel Intensity", Range(0,50)) = 1
		[Toggle(InvertFresnel)] _InvertFresnel ("Invert ?", int) = 0


        [Header (DISTORTION)]
        _DistortTex ("Distortion Texture (first)", 2D) = "white" {}
        _DistortionScale ("Distortion Scale", Float) = 0.3
        _DistortionSpeedX ("USpeed", Float) = 1
        _DistortionSpeedY ("VSpeed", Float) = 1




        [Header (RENDERING OPTION)]
		[Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull mode", int) = 2
		[Enum(Add,1, Alpha,10)] _Dest ("Blend mode", int) = 1
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4 //"LessEqual"

		
    }
    
    SubShader {
        Tags 
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }
        Pass {
            
            Blend SrcAlpha [_Dest]
            Lighting Off
            zwrite off
			Ztest [_ZTest]
            Cull [_Cull]
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature MainDistort
            #pragma shader_feature UsingAlphaChannel
			#pragma shader_feature Erosion
            #pragma shader_feature MaskDistort
            #pragma shader_feature Fresnel
	        #pragma shader_feature InvertFresnel
            #pragma multi_compile _ALPHAC_NONE _ALPHAC_SEPARATE _ALPHAC_COMBINE

            #include "UnityCG.cginc"

            sampler2D _MainTex; 
            sampler2D _MaskTex;
            sampler2D _DistortTex;
            sampler2D _DistortTex02;

            half4 _MaskTex_ST;
            half4 _MainTex_ST;
            half4 _DistortTex_ST;
            half4 _DistortTex02_ST;
            half4 _TintColor;

            half _ColorScale;
            half _Opacity;
            half _DistortionScale;
            half _DistortionSpeedX;
            half _DistortionSpeedY;

            half _FresnelPower;
            half _FresnelIntensity;
            
            struct a2v
            {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
                float4 vertexColor : COLOR;
                float3 normal : NORMAL;
            };
            
            struct v2f 
            {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 vertexColor : COLOR;
                float3 viewDir : TEXCOORD1;
                float3 normal : TEXCOORD2;
                float fresnel : TEXCOORD3;
            };
            
            v2f vert (a2v v) {
                v2f o = (v2f)0;
                o.uv0 = v.texcoord0;
                o.vertexColor = v.vertexColor;
                o.pos = UnityObjectToClipPos(v.vertex );

                #if Fresnel
	                o.normal = v.normal;
	                o.viewDir = normalize(ObjSpaceViewDir(v.vertex));
				    o.fresnel = saturate(pow(abs(dot(o.viewDir, o.normal)), _FresnelPower) * _FresnelIntensity);
					    #if InvertFresnel
					    o.fresnel = saturate(pow(1 - abs(dot(o.viewDir, o.normal)), _FresnelPower) * _FresnelIntensity);
					    #endif
				    #endif

                return o;
            }
            
            fixed4 frag(v2f i) : COLOR 
            {
            	#if MainDistort || MaskDistort
            	

            	float2 reuv = float2(i.uv0.r + (_DistortionSpeedX*_Time.x), i.uv0.g + (_DistortionSpeedY*_Time.x));
				fixed4 DistTex = tex2D(_DistortTex,TRANSFORM_TEX(reuv, _DistortTex));

				DistTex.rg = DistTex.rg*2.0-1.0;   

				float2 combUV = float2(DistTex.rg * _DistortionScale);

				             	
					

         		float2 distuv = (i.uv0+ combUV);	
             	#endif
             	          	

             	fixed4 MainTex = tex2D(_MainTex,TRANSFORM_TEX(i.uv0, _MainTex)); 

            	#if MainDistort
                MainTex = tex2D(_MainTex,TRANSFORM_TEX(distuv, _MainTex));
           		#endif

           		fixed AlphaSource = MainTex.r;
               		#if UsingAlphaChannel
               		AlphaSource = MainTex.a;
               		#endif


				fixed4  MaskTex = tex2D(_MaskTex,TRANSFORM_TEX(i.uv0, _MaskTex));
            	#if MaskDistort 
         		MaskTex = tex2D(_MaskTex,TRANSFORM_TEX(distuv, _MaskTex));
         		#endif

           		#if _ALPHAC_SEPARATE
               		AlphaSource = MaskTex.r;
               		#elif _ALPHAC_COMBINE
               		AlphaSource *= MaskTex.r;
         		#endif

         		#if Fresnel
             		AlphaSource *= i.fresnel;
             	#endif

                fixed3 emissive = (MainTex.rgb * i.vertexColor.rgb );
                
				fixed Alpha = AlphaSource * _Opacity * i.vertexColor.a;
				#if Erosion
					Alpha = saturate(AlphaSource - (1.0 - _Opacity * i.vertexColor.a));
				#endif
                

                fixed3 finalColor = emissive * _ColorScale *_TintColor.rgb;
                fixed4 finalRGBA = fixed4(finalColor,Alpha);

                return finalRGBA;
            }
            ENDCG
        }
    }

}