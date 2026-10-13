// 傳送門窗戶：依螢幕座標取 PortalWindow 鏡像相機的畫面，讓沙灘上的小魚缸玻璃「看進」魚缸世界。
// 鏡像畫面透明的地方（魚缸世界的空水、背景）透出窗戶後面的沙灘（需要相機的 Opaque Texture，PortalWindow 會自動開）。
Shader "FishTank/PortalWindow"
{
    Properties
    {
        [HideInInspector] _PortalTex ("Portal (set by script)", 2D) = "black" {}
        _Tint ("Portal Tint", Color) = (1, 1, 1, 1)
        _GlassColor ("Glass Color (A = 強度)", Color) = (0.6, 0.85, 1, 0.08)
        [Toggle] _FlipY ("Flip Y（畫面上下顛倒時勾）", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "PortalWindow"
            Tags { "LightMode" = "UniversalForward" }

            // 每個像素直接覆寫成「後面的場景 + 鏡像畫面」，好幾片玻璃重疊也不會疊色
            ZWrite Off
            Blend Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            TEXTURE2D(_PortalTex);
            SAMPLER(sampler_PortalTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half4 _GlassColor;
                float _FlipY;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.screenPos.xy / input.screenPos.w;
                float2 portalUV = float2(uv.x, _FlipY > 0.5 ? 1.0 - uv.y : uv.y);

                half4 portal = SAMPLE_TEXTURE2D(_PortalTex, sampler_PortalTex, portalUV);
                half3 behind = SampleSceneColor(uv);
                half3 color = lerp(behind, portal.rgb * _Tint.rgb, saturate(portal.a));
                color = lerp(color, _GlassColor.rgb, _GlassColor.a);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
