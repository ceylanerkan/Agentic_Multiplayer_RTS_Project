// Models of buildings, units and nature: one picture times one colour, sun with shadows, ambient light, optional cut-out (leaves).
// An own small shader instead of URP Lit, because the materials are made at run time and their variants would be stripped from a build.
Shader "RTS/Model"
{
    Properties
    {
        [MainTexture] _BaseMap ("Picture", 2D) = "white" {}
        [MainColor] _BaseColor ("Colour", Color) = (1, 1, 1, 1)
        _Cutoff ("Cut out below this alpha", Range(0, 1)) = 0
        _Shine ("Shine", Range(0, 1)) = 0.05
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Cull [_Cull]

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;
        half4 _BaseColor;
        half _Cutoff;
        half _Shine;
        half _Cull;
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

            half4 Frag(Varyings i) : SV_Target
            {
                half4 picture = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                clip(picture.a - _Cutoff);
                half3 colour = picture.rgb * _BaseColor.rgb;
                float3 normal = normalize(i.normal);
                Light sun = GetMainLight(TransformWorldToShadowCoord(i.world));
                half light = saturate(dot(normal, sun.direction)) * sun.shadowAttenuation * sun.distanceAttenuation;
                half3 lit = colour * (sun.color * light + SampleSH(normal));
                half3 view = GetWorldSpaceNormalizeViewDir(i.world);
                lit += sun.color * pow(saturate(dot(normal, normalize(sun.direction + view))), 32) * light * _Shine;
                return half4(MixFog(lit, i.fog), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct Attributes { float4 position : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 position : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings Vert(Attributes i)
            {
                Varyings o;
                float3 world = TransformObjectToWorld(i.position.xyz), normal = TransformObjectToWorldNormal(i.normal);
                o.position = TransformWorldToHClip(ApplyShadowBias(world, normal, _LightDirection));
                #if UNITY_REVERSED_Z
                o.position.z = min(o.position.z, UNITY_NEAR_CLIP_VALUE);
                #else
                o.position.z = max(o.position.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                o.uv = i.uv;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).a - _Cutoff);
                return 0;
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
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            struct Varyings { float4 position : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(float4 position : POSITION, float2 uv : TEXCOORD0) { Varyings o; o.position = TransformObjectToHClip(position.xyz); o.uv = uv; return o; }
            half4 Frag(Varyings i) : SV_Target
            {
                clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).a - _Cutoff);
                return i.position.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            struct Attributes { float4 position : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 position : SV_POSITION; float2 uv : TEXCOORD0; float3 normal : TEXCOORD1; };
            Varyings Vert(Attributes i) { Varyings o; o.position = TransformObjectToHClip(i.position.xyz); o.normal = TransformObjectToWorldNormal(i.normal); o.uv = i.uv; return o; }
            half4 Frag(Varyings i) : SV_Target
            {
                clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).a - _Cutoff);
                return half4(normalize(i.normal), 0);
            }
            ENDHLSL
        }
    }
}
