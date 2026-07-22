// Full-screen storm desaturation when the player is outside the safe zone.
// Driven by SurvivorStormDesaturatePass — _PlayerInStorm blends 0..1.
Shader "Saturn/Survivor/StormDesaturate"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "StormDesaturate"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _PlayerInStorm;
            float _DesaturateAmount;

            float3 Desaturate(float3 color, float amount)
            {
                float luma = dot(color, float3(0.2126, 0.7152, 0.0722));
                return lerp(color, luma.xxx, amount);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord.xy;
                float4 sceneColor = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, 0);

                float amount = saturate(_PlayerInStorm) * saturate(_DesaturateAmount);
                float3 result = Desaturate(sceneColor.rgb, amount);
                return float4(result, sceneColor.a);
            }
            ENDHLSL
        }
    }
}
