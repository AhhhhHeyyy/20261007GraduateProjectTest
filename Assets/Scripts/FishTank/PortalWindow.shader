// 傳送門窗戶：依螢幕座標取 PortalWindow 鏡像相機的畫面，讓沙灘上的小魚缸玻璃「看進」魚缸世界。
// 鏡像畫面透明的地方（魚缸世界的空水、背景）透出窗戶後面的場景。
// 兩套 SubShader，Unity 依專案的渲染管線自動挑：
//   - URP：後面的場景取自 _CameraOpaqueTexture（PortalWindow 會自動打開相機的 Opaque Texture）
//   - Built-in：後面的場景用 GrabPass 取
// 兩套都只用 UnityCG，不 include URP 的檔案，沒裝 URP 的專案也能編譯。
Shader "FishTank/PortalWindow"
{
    Properties
    {
        [HideInInspector] _PortalTex ("Portal (set by script)", 2D) = "black" {}
        _Tint ("Portal Tint", Color) = (1, 1, 1, 1)
        _GlassColor ("Glass Color (A = 強度)", Color) = (0.6, 0.85, 1, 0.08)
        [Toggle] _FlipY ("Flip Y（畫面上下顛倒時勾）", Float) = 0
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    sampler2D _PortalTex;
    half4 _Tint;
    half4 _GlassColor;
    float _FlipY;

    struct v2f
    {
        float4 pos : SV_POSITION;
        float4 screenPos : TEXCOORD0;
    };

    v2f vert(appdata_base v)
    {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.screenPos = ComputeScreenPos(o.pos);
        return o;
    }

    // behind = 窗戶後面的場景顏色；每個像素直接覆寫，好幾片玻璃重疊也不會疊色
    half4 Composite(float2 uv, half3 behind)
    {
        float2 portalUV = float2(uv.x, _FlipY > 0.5 ? 1.0 - uv.y : uv.y);
        half4 portal = tex2D(_PortalTex, portalUV);
        half3 color = lerp(behind, portal.rgb * _Tint.rgb, saturate(portal.a));
        color = lerp(color, _GlassColor.rgb, _GlassColor.a);
        return half4(color, 1);
    }
    ENDCG

    // ---------- URP ----------
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "PortalWindow"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite Off
            Blend Off
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _CameraOpaqueTexture;

            half4 frag(v2f i) : SV_Target
            {
                float2 uv = i.screenPos.xy / i.screenPos.w;
                return Composite(uv, tex2D(_CameraOpaqueTexture, uv).rgb);
            }
            ENDCG
        }
    }

    // ---------- Built-in ----------
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }

        // 具名 GrabPass：同一幀所有窗戶共用一次擷取
        GrabPass { "_PortalBehindTex" }

        Pass
        {
            ZWrite Off
            Blend Off
            Cull Back

            CGPROGRAM
            #pragma vertex vertGrab
            #pragma fragment frag

            sampler2D _PortalBehindTex;

            struct v2fGrab
            {
                float4 pos : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float4 grabPos : TEXCOORD1;
            };

            v2fGrab vertGrab(appdata_base v)
            {
                v2fGrab o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.pos);
                o.grabPos = ComputeGrabScreenPos(o.pos);
                return o;
            }

            half4 frag(v2fGrab i) : SV_Target
            {
                float2 uv = i.screenPos.xy / i.screenPos.w;
                return Composite(uv, tex2Dproj(_PortalBehindTex, i.grabPos).rgb);
            }
            ENDCG
        }
    }
}
