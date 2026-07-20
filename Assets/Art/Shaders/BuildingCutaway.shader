Shader "Saturn/BuildingCutaway"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.6, 0.55, 0.5, 1)
        _Fade ("Fade", Range(0, 1)) = 0
        _InsideAlpha ("Inside Alpha", Range(0.05, 1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Fade;
                float _InsideAlpha;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.positionCS = posInputs.positionCS;
                output.normalWS = normalInputs.normalWS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Outside: fully opaque. Inside: lerp toward semi-transparent (still visible).
                float fade = saturate(_Fade);
                float alpha = lerp(1.0, _InsideAlpha, fade) * _BaseColor.a;

                Light mainLight = GetMainLight();
                float ndotl = saturate(dot(normalize(input.normalWS), mainLight.direction));
                float3 lit = _BaseColor.rgb * (0.35 + 0.65 * ndotl);
                return half4(lit, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
