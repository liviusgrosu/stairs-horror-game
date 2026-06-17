Shader "Custom/BumpyIceGlass"
{
    Properties
    {
        _BaseColor       ("Ice Color", Color) = (0.81, 0.91, 0.94, 1)
        _RimColor        ("Rim/Emission Color", Color) = (0.4, 0.7, 1.0, 1)

        _MinAlpha        ("Face Alpha", Range(0,1)) = 0.20
        _MaxAlpha        ("Edge Alpha", Range(0,1)) = 0.90
        _FresnelPower    ("Fresnel Power", Range(0.5, 8)) = 3.0
        _RimStrength     ("Rim Glow Strength", Range(0, 4)) = 0.6

        _Smoothness      ("Smoothness", Range(0,1)) = 0.85
        _SpecStrength    ("Specular Strength", Range(0,2)) = 1.0

        [Header(Bumps)]
        _BumpScale       ("Bump Tiling", Float) = 8.0
        _BumpStrength    ("Bump Strength", Range(0,1)) = 0.25

        [Header(Cracks)]
        [Toggle(_CRACKS_ON)] _Cracks ("Enable Cracks", Float) = 1
        _CrackColor      ("Crack Color", Color) = (0.92, 0.97, 1.0, 1)
        _CrackScale      ("Crack Scale (density)", Float) = 4.0
        _CrackWidth      ("Crack Width", Range(0.001, 0.3)) = 0.06
        _CrackIntensity  ("Crack Intensity", Range(0, 3)) = 1.2
        _CrackGroove     ("Crack Normal Groove", Range(0, 1)) = 0.3

        [Header(Refraction (needs Opaque Texture ON))]
        _RefractStrength ("Refraction Strength", Range(0, 0.2)) = 0.04

        [Header(Pixelation)]
        [Toggle(_PIXELATE_ON)] _Pixelate ("Pixelate (point filter look)", Float) = 1
        _PixelSize       ("Pixel Block Size (screen px)", Range(1, 32)) = 6

        [Toggle(_FACETED_ON)] _Faceted ("Force Faceted (flat) Normals", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _FACETED_ON
            #pragma shader_feature_local _PIXELATE_ON
            #pragma shader_feature_local _CRACKS_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float4 screenPos   : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _RimColor;
                float  _MinAlpha;
                float  _MaxAlpha;
                float  _FresnelPower;
                float  _RimStrength;
                float  _Smoothness;
                float  _SpecStrength;
                float  _BumpScale;
                float  _BumpStrength;
                float  _RefractStrength;
                float  _PixelSize;
                float4 _CrackColor;
                float  _CrackScale;
                float  _CrackWidth;
                float  _CrackIntensity;
                float  _CrackGroove;
            CBUFFER_END

            // --- cheap hash / value noise for procedural bumps ---
            float hash13(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float valueNoise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float n000 = hash13(i + float3(0,0,0));
                float n100 = hash13(i + float3(1,0,0));
                float n010 = hash13(i + float3(0,1,0));
                float n110 = hash13(i + float3(1,1,0));
                float n001 = hash13(i + float3(0,0,1));
                float n101 = hash13(i + float3(1,0,1));
                float n011 = hash13(i + float3(0,1,1));
                float n111 = hash13(i + float3(1,1,1));

                float x00 = lerp(n000, n100, f.x);
                float x10 = lerp(n010, n110, f.x);
                float x01 = lerp(n001, n101, f.x);
                float x11 = lerp(n011, n111, f.x);

                float y0 = lerp(x00, x10, f.y);
                float y1 = lerp(x01, x11, f.y);

                return lerp(y0, y1, f.z);
            }

            // 3D vector hash for Voronoi feature points
            float3 hash33(float3 p)
            {
                p = float3(dot(p, float3(127.1, 311.7,  74.7)),
                           dot(p, float3(269.5, 183.3, 246.1)),
                           dot(p, float3(113.5, 271.9, 124.6)));
                return frac(sin(p) * 43758.5453123);
            }

            // Voronoi edge distance: returns (F2 - F1). It approaches 0 along the
            // borders between cells, which we threshold into thin crack lines.
            float voronoiEdge(float3 x)
            {
                float3 p = floor(x);
                float3 f = frac(x);

                float f1 = 8.0;
                float f2 = 8.0;
                [unroll]
                for (int k = -1; k <= 1; k++)
                [unroll]
                for (int j = -1; j <= 1; j++)
                [unroll]
                for (int i = -1; i <= 1; i++)
                {
                    float3 g = float3(i, j, k);
                    float3 o = hash33(p + g);
                    float3 r = g + o - f;
                    float d = dot(r, r);
                    if (d < f1)      { f2 = f1; f1 = d; }
                    else if (d < f2) { f2 = d; }
                }
                return sqrt(f2) - sqrt(f1);
            }

            // Crack mask + screen-space gradient (for grooving the normal).
            // Returns mask in .x, gradient in .yz (d(mask)/d(screen)).
            float crackMask(float3 posWS, out float dMaskX, out float dMaskY)
            {
                float edge  = voronoiEdge(posWS * _CrackScale);
                float mask  = saturate((1.0 - smoothstep(0.0, _CrackWidth, edge)) * _CrackIntensity);
                dMaskX = ddx(mask);
                dMaskY = ddy(mask);
                return mask;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.screenPos   = ComputeScreenPos(posInputs.positionCS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 posWS    = IN.positionWS;
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;

                // world-space derivatives (constant per triangle) — shared by the
                // faceted normal, pixel reconstruction, and crack grooving
                float3 dpx = ddx(IN.positionWS);
                float3 dpy = ddy(IN.positionWS);

                // --- pixelation: snap to a screen-pixel grid, then reconstruct the
                //     world position at the block's representative pixel so ALL shading
                //     (lighting, fresnel, refraction) quantizes into chunky blocks ---
            #ifdef _PIXELATE_ON
                float blockSize  = max(_PixelSize, 1.0);
                float2 pixelCoord = screenUV * _ScreenParams.xy;
                float2 snapped    = (floor(pixelCoord / blockSize) + 0.5) * blockSize;
                float2 deltaPx    = snapped - pixelCoord;          // offset to block center
                // shift world pos along screen-space derivatives to the block center
                posWS    += dpx * deltaPx.x + dpy * deltaPx.y;
                screenUV  = snapped / _ScreenParams.xy;
            #endif

                // --- base normal ---
                float3 N;
            #ifdef _FACETED_ON
                // Derive a flat per-triangle normal from world-pos derivatives.
                // This gives the lowpoly faceted look regardless of mesh smoothing.
                N = normalize(cross(dpy, dpx));
                // keep it facing the same hemisphere as the interpolated normal
                N *= sign(dot(N, IN.normalWS));
            #else
                N = normalize(IN.normalWS);
            #endif

                // --- procedural bump perturbation ---
                float3 sp = posWS * _BumpScale;
                float e = 0.15;
                float nC = valueNoise(sp);
                float nX = valueNoise(sp + float3(e,0,0));
                float nY = valueNoise(sp + float3(0,e,0));
                float nZ = valueNoise(sp + float3(0,0,e));
                float3 grad = float3(nX - nC, nY - nC, nZ - nC) / e;
                N = normalize(N - grad * _BumpStrength);

                // --- cracks: thin voronoi-edge fracture lines ---
                float crack = 0.0;
            #ifdef _CRACKS_ON
                float dMaskX, dMaskY;
                crack = crackMask(posWS, dMaskX, dMaskY);
                // tilt the normal into a V-groove along the crack so it catches light
                float3 grooveDir = normalize(dpx) * dMaskX + normalize(dpy) * dMaskY;
                float  gl = length(grooveDir);
                grooveDir = (gl > 1e-5) ? grooveDir / gl : float3(0, 0, 0);
                N = normalize(N - grooveDir * crack * _CrackGroove);
            #endif

                float3 V = GetWorldSpaceNormalizeViewDir(posWS);

                // --- fresnel drives edge opacity + rim glow ---
                float fres = pow(saturate(1.0 - saturate(dot(N, V))), _FresnelPower);
                float alpha = lerp(_MinAlpha, _MaxAlpha, fres);

                // --- refraction via scene opaque texture (screenUV already snapped) ---
                float2 refractUV = screenUV + N.xy * _RefractStrength;
                float3 sceneCol = SampleSceneColor(refractUV);
                float3 baseCol  = lerp(sceneCol, _BaseColor.rgb, _BaseColor.a * 0.6);

                // --- main directional light specular (glassy highlight) ---
                Light mainLight = GetMainLight();
                float3 L = mainLight.direction;
                float3 H = normalize(L + V);
                float specPow = exp2(_Smoothness * 10.0 + 1.0);
                float spec = pow(saturate(dot(N, H)), specPow) * _SpecStrength;
                float NdotL = saturate(dot(N, L));

                float3 lit = baseCol * (0.6 + 0.4 * NdotL) * mainLight.color;
                lit += spec * mainLight.color;
                lit += fres * _RimColor.rgb * _RimStrength;

                // cracks tint toward the crack color and read as brighter, more solid lines
                lit = lerp(lit, _CrackColor.rgb, crack);

                // brighten alpha where specular/rim/cracks hit so they read as solid glints
                alpha = saturate(alpha + spec + fres * 0.15 + crack * 0.5);

                return half4(lit, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
