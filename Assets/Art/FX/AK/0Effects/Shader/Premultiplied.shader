Shader "EGA/Particles/Premultiplied"
{
	Properties
	{
		_InvFade ("Soft Particles Factor", Range(0.01,6.0)) = 1.0
		_MainTex("MainTex", 2D) = "white" {}
		_Noise("Noise", 2D) = "white" {}
		_SpeedMainTexUVNoiseZW("Speed MainTex U/V + Noise Z/W", Vector) = (0,0,0,0)
		_Emission("Emission", Float) = 2
		_Color("Color", Color) = (0.5,0.5,0.5,1)
		_Mask("Mask", 2D) = "white" {}
		_Opacity("Opacity", Range( 0 , 1)) = 1
		[Toggle]_Usecenterglow("Use center glow", Float) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
	}


	Category 
	{
		SubShader
		{
			Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
			Blend One OneMinusSrcAlpha
			ColorMask RGB
			Cull Off
			Lighting Off 
			ZWrite Off
			ZTest LEqual
			
			Pass {
			
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 2.0
				#pragma multi_compile_particles
				#pragma multi_compile_fog
				#include "UnityShaderVariables.cginc"


				#include "UnityCG.cginc"

				struct appdata_t 
				{
					float4 vertex : POSITION;
					fixed4 color : COLOR;
					float4 texcoord : TEXCOORD0;
					UNITY_VERTEX_INPUT_INSTANCE_ID
					
				};

				struct v2f 
				{
					float4 vertex : SV_POSITION;
					fixed4 color : COLOR;
					float4 texcoord : TEXCOORD0;
					UNITY_FOG_COORDS(1)
					#ifdef SOFTPARTICLES_ON
					float4 projPos : TEXCOORD2;
					#endif
					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
					
				};
				
				
				#if UNITY_VERSION >= 560
				UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
				#else
				uniform sampler2D_float _CameraDepthTexture;
				#endif

				//Don't delete this comment
				// uniform sampler2D_float _CameraDepthTexture;

				uniform sampler2D _MainTex;
				uniform fixed4 _TintColor;
				uniform float4 _MainTex_ST;
				uniform float _InvFade;
				uniform float _Usecenterglow;
				uniform float4 _SpeedMainTexUVNoiseZW;
				uniform sampler2D _Noise;
				uniform float4 _Noise_ST;
				uniform float4 _Color;
				uniform sampler2D _Mask;
				uniform float4 _Mask_ST;
				uniform float _Emission;
				uniform float _Opacity;

				v2f vert ( appdata_t v  )
				{
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
					UNITY_TRANSFER_INSTANCE_ID(v, o);
					

					v.vertex.xyz +=  float3( 0, 0, 0 ) ;
					o.vertex = UnityObjectToClipPos(v.vertex);
					#ifdef SOFTPARTICLES_ON
						o.projPos = ComputeScreenPos (o.vertex);
						COMPUTE_EYEDEPTH(o.projPos.z);
					#endif
					o.color = v.color;
					o.texcoord = v.texcoord;
					UNITY_TRANSFER_FOG(o,o.vertex);
					return o;
				}

				fixed4 frag ( v2f i  ) : SV_Target
				{
					#ifdef SOFTPARTICLES_ON
						float sceneZ = LinearEyeDepth (SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
						float partZ = i.projPos.z;
						float fade = saturate (_InvFade * (sceneZ-partZ));
						i.color.a *= fade;
					#endif

					float2 appendResult21 = (float2(_SpeedMainTexUVNoiseZW.x , _SpeedMainTexUVNoiseZW.y));
					float3 uv0_MainTex = i.texcoord.xyz;
					uv0_MainTex.xy = i.texcoord.xyz.xy * _MainTex_ST.xy + _MainTex_ST.zw;
					float4 tex2DNode13 = tex2D( _MainTex, ( ( appendResult21 * _Time.y ) + (uv0_MainTex).xy ) );
					float2 uv0_Noise = i.texcoord.xy * _Noise_ST.xy + _Noise_ST.zw;
					float2 appendResult22 = (float2(_SpeedMainTexUVNoiseZW.z , _SpeedMainTexUVNoiseZW.w));
					float4 tex2DNode14 = tex2D( _Noise, ( uv0_Noise + ( _Time.y * appendResult22 ) ) );
					float4 temp_output_30_0 = ( tex2DNode13 * tex2DNode14 * _Color * i.color );
					float2 uv_Mask = i.texcoord.xy * _Mask_ST.xy + _Mask_ST.zw;
					float4 tex2DNode33 = tex2D( _Mask, uv_Mask );
					float4 temp_cast_0 = ((1.0 + (uv0_MainTex.z - 0.0) * (0.0 - 1.0) / (1.0 - 0.0))).xxxx;
					float4 clampResult38 = clamp( ( tex2DNode33 - temp_cast_0 ) , float4( 0,0,0,0 ) , float4( 1,1,1,1 ) );
					float4 clampResult40 = clamp( ( tex2DNode33 * clampResult38 ) , float4( 0,0,0,0 ) , float4( 1,1,1,1 ) );
					float4 appendResult87 = (float4(( (lerp(temp_output_30_0,( temp_output_30_0 * clampResult40 ),_Usecenterglow)).rgb * _Emission ) , ( tex2DNode13.a * tex2DNode14.a * _Color.a * i.color.a * _Opacity )));
					

					fixed4 col = appendResult87;
					UNITY_APPLY_FOG(i.fogCoord, col);
					return col;
				}
				ENDCG 
			}
		}	
	}
	
	
	
}
/*ASEBEGIN
Version=16700
563;92;1003;655;2364.111;620.0943;3.290332;True;False
Node;AmplifyShaderEditor.TextureCoordinatesNode;29;-2522.62,71.46032;Float;False;0;13;3;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector4Node;15;-3103.802,24.82627;Float;False;Property;_SpeedMainTexUVNoiseZW;Speed MainTex U/V + Noise Z/W;2;0;Create;True;0;0;False;0;0,0,0,0;0,0,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TFHCRemapNode;36;-2078.541,865.2081;Float;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;1;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;33;-2267.097,648.0693;Float;True;Property;_Mask;Mask;6;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TimeNode;17;-2772.28,53.38881;Float;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;22;-2739.856,185.038;Float;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;21;-2742.397,-37.24139;Float;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ComponentMaskNode;59;-2264.626,43.66348;Float;False;True;True;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;89;-2261.242,127.7358;Float;False;0;14;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;23;-2523.281,230.8774;Float;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;37;-1843.933,836.1506;Float;True;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;24;-2525.724,-35.90024;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ClampOpNode;38;-1606.358,860.2141;Float;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;1,1,1,1;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;27;-2028.065,228.7832;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;26;-2051.538,-40.88631;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;13;-1803.192,-66.2159;Float;True;Property;_MainTex;MainTex;0;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.VertexColorNode;32;-1670.612,486.0577;Float;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;14;-1804.579,119.2214;Float;True;Property;_Noise;Noise;1;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;31;-1728.612,316.0578;Float;False;Property;_Color;Color;4;0;Create;True;0;0;False;0;0.5,0.5,0.5,1;0.5,0.5,0.5,1;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;39;-1344.594,664.8295;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ClampOpNode;40;-1177.643,664.8295;Float;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;1,1,1,1;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;30;-700.5079,93.06104;Float;False;4;4;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;41;-442.1707,225.2773;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ToggleSwitchNode;90;-216.4182,111.5104;Float;False;Property;_Usecenterglow;Use center glow;8;0;Create;True;0;0;False;0;0;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;52;189.0134,237.9857;Float;False;Property;_Emission;Emission;3;0;Create;True;0;0;False;0;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;85;111.6254,124.0073;Float;False;True;True;True;False;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;62;-330.3089,576.7908;Float;False;Property;_Opacity;Opacity;7;0;Create;True;0;0;False;0;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;51;372.5463,162.5181;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;88;-41.1605,425.951;Float;False;5;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;87;576.6387,178.2277;Float;False;FLOAT4;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;68;760.3594,176.5524;Float;False;True;2;Float;;0;11;EGA/Particles/Premultiplied;0b6a9f8b4f707c74ca64c0be8e590de0;True;SubShader 0 Pass 0;0;0;SubShader 0 Pass 0;2;True;3;1;False;-1;10;False;-1;0;1;False;-1;0;False;-1;False;False;True;2;False;-1;True;True;True;True;False;0;False;-1;False;True;2;False;-1;True;3;False;-1;False;True;4;Queue=Transparent=Queue=0;IgnoreProjector=True;RenderType=Transparent=RenderType;PreviewType=Plane;False;0;False;False;False;False;False;False;False;False;False;False;True;0;0;;0;0;Standard;0;0;1;True;False;2;0;FLOAT4;0,0,0,0;False;1;FLOAT3;0,0,0;False;0
WireConnection;36;0;29;3
WireConnection;22;0;15;3
WireConnection;22;1;15;4
WireConnection;21;0;15;1
WireConnection;21;1;15;2
WireConnection;59;0;29;0
WireConnection;23;0;17;2
WireConnection;23;1;22;0
WireConnection;37;0;33;0
WireConnection;37;1;36;0
WireConnection;24;0;21;0
WireConnection;24;1;17;2
WireConnection;38;0;37;0
WireConnection;27;0;89;0
WireConnection;27;1;23;0
WireConnection;26;0;24;0
WireConnection;26;1;59;0
WireConnection;13;1;26;0
WireConnection;14;1;27;0
WireConnection;39;0;33;0
WireConnection;39;1;38;0
WireConnection;40;0;39;0
WireConnection;30;0;13;0
WireConnection;30;1;14;0
WireConnection;30;2;31;0
WireConnection;30;3;32;0
WireConnection;41;0;30;0
WireConnection;41;1;40;0
WireConnection;90;0;30;0
WireConnection;90;1;41;0
WireConnection;85;0;90;0
WireConnection;51;0;85;0
WireConnection;51;1;52;0
WireConnection;88;0;13;4
WireConnection;88;1;14;4
WireConnection;88;2;31;4
WireConnection;88;3;32;4
WireConnection;88;4;62;0
WireConnection;87;0;51;0
WireConnection;87;3;88;0
WireConnection;68;0;87;0
ASEEND*/
//CHKSM=ADBD9B0DDE1E2F0273AF884745146645BB47969C