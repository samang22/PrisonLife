// RockFieldInstancedRenderer: URP 메인라이트 + 그림자 수신 / ShadowCaster(투영)
Shader "PrisonLife/Rendering/RockFieldInstanced"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor]   _BaseColor("Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        // Forward + 그림자 수신
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On
            ZTest LEqual
            Cull Back
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _RECEIVE_SHADOWS_OFF
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv0        : TEXCOORD0;
            };

            struct Varyings
            {
                float4  positionCS  : SV_POSITION;
                float2  uv          : TEXCOORD0;
                float3  positionWS  : TEXCOORD1;
                float3  normalWS    : TEXCOORD2;
                nointerpolation float active : TEXCOORD3;
            };

            StructuredBuffer<float4x4> _InstanceMatrices;
            StructuredBuffer<float>  _InstanceActive;

            Varyings vert(Attributes v, uint iid : SV_InstanceID)
            {
                Varyings o;
                o.active = _InstanceActive[iid];
                float4x4 m = _InstanceMatrices[iid];
                float3 posWS = mul(m, float4(v.positionOS, 1.0)).xyz;
                float3x3 o2w = (float3x3)m;
                float3 nWS = normalize(mul(o2w, v.normalOS));
                o.uv = TRANSFORM_TEX(v.uv0, _BaseMap);
                o.positionWS = posWS;
                o.normalWS = nWS;
                o.positionCS = TransformWorldToHClip(posWS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                if (i.active < 0.5) discard;

                float3 n = normalize(i.normalWS);
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb * _BaseColor.rgb;
                float4 sc = TransformWorldToShadowCoord(i.positionWS);
                Light L = GetMainLight(sc, i.positionWS, i.positionCS);
                half Ndl = saturate(dot(n, L.direction));
                half3 direct = albedo * L.color * (Ndl * L.shadowAttenuation) * L.distanceAttenuation;
                half3 amb = albedo * half3(0.15, 0.16, 0.18);
                return half4(direct + amb, 1.0h);
            }
            ENDHLSL
        }
        // 그림자 맵(투영) — URP는 ShadowCaster + ApplyShadowBias
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex   shadowVert
            #pragma fragment shadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            // Shadows.hlsl(298)의 LerpWhiteTo — URP 14는 CommonMaterial.hlsl에 정의됨( Color.hlsl 아님)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct AttributesS
            {
                float3 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct VaryingsS
            {
                float4 positionCS : SV_POSITION;
                nointerpolation float active : TEXCOORD0;
            };

            StructuredBuffer<float4x4> _InstanceMatrices;
            StructuredBuffer<float>  _InstanceActive;

            VaryingsS shadowVert(AttributesS v, uint iid : SV_InstanceID)
            {
                VaryingsS o;
                o.active = _InstanceActive[iid];
                float4x4 m = _InstanceMatrices[iid];
                float3 posWS = mul(m, float4(v.positionOS, 1.0)).xyz;
                float3x3 o2w = (float3x3)m;
                float3 nWS = normalize(mul(o2w, v.normalOS));
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 ldir = normalize(_LightPosition - posWS);
            #else
                float3 ldir = _LightDirection; // URP: 방향광/스팟 셰도우 패스에 설정됨(ShadowUtils)
            #endif
                float4 pCS = TransformWorldToHClip(ApplyShadowBias(posWS, nWS, ldir));
                o.positionCS = ApplyShadowClamping(pCS);
                return o;
            }

            half4 shadowFrag(VaryingsS i) : SV_Target
            {
                if (i.active < 0.5) clip(-1);
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
