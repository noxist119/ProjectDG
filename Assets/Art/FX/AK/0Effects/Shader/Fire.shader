Shader "EGA/Particles/Fire"
{
	Properties
	{
		_InvFade ("Soft Particles Factor", Range(0.01,3.0)) = 1.0
		_Tex1("Tex1", 2D) = "white" {}
		_Tex2("Tex2", 2D) = "white" {}
		_Mask("Mask", 2D) = "white" {}
		_SpeedTex1("Speed Tex1", Vector) = (0,0,0,0)
		_SpeedTex2XYEmission("Speed Tex2 XY / Emission", Vector) = (0,0,0,0)
		_Color2("Color 2", Color) = (1,0,0,1)
		_Color1("Color 1", Color) = (1,0.5423229,0,1)
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
	}


	Category 
	{
		SubShader
		{
			Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
			Blend SrcAlpha OneMinusSrcAlpha
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
				uniform float4 _MainTex_ST;
				uniform float _InvFade;
				uniform float4 _SpeedTex2XYEmission;
				uniform float4 _Color1;
				uniform float4 _Color2;
				uniform sampler2D _Tex1;
				uniform float4 _SpeedTex1;
				uniform float4 _Tex1_ST;
				uniform sampler2D _Tex2;
				uniform float4 _Tex2_ST;
				uniform sampler2D _Mask;
				uniform float4 _Mask_ST;

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

					float Emission39 = _SpeedTex2XYEmission.z;
					float2 appendResult16 = (float2(_SpeedTex1.x , _SpeedTex1.y));
					float2 uv0_Tex1 = i.texcoord.xy * _Tex1_ST.xy + _Tex1_ST.zw;
					float2 panner7 = ( 1.0 * _Time.y * appendResult16 + uv0_Tex1);
					float2 appendResult14 = (float2(_SpeedTex1.z , _SpeedTex1.w));
					float2 panner8 = ( 1.0 * _Time.y * appendResult14 + uv0_Tex1);
					float4 tex2DNode4 = tex2D( _Tex1, panner8 );
					float2 appendResult21 = (float2(_SpeedTex2XYEmission.x , _SpeedTex2XYEmission.y));
					float2 uv0_Tex2 = i.texcoord.xy * _Tex2_ST.xy + _Tex2_ST.zw;
					float2 panner20 = ( 1.0 * _Time.y * appendResult21 + uv0_Tex2);
					float4 tex2DNode5 = tex2D( _Tex2, panner20 );
					float2 uv_Mask = i.texcoord.xy * _Mask_ST.xy + _Mask_ST.zw;
					float4 tex2DNode6 = tex2D( _Mask, uv_Mask );
					float temp_output_27_0 = ( ( ( ( ( tex2D( _Tex1, panner7 ).r + tex2DNode4.r ) * tex2DNode4.r * tex2DNode5.g ) + ( tex2DNode6.b * 0.5 ) ) * tex2DNode5.g ) * tex2DNode6.b );
					float4 lerpResult33 = lerp( _Color1 , _Color2 , temp_output_27_0);
					float4 appendResult35 = (float4((( Emission39 * lerpResult33 * i.color )).rgb , ( _Color1.a * _Color2.a * temp_output_27_0 * i.color.a )));
					

					fixed4 col = appendResult35;
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
563;92;1003;655;1603.413;524.0717;2.010815;True;False
Node;AmplifyShaderEditor.Vector4Node;13;-2850.329,30.79891;Float;False;Property;_SpeedTex1;Speed Tex1;3;0;Create;True;0;0;False;0;0,0,0,0;0,0,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;10;-2534.298,-92.18343;Float;False;0;17;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;16;-2456.836,22.10848;Float;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;14;-2455.048,144.7154;Float;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector4Node;19;-2811.947,399.0959;Float;False;Property;_SpeedTex2XYEmission;Speed Tex2 XY / Emission;4;0;Create;True;0;0;False;0;0,0,0,0;0,0,0,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PannerNode;8;-2279.111,77.22112;Float;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;7;-2286.721,-58.46526;Float;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexturePropertyNode;17;-2302.899,-264.0622;Float;True;Property;_Tex1;Tex1;0;0;Create;True;0;0;False;0;b40e71d48597f2f4bbb7f938908725e3;b40e71d48597f2f4bbb7f938908725e3;False;white;Auto;Texture2D;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.DynamicAppendNode;21;-2457.837,428.2516;Float;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;22;-2535.299,313.9597;Float;False;0;5;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;18;-1942.855,-241.3017;Float;True;Property;_TextureSample0;Texture Sample 0;0;0;Create;True;0;0;False;0;b40e71d48597f2f4bbb7f938908725e3;b40e71d48597f2f4bbb7f938908725e3;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;4;-1942.498,-60.04169;Float;True;Property;_Tex0;Tex0;0;0;Create;True;0;0;False;0;b40e71d48597f2f4bbb7f938908725e3;b40e71d48597f2f4bbb7f938908725e3;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PannerNode;20;-2287.722,347.6778;Float;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;24;-1567.076,-149.5738;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;5;-1940.967,122.1309;Float;True;Property;_Tex2;Tex2;1;0;Create;True;0;0;False;0;1666b2d68bbffc64cac23d5a76d0788f;1666b2d68bbffc64cac23d5a76d0788f;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;6;-1940.419,303.0058;Float;True;Property;_Mask;Mask;2;0;Create;True;0;0;False;0;d5b30f5e6a5dd1343a28d52c0f03ffd2;d5b30f5e6a5dd1343a28d52c0f03ffd2;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;32;-1445.526,66.21613;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;23;-1417.404,-90.89656;Float;False;3;3;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;26;-1257.95,-8.849566;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;43;-1106.04,71.12432;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;34;-847.2845,-269.326;Float;False;Property;_Color1;Color 1;6;0;Create;True;0;0;False;0;1,0.5423229,0,1;1,0.5423229,0,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;27;-856.3154,126.7215;Float;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;37;-837.8048,-79.7439;Float;False;Property;_Color2;Color 2;5;0;Create;True;0;0;False;0;1,0,0,1;1,0,0,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;39;-2476.219,524.1384;Float;False;Emission;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;46;-835.8689,363.9673;Float;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;33;-546.3663,-82.49611;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;40;-554.0267,-173.6586;Float;False;39;Emission;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;47;-420.7433,289.7996;Float;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;38;-336.4936,-99.03915;Float;False;3;3;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;45;-535.0452,121.6697;Float;False;4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;36;-157.4211,-64.95212;Float;False;True;True;True;False;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.DynamicAppendNode;35;78.50879,-19.70944;Float;False;FLOAT4;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;1;332.121,55.53053;Float;False;True;2;Float;;0;11;EGA/Particles/Fire;0b6a9f8b4f707c74ca64c0be8e590de0;True;SubShader 0 Pass 0;0;0;SubShader 0 Pass 0;2;True;2;5;False;-1;10;False;-1;0;1;False;-1;0;False;-1;False;False;True;2;False;-1;True;True;True;True;False;0;False;-1;False;True;2;False;-1;True;3;False;-1;False;True;4;Queue=Transparent=Queue=0;IgnoreProjector=True;RenderType=Transparent=RenderType;PreviewType=Plane;False;0;False;False;False;False;False;False;False;False;False;False;True;0;0;;0;0;Standard;0;0;1;True;False;2;0;FLOAT4;0,0,0,0;False;1;FLOAT3;0,0,0;False;0
WireConnection;16;0;13;1
WireConnection;16;1;13;2
WireConnection;14;0;13;3
WireConnection;14;1;13;4
WireConnection;8;0;10;0
WireConnection;8;2;14;0
WireConnection;7;0;10;0
WireConnection;7;2;16;0
WireConnection;21;0;19;1
WireConnection;21;1;19;2
WireConnection;18;0;17;0
WireConnection;18;1;7;0
WireConnection;4;0;17;0
WireConnection;4;1;8;0
WireConnection;20;0;22;0
WireConnection;20;2;21;0
WireConnection;24;0;18;1
WireConnection;24;1;4;1
WireConnection;5;1;20;0
WireConnection;32;0;6;3
WireConnection;23;0;24;0
WireConnection;23;1;4;1
WireConnection;23;2;5;2
WireConnection;26;0;23;0
WireConnection;26;1;32;0
WireConnection;43;0;26;0
WireConnection;43;1;5;2
WireConnection;27;0;43;0
WireConnection;27;1;6;3
WireConnection;39;0;19;3
WireConnection;33;0;34;0
WireConnection;33;1;37;0
WireConnection;33;2;27;0
WireConnection;47;0;46;0
WireConnection;38;0;40;0
WireConnection;38;1;33;0
WireConnection;38;2;47;0
WireConnection;45;0;34;4
WireConnection;45;1;37;4
WireConnection;45;2;27;0
WireConnection;45;3;46;4
WireConnection;36;0;38;0
WireConnection;35;0;36;0
WireConnection;35;3;45;0
WireConnection;1;0;35;0
ASEEND*/
//CHKSM=0DDF622DA9ED0534338FAD78E448E3DABB08D985