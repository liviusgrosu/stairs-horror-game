Shader "Stairs Horror/Chest Glow Beam"
{
    Properties
    {
        [HDR] _BaseColor ("Floor Color", Color) = (0.03, 0.55, 3.5, 1)
        [HDR] _BeamColor ("Beam Color", Color) = (0.01, 0.12, 2.2, 1)
        [HDR] _CoreColor ("Streak Core", Color) = (0.08, 0.75, 4.8, 1)
        _Opacity ("Opacity", Range(0, 1)) = 0.68
        _BodyStrength ("Soft Body", Range(0, 2)) = 0.32
        _StreakStrength ("Streak Brightness", Range(0, 5)) = 2.8
        _StreakDensity ("Primary Ray Density", Range(12, 160)) = 72
        _StreakSharpness ("Ray Edge Sharpness", Range(1, 16)) = 5
        _FineRayStrength ("Fine Ray Strength", Range(0, 2)) = 0.8
        _MicroRayStrength ("Micro Ray Strength", Range(0, 2)) = 0.55
        _RayWaviness ("Ray Waviness", Range(0, 0.02)) = 0.0045
        _ScrollSpeed ("Upward Speed", Range(-3, 3)) = 0.65
        _ScrollContrast ("Scroll Visibility", Range(0, 1)) = 0.68
        _ShimmerSpeed ("Shimmer Speed", Range(0, 5)) = 1.15
        _BaseGlow ("Floor Glow", Range(0, 5)) = 1.8
        _TopFade ("Top Fade", Range(0.01, 1)) = 0.13
        _Breakup ("Top Breakup", Range(0, 1)) = 0.72
        _Flicker ("Flicker", Range(0, 1)) = 0.18
        [Header(Pixelation)]
        [Toggle] _Pixelate ("Pixelated Point Filter", Float) = 1
        _PixelSize ("Pixel Block Size (screen px)", Range(1, 16)) = 6
        [Header(Distance Fade)]
        _FadeNear ("Fade Start Distance", Float) = 20
        _FadeFar ("Fade End Distance", Float) = 30
        [HideInInspector] _LayerSpeed ("Layer Speed", Float) = 1
        [HideInInspector] _LayerPhase ("Layer Phase", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+20"
        }

        Pass
        {
            Name "AnimatedMagicBeam"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            Cull Off
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirectionWS : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                float4 screenPosition : TEXCOORD4;
                float3 objectOriginWS : TEXCOORD5;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _BeamColor;
                half4 _CoreColor;
                half _Opacity;
                half _BodyStrength;
                half _StreakStrength;
                half _StreakDensity;
                half _StreakSharpness;
                half _FineRayStrength;
                half _MicroRayStrength;
                half _RayWaviness;
                half _ScrollSpeed;
                half _ScrollContrast;
                half _ShimmerSpeed;
                half _BaseGlow;
                half _TopFade;
                half _Breakup;
                half _Flicker;
                half _Pixelate;
                half _PixelSize;
                half _FadeNear;
                half _FadeFar;
                half _LayerSpeed;
                half _LayerPhase;
            CBUFFER_END

            float WrappedCellHash(float cell, float cellCount, float seed)
            {
                float wrappedCell = cell - floor(cell / cellCount) * cellCount;
                return frac(sin((wrappedCell + 1.37) * 127.1 + seed * 311.7) * 43758.5453);
            }

            float WrappedValueNoise(float u, float cellCount, float seed)
            {
                cellCount = max(2.0, floor(cellCount));
                float position = frac(u) * cellCount;
                float cell = floor(position);
                float blend = frac(position);
                blend = blend * blend * (3.0 - 2.0 * blend);
                float left = WrappedCellHash(cell, cellCount, seed);
                float right = WrappedCellHash(cell + 1.0, cellCount, seed);
                return lerp(left, right, blend);
            }

            float RandomRayLayer(float u, float rayCount, float seed, float minimumWidth, float maximumWidth)
            {
                rayCount = max(4.0, floor(rayCount));
                float position = frac(u) * rayCount;
                float baseCell = floor(position);
                float ray = 0.0;

                // Each cell contains one independently offset, sized and brightened ray.
                // Sampling adjacent cells preserves heavily-jittered rays at cell boundaries.
                [unroll]
                for (int offset = -1; offset <= 1; offset++)
                {
                    float cell = baseCell + offset;
                    float randomPosition = WrappedCellHash(cell, rayCount, seed);
                    float randomWidth = WrappedCellHash(cell, rayCount, seed + 17.3);
                    float randomBrightness = WrappedCellHash(cell, rayCount, seed + 43.7);
                    float center = cell + lerp(0.08, 0.92, randomPosition);
                    float width = lerp(minimumWidth, maximumWidth, randomWidth);
                    float distanceToRay = abs(position - center);
                    float profile = 1.0 - smoothstep(width, width * 2.8, distanceToRay);
                    profile = pow(saturate(profile), max(0.5, _StreakSharpness * 0.24));
                    ray = max(ray, profile * lerp(0.38, 1.0, randomBrightness));
                }

                return ray;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = positions.positionCS;
                output.uv = input.uv;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirectionWS = GetWorldSpaceViewDir(positions.positionWS);
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                output.screenPosition = ComputeScreenPos(positions.positionCS);
                output.objectOriginWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
                return output;
            }

            half4 Frag(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                float2 sampledUV = input.uv;
                if (_Pixelate > 0.5)
                {
                    // Sample the procedural shader once at the center of each screen-space
                    // block—the procedural equivalent of point-filtering a low-res texture.
                    float2 screenUV = input.screenPosition.xy / input.screenPosition.w;
                    float2 pixelPosition = screenUV * _ScreenParams.xy;
                    float blockSize = max(_PixelSize, 1.0);
                    float2 snappedPixel = (floor(pixelPosition / blockSize) + 0.5) * blockSize;
                    float2 pixelOffset = snappedPixel - pixelPosition;
                    sampledUV += ddx(input.uv) * pixelOffset.x + ddy(input.uv) * pixelOffset.y;
                }

                float u = frac(sampledUV.x);
                float v = saturate(sampledUV.y);
                float angle = u * TWO_PI;
                float time = _Time.y;
                float scrollTime = time * _ScrollSpeed * _LayerSpeed + _LayerPhase * TWO_PI;

                float primaryCount = max(12.0, floor(_StreakDensity));
                float fineCount = floor(primaryCount * 1.83);
                float microCount = floor(primaryCount * 3.41);

                // Height-dependent displacement bends the rays without breaking their continuity.
                float localPhase = WrappedValueNoise(u, 19.0, 8.2) * TWO_PI;
                float rayWarp = sin(v * 12.7 - scrollTime * 2.1 + localPhase) * 0.62;
                rayWarp += sin(v * 27.3 - scrollTime * 1.3 + localPhase * 1.71) * 0.38;
                float warpedU = frac(u + rayWarp * _RayWaviness);

                float primaryRays = RandomRayLayer(warpedU, primaryCount, 3.1, 0.055, 0.15);
                float fineRays = RandomRayLayer(warpedU + rayWarp * 0.0007, fineCount, 11.7, 0.025, 0.09);
                float microRays = RandomRayLayer(warpedU - rayWarp * 0.0005, microCount, 29.3, 0.012, 0.055);
                float streakColumns = max(primaryRays, max(fineRays * _FineRayStrength, microRays * _MicroRayStrength));

                // This modulation never reaches zero, so rays scroll and shimmer but remain connected.
                float flowA = 0.5 + 0.5 * sin(v * 15.0 - scrollTime * 5.0 + localPhase);
                float flowB = 0.5 + 0.5 * sin(v * 31.0 - scrollTime * 2.7 + localPhase * 1.43);
                float travellingLight = flowA * 0.62 + flowB * 0.38;
                // Brightness details travel upward but never reach zero, preserving continuous rays.
                float continuousFlow = lerp(1.0, lerp(0.5, 1.32, travellingLight), _ScrollContrast);

                float heightRandom = WrappedValueNoise(warpedU, primaryCount, 57.1) * 0.62;
                heightRandom += WrappedValueNoise(warpedU, 31.0, 91.4) * 0.38;
                float columnHeight = lerp(0.5, 1.1, heightRandom);
                float raggedTop = 1.0 - smoothstep(columnHeight - _TopFade, columnHeight, v);
                raggedTop = lerp(1.0 - smoothstep(0.72, 1.0, v), raggedTop, _Breakup);

                float verticalFade = pow(saturate(1.0 - v), 0.72);
                // Exponential falloff avoids a horizontal band around the cylinder.
                float baseBand = exp2(-v * 22.0);
                float softNoise = WrappedValueNoise(u + v * 0.013, 37.0, 14.8);
                float softBody = verticalFade * raggedTop * lerp(0.12, 0.62, softNoise) * _BodyStrength;
                float streaks = streakColumns * continuousFlow * lerp(1.0, 0.62, v) * raggedTop * _StreakStrength;
                float baseTexture = saturate(0.12 + streakColumns * 1.3 + softNoise * 0.24);
                float texturedBase = baseBand * baseTexture;

                float3 normalWS = normalize(input.normalWS) * (isFrontFace ? 1.0 : -1.0);
                float3 viewWS = normalize(input.viewDirectionWS);
                float facing = saturate(abs(dot(normalWS, viewWS)));
                float volumeVisibility = lerp(0.55, 1.0, facing);
                float flickerPhase = WrappedValueNoise(warpedU, primaryCount, 122.5) * TWO_PI;
                float flicker = lerp(1.0, 0.84 + 0.16 * sin(time * _ShimmerSpeed * 6.1 + flickerPhase), _Flicker);

                float intensity = (softBody + streaks + texturedBase * _BaseGlow) * volumeVisibility * flicker;
                float3 gradient = lerp(_BaseColor.rgb, _BeamColor.rgb, smoothstep(0.0, 0.55, v));
                gradient = lerp(gradient, _CoreColor.rgb, saturate(streaks * 0.45 + texturedBase * 0.55));
                float alpha = saturate((softBody * 0.5 + streaks * 0.46 + texturedBase) * _Opacity);

                // Match the chain/furnace-style camera-distance fade. Object-center distance
                // keeps every point of this very wide ring fading at the same time.
                float safeFadeFar = max(_FadeFar, _FadeNear + 0.001);
                float cameraDistance = distance(_WorldSpaceCameraPos.xyz, input.objectOriginWS);
                float distanceFade = 1.0 - smoothstep(_FadeNear, safeFadeFar, cameraDistance);
                alpha *= distanceFade;

                float3 color = gradient * intensity;
                color = MixFog(color, input.fogFactor);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
