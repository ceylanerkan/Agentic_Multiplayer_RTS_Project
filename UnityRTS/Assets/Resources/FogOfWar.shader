// Fog of war, projected onto everything below it: a box around the map is drawn after the world; for every pixel the depth texture
// gives the world position behind it, and the fog texture is read at that x/z. So the fog lies on the ground, trees and buildings
// instead of hanging over them as a sheet.
Shader "RTS/FogOfWar"
{
    Properties
    {
        _FogTex ("Fog (alpha)", 2D) = "black" {}
        _FogColor ("Colour", Color) = (0.02, 0.03, 0.06, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+100" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Front
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_FogTex);
            SAMPLER(sampler_FogTex);
            float4 _FogMap; // x min, z min, 1 / width, 1 / depth of the map
            half4 _FogColor;

            float4 Vert(float4 position : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(position.xyz);
            }

            half4 Frag(float4 position : SV_POSITION) : SV_Target
            {
                float2 uv = GetNormalizedScreenSpaceUV(position);
                float depth = SampleSceneDepth(uv);
                #if UNITY_REVERSED_Z
                if (depth < 0.000001) return 0; // sky
                #else
                if (depth > 0.999999) return 0;
                #endif
                #if !UNITY_REVERSED_Z
                depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, depth);
                #endif
                float3 world = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
                float2 map = (world.xz - _FogMap.xy) * _FogMap.zw;
                // outside the map the fog of the edge goes on, but thin over high ground: the mountains can be seen (nobody can be there)
                float2 over = max(max(-map, map - 1), 0) / _FogMap.zw; // metres
                half thin = 1 - 0.72 * smoothstep(0, 12, length(over)) * smoothstep(0.5, 4, world.y);
                return half4(_FogColor.rgb, SAMPLE_TEXTURE2D(_FogTex, sampler_FogTex, saturate(map)).a * thin);
            }
            ENDHLSL
        }
    }
}
