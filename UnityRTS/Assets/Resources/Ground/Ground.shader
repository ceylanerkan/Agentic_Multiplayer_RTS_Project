// Ground: five photo textures (grass, dry grass, sand, earth, rock) mixed by a control texture that GroundMesh paints.
// The transitions follow the brightness of the textures (stones stay on top of sand), and the beach gets wet near the water.
Shader "RTS/Ground"
{
    Properties
    {
        [NoScaleOffset] _Control ("Control: dry, sand, earth, rock", 2D) = "black" {}
        [NoScaleOffset] _Grass ("Grass", 2D) = "gray" {}
        [NoScaleOffset] _GrassN ("Grass normal", 2D) = "gray" {}
        [NoScaleOffset] _Dry ("Dry grass", 2D) = "gray" {}
        [NoScaleOffset] _DryN ("Dry grass normal", 2D) = "gray" {}
        [NoScaleOffset] _Sand ("Sand", 2D) = "gray" {}
        [NoScaleOffset] _SandN ("Sand normal", 2D) = "gray" {}
        [NoScaleOffset] _Earth ("Earth", 2D) = "gray" {}
        [NoScaleOffset] _EarthN ("Earth normal", 2D) = "gray" {}
        [NoScaleOffset] _Rock ("Rock", 2D) = "gray" {}
        [NoScaleOffset] _RockN ("Rock normal", 2D) = "gray" {}
        _SeaLevel ("Sea level", Float) = -0.35
        _Bump ("Bump", Range(0, 2)) = 0.9
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry-10" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
        float _SeaLevel;
        float _Bump;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_Control); SAMPLER(sampler_Control);
            TEXTURE2D(_Grass); SAMPLER(sampler_Grass);
            TEXTURE2D(_GrassN); TEXTURE2D(_Dry); TEXTURE2D(_DryN); TEXTURE2D(_Sand); TEXTURE2D(_SandN);
            TEXTURE2D(_Earth); TEXTURE2D(_EarthN); TEXTURE2D(_Rock); TEXTURE2D(_RockN);

            struct Attributes { float4 position : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 position : SV_POSITION; float2 uv : TEXCOORD0; float3 world : TEXCOORD1; float3 normal : TEXCOORD2; float fog : TEXCOORD3; };

            Varyings Vert(Attributes i)
            {
                Varyings o;
                o.world = TransformObjectToWorld(i.position.xyz);
                o.position = TransformWorldToHClip(o.world);
                o.normal = TransformObjectToWorldNormal(i.normal);
                o.uv = i.uv;
                o.fog = ComputeFogFactor(o.position.z);
                return o;
            }

            // two sizes of the same texture, the second turned: the repetition is hard to see
            half3 Colour(TEXTURE2D_PARAM(tex, samp), float2 p, float size)
            {
                half3 a = SAMPLE_TEXTURE2D(tex, samp, p / size).rgb;
                half3 b = SAMPLE_TEXTURE2D(tex, samp, float2(p.y, -p.x) / (size * 3.3) + 0.37).rgb;
                return lerp(a, b, 0.38);
            }

            // the pictures are imported as colours, so the curve must be undone before they are read as directions
            half2 Bump(TEXTURE2D_PARAM(tex, samp), float2 p, float size)
            {
                half3 c = SAMPLE_TEXTURE2D(tex, samp, p / size).rgb;
                #ifndef UNITY_COLORSPACE_GAMMA
                c = LinearToSRGB(c);
                #endif
                return c.xy * 2 - 1;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float2 p = i.world.xz;
                half4 c = SAMPLE_TEXTURE2D(_Control, sampler_Control, i.uv);
                half grassWeight = saturate(1 - c.r - c.g - c.b - c.a);

                half3 grass = Colour(TEXTURE2D_ARGS(_Grass, sampler_Grass), p, 7);
                half3 dry = Colour(TEXTURE2D_ARGS(_Dry, sampler_Grass), p, 16);
                half3 sand = Colour(TEXTURE2D_ARGS(_Sand, sampler_Grass), p, 9);
                half3 earth = Colour(TEXTURE2D_ARGS(_Earth, sampler_Grass), p, 8);
                half3 rock = Colour(TEXTURE2D_ARGS(_Rock, sampler_Grass), p, 9);

                // transitions by height: the brighter part of a texture stays on top
                half3 lumen = half3(0.3, 0.59, 0.11);
                half h0 = grassWeight + dot(grass, lumen) * 0.9 * step(0.01, grassWeight);
                half h1 = c.r + dot(dry, lumen) * 0.5 * step(0.01, c.r);
                half h2 = c.g + dot(sand, lumen) * 0.3 * step(0.01, c.g);
                half h3 = c.b + dot(earth, lumen) * 0.6 * step(0.01, c.b);
                half h4 = c.a + dot(rock, lumen) * 0.6 * step(0.01, c.a);
                half top = max(max(max(h0, h1), max(h2, h3)), h4) - 0.22;
                h0 = max(h0 - top, 0); h1 = max(h1 - top, 0); h2 = max(h2 - top, 0); h3 = max(h3 - top, 0); h4 = max(h4 - top, 0);
                half sum = max(h0 + h1 + h2 + h3 + h4, 0.001);
                half3 colour = (grass * h0 + dry * h1 + sand * h2 + earth * h3 + rock * h4) / sum;
                half2 bump = (Bump(TEXTURE2D_ARGS(_GrassN, sampler_Grass), p, 7) * h0 + Bump(TEXTURE2D_ARGS(_DryN, sampler_Grass), p, 16) * h1
                            + Bump(TEXTURE2D_ARGS(_SandN, sampler_Grass), p, 9) * h2 + Bump(TEXTURE2D_ARGS(_EarthN, sampler_Grass), p, 8) * h3
                            + Bump(TEXTURE2D_ARGS(_RockN, sampler_Grass), p, 9) * h4) / sum;
                half3 normal = normalize(i.normal + half3(bump.x, 0, bump.y) * _Bump);

                // wet sand at the waterline
                half wet = 1 - smoothstep(_SeaLevel + 0.02, _SeaLevel + 0.30, i.world.y);
                colour *= lerp(1, 0.58, wet);

                float4 shadowCoord = TransformWorldToShadowCoord(i.world);
                Light sun = GetMainLight(shadowCoord);
                half light = saturate(dot(normal, sun.direction)) * sun.shadowAttenuation * sun.distanceAttenuation;
                half3 lit = colour * (sun.color * light + SampleSH(normal));
                half3 view = GetWorldSpaceNormalizeViewDir(i.world);
                half shine = pow(saturate(dot(normal, normalize(sun.direction + view))), 48) * light;
                lit += sun.color * shine * (0.03 + 0.35 * wet);
                return half4(MixFog(lit, i.fog), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            float4 Vert(float4 position : POSITION) : SV_POSITION { return TransformObjectToHClip(position.xyz); }
            half4 Frag(float4 position : SV_POSITION) : SV_Target { return position.z; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            struct Attributes { float4 position : POSITION; float3 normal : NORMAL; };
            struct Varyings { float4 position : SV_POSITION; float3 normal : TEXCOORD0; };
            Varyings Vert(Attributes i) { Varyings o; o.position = TransformObjectToHClip(i.position.xyz); o.normal = TransformObjectToWorldNormal(i.normal); return o; }
            half4 Frag(Varyings i) : SV_Target { return half4(normalize(i.normal), 0); }
            ENDHLSL
        }
    }
}
