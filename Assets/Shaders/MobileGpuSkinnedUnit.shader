Shader "DefenseGame/Mobile GPU Skinned Unit"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        _MainTex("Legacy Base Map", 2D) = "white" {}
        [HideInInspector] _UseLegacyMainTex("Use Legacy Main Texture", Float) = 0
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _Color("Legacy Color", Color) = (1, 1, 1, 1)
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Range(0, 2)) = 1
        _EmissionMap("Emission Map", 2D) = "black" {}
        [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 0)
        _Metallic("Metallic", Range(0, 1)) = 0
        _Smoothness("Smoothness", Range(0, 1)) = 0.2
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Alpha Clip", Float) = 0
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "GpuSkinnedForward"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma shader_feature_local_fragment _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                float4 boneWeights : BLENDWEIGHTS0;
                uint4 boneIndices : BLENDINDICES0;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uvBase : TEXCOORD0;
                float2 uvLegacy : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                half3 normalWS : TEXCOORD3;
                half4 tangentWS : TEXCOORD4;
                nointerpolation uint instanceID : TEXCOORD5;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);
            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _MainTex_ST;
                half4 _BaseColor;
                half4 _Color;
                half4 _EmissionColor;
                half _UseLegacyMainTex;
                half _BumpScale;
                half _Metallic;
                half _Smoothness;
                half _AlphaClip;
                half _Cutoff;
            CBUFFER_END

            StructuredBuffer<float4x4> _GpuSkinBones;
            StructuredBuffer<float4x4> _GpuRootMatrices;
            StructuredBuffer<float4> _GpuSkinColors;
            StructuredBuffer<float4> _GpuSkinFlash;
            int _GpuBonesPerInstance;

            float4x4 LoadSkinMatrix(uint instanceID, uint4 indices, float4 weights)
            {
                uint offset = instanceID * (uint)_GpuBonesPerInstance;
                float4x4 skin =
                    _GpuSkinBones[offset + indices.x] * weights.x +
                    _GpuSkinBones[offset + indices.y] * weights.y +
                    _GpuSkinBones[offset + indices.z] * weights.z +
                    _GpuSkinBones[offset + indices.w] * weights.w;

                // Keep the per-instance root buffer live for drivers that require
                // all bound StructuredBuffers to participate in the vertex stage.
                skin[3][3] += _GpuRootMatrices[instanceID][3][3] * 0.0;
                return skin;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float4x4 skin = LoadSkinMatrix(input.instanceID, input.boneIndices, input.boneWeights);
                float3 positionWS = mul(skin, input.positionOS).xyz;
                float3 normalWS = normalize(mul((float3x3)skin, input.normalOS));
                float3 tangentWS = normalize(mul((float3x3)skin, input.tangentOS.xyz));

                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.normalWS = normalWS;
                output.tangentWS = half4(tangentWS, input.tangentOS.w);
                output.uvBase = TRANSFORM_TEX(input.uv, _BaseMap);
                output.uvLegacy = TRANSFORM_TEX(input.uv, _MainTex);
                output.instanceID = input.instanceID;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uvBase);
                half4 legacySample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uvLegacy);
                half4 surfaceSample = lerp(baseSample, legacySample, saturate(_UseLegacyMainTex));
                half4 tint = (half4)_GpuSkinColors[input.instanceID];
                half4 albedoAlpha = surfaceSample * _BaseColor * _Color * tint;

                #if defined(_ALPHATEST_ON)
                    clip(albedoAlpha.a - _Cutoff);
                #endif

                half3 tangentNormal = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uvBase),
                    _BumpScale);
                half3 bitangentWS = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                half3 normalWS = normalize(
                    tangentNormal.x * input.tangentWS.xyz +
                    tangentNormal.y * bitangentWS +
                    tangentNormal.z * input.normalWS);

                Light mainLight = GetMainLight();
                half ndotl = saturate(dot(normalWS, mainLight.direction));
                half3 diffuse = SampleSH(normalWS) + mainLight.color * ndotl;
                half3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uvBase).rgb
                    * _EmissionColor.rgb;
                half4 flash = (half4)_GpuSkinFlash[input.instanceID];
                half3 color = albedoAlpha.rgb * diffuse + emission;
                color = lerp(color, flash.rgb, saturate(flash.a));
                return half4(color, albedoAlpha.a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
