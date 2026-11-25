// Sea: turquoise and clear over the beach, dark over deep water, small moving waves, sunlight on them and foam along the shore.
// The depth comes from the depth texture (the sea floor of GroundMesh).
Shader "RTS/Water"
{
    Properties
    {
        _Shallow ("Shallow", Color) = (0.23, 0.62, 0.62, 1)
        _Deep ("Deep", Color) = (0.03, 0.16, 0.30, 1)
        _Sky ("Reflected sky", Color) = (0.60, 0.74, 0.88, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent-100" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
            half4 _Shallow;
            half4 _Deep;
            half4 _Sky;
            CBUFFER_END
            float _RtsNight; // set by DayNight, 0 = day

            struct Varyings { float4 position : SV_POSITION; float3 world : TEXCOORD0; float fog : TEXCOORD1; };

            Varyings Vert(float4 position : POSITION)
            {
                Varyings o;
                o.world = TransformObjectToWorld(position.xyz);
                o.position = TransformWorldToHClip(o.world);
                o.fog = ComputeFogFactor(o.position.z);
                return o;
            }

            float Hash(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }

            float Noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3 - 2 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x), lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), f.x), f.y);
            }

            float Waves(float2 p)
            {
                float t = _Time.y;
                return Noise(p * 0.42 + float2(0.30, 0.22) * t) * 0.55 + Noise(p * 0.95 - float2(0.35, -0.20) * t) * 0.30 + Noise(p * 2.3 + float2(-0.5, 0.6) * t) * 0.15;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float2 uv = GetNormalizedScreenSpaceUV(i.position);
                float raw = SampleSceneDepth(uv);
                #if !UNITY_REVERSED_Z
                raw = lerp(UNITY_NEAR_CLIP_VALUE, 1, raw);
                #endif
                float3 floorPoint = ComputeWorldSpacePosition(uv, raw, UNITY_MATRIX_I_VP);
                float depth = max(0, i.world.y - floorPoint.y); // metres of water under this point

                float2 p = i.world.xz;
                float e = 0.35;
                float h = Waves(p);
                float3 normal = normalize(float3((h - Waves(p + float2(e, 0))) * 0.55, e, (h - Waves(p + float2(0, e))) * 0.55));

                Light sun = GetMainLight(TransformWorldToShadowCoord(i.world));
                half3 view = GetWorldSpaceNormalizeViewDir(i.world);
                half shade = lerp(0.45, 1, sun.shadowAttenuation);

                half deep = smoothstep(0.05, 2.2, depth + (h - 0.5) * 0.35);
                half3 water = lerp(_Shallow.rgb, _Deep.rgb, deep);
                half3 colour = water * (sun.color * saturate(dot(normal, sun.direction)) * shade * 0.75 + SampleSH(normal) * 0.6);
                half fresnel = pow(1 - saturate(dot(normal, view)), 4);
                colour = lerp(colour, _Sky.rgb * lerp(half3(1, 1, 1), half3(0.16, 0.20, 0.32), _RtsNight), saturate(0.06 + fresnel * 0.6));
                half glint = pow(saturate(dot(normal, normalize(sun.direction + view))), 260);
                colour += sun.color * glint * 1.2 * shade;

                // foam: a broken line at the waterline and weaker rings that run up the beach
                float n = Noise(p * 1.7 + _Time.y * 0.25) * 0.6 + Noise(p * 4.1 - _Time.y * 0.2) * 0.4;
                float edge = 1 - saturate(depth / 0.22);
                float rings = saturate(sin(depth * 11 + _Time.y * 1.3 + n * 3) - 0.55) * (1 - saturate(depth / 0.75)) * 0.9;
                half foam = saturate(smoothstep(0.35, 0.75, edge + n * 0.45 - 0.12) + rings * smoothstep(0.35, 0.65, n));
                colour = lerp(colour, half3(0.95, 0.97, 0.98) * (0.45 + 0.6 * shade) * lerp(half3(1, 1, 1), half3(0.30, 0.36, 0.50), _RtsNight), foam * 0.85);

                half alpha = saturate(lerp(0.30, 0.97, smoothstep(0.0, 1.7, depth)) + fresnel * 0.3 + foam * 0.6);
                return half4(MixFog(colour, i.fog), alpha);
            }
            ENDHLSL
        }
    }
}
