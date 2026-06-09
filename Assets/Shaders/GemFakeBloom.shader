Shader "Custom/GemFakeBloom"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.1, 0.1, 0.15, 1)
        [HDR] _GlowColor("Glow Color (HDR)", Color) = (1, 1, 1, 1)
        _RimPower("Rim Power", Range(0.1, 8)) = 2
        _RimIntensity("Rim Intensity", Range(0, 8)) = 3
        _CoreEmission("Core Emission", Range(0, 4)) = 0.5
        _FacingBoost("Facing Boost", Range(0, 4)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "GemBody"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _GlowColor;
                float _RimPower;
                float _RimIntensity;
                float _CoreEmission;
                float _FacingBoost;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = posInputs.positionCS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(IN.viewDirWS);
                float NdotV = saturate(dot(N, V));

                float rim = pow(1.0 - NdotV, _RimPower) * _RimIntensity;
                float facing = NdotV * _FacingBoost;

                float3 core = _BaseColor.rgb * _CoreEmission;
                float3 glow = _GlowColor.rgb * (rim + facing);
                return half4(core + glow, 1);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
