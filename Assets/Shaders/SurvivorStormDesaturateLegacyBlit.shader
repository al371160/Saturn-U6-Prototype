// Built-in Render Pipeline counterpart to SurvivorStormDesaturate.shader, used only by
// SurvivorStormDesaturateCameraFallback's OnRenderImage/Graphics.Blit path (URP never calls
// OnRenderImage, so this only matters if the project is ever switched off URP). Uses the classic
// image-effect appdata_img/Graphics.Blit quad instead of the SRP full-screen-triangle trick, and
// reconstructs world position from depth via an inverse view-projection matrix supplied by the
// camera script each frame.
Shader "Saturn/Survivor/StormDesaturateLegacyBlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;
            float4x4 _StormInvViewProjection;
            float4 _StormCenter;
            float _StormRadius;
            float _EdgeSoftness;
            float _DesaturateAmount;

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(appdata_img input)
            {
                Varyings output;
                output.positionCS = UnityObjectToClipPos(input.vertex);
                output.uv = input.texcoord;
                return output;
            }

            float3 Desaturate(float3 color, float amount)
            {
                float luma = dot(color, float3(0.2126, 0.7152, 0.0722));
                return lerp(color, luma.xxx, amount);
            }

            fixed4 Frag(Varyings input) : SV_Target
            {
                fixed4 sceneColor = tex2D(_MainTex, input.uv);

                float rawDepth = tex2D(_CameraDepthTexture, input.uv).r;
                float4 clipPos = float4(input.uv * 2.0 - 1.0, rawDepth, 1.0);
#if defined(UNITY_UV_STARTS_AT_TOP)
                clipPos.y = -clipPos.y;
#endif
                float4 worldPos4 = mul(_StormInvViewProjection, clipPos);
                float3 worldPos = worldPos4.xyz / worldPos4.w;

                float distFromCenter = length(worldPos.xz - _StormCenter.xz);
                float edge = saturate((distFromCenter - _StormRadius) / max(_EdgeSoftness, 0.001));

                float3 result = Desaturate(sceneColor.rgb, edge * _DesaturateAmount);
                return fixed4(result, sceneColor.a);
            }
            ENDCG
        }
    }
}
