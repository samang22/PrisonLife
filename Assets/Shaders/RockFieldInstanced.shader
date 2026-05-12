Shader "PrisonLife/Rendering/RockFieldInstanced"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor]   _BaseColor("Color", Color) = (1,1,1,1)
        [HideInInspector] _BatchInstanceOffset("Batch Base", Int) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        // Forward
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            ZWrite On ZTest LEqual Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                int    _BatchInstanceOffset;
            CBUFFER_END

            StructuredBuffer<float> _InstanceActive;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv0        : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                Varyings o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                #ifdef UNITY_INSTANCING_ENABLED
                    uint gid = (uint)_BatchInstanceOffset + (uint)unity_InstanceID;
                #else
                    uint gid = (uint)_BatchInstanceOffset;
                #endif

                float active = _InstanceActive[gid];
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                if (active < 0.5)
                    posWS = float3(0, -1e9, 0);

                o.positionCS = TransformWorldToHClip(posWS);
                o.positionWS = posWS;
                o.normalWS   = TransformObjectToWorldNormal(v.normalOS);
                o.uv         = TRANSFORM_TEX(v.uv0, _BaseMap);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb
                             * _BaseColor.rgb;
                float4 sc = TransformWorldToShadowCoord(i.positionWS);
                Light L = GetMainLight(sc, i.positionWS, i.positionCS);
                half  NdL    = saturate(dot(n, L.direction));
                half3 direct = albedo * L.color * NdL * L.shadowAttenuation * L.distanceAttenuation;
                half3 amb    = albedo * half3(0.15h, 0.16h, 0.18h);
                return half4(direct + amb, 1.0h);
            }
            ENDHLSL
        }

        // Shadow
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0 Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex   shadowVert
            #pragma fragment shadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                int    _BatchInstanceOffset;
            CBUFFER_END

            StructuredBuffer<float> _InstanceActive;

            struct AttributesS
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VaryingsS
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            VaryingsS shadowVert(AttributesS v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                VaryingsS o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                #ifdef UNITY_INSTANCING_ENABLED
                    uint gid = (uint)_BatchInstanceOffset + (uint)unity_InstanceID;
                #else
                    uint gid = (uint)_BatchInstanceOffset;
                #endif

                float active = _InstanceActive[gid];

                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 nWS   = TransformObjectToWorldNormal(v.normalOS);

                if (active < 0.5)
                {
                    o.positionCS = float4(0, 0, -2, 1);
                    return o;
                }

            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 ldir = normalize(_LightPosition - posWS);
            #else
                float3 ldir = normalize(_LightDirection);
            #endif

                float4 pCS = TransformWorldToHClip(ApplyShadowBias(posWS, nWS, ldir));
            #if UNITY_REVERSED_Z
                pCS.z = min(pCS.z, pCS.w * UNITY_NEAR_CLIP_VALUE);
            #else
                pCS.z = max(pCS.z, pCS.w * UNITY_NEAR_CLIP_VALUE);
            #endif
                o.positionCS = pCS;
                return o;
            }

            half4 shadowFrag(VaryingsS i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
    FallBack Off
}