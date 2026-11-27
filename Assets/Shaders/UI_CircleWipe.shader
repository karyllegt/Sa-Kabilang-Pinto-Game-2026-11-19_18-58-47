// Circle wipe for screen transitions: black everywhere outside a circle, with a stepped (pixel-art) edge.
// Used by Skp.UI.CircleWipe on an overlay canvas. _Center and _Radius are in screen pixels.
Shader "Skp/UI/CircleWipe"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Color", Color) = (0.039, 0.027, 0.063, 1)
        _Center ("Center (px)", Vector) = (0, 0, 0, 0)
        _Radius ("Radius (px)", Float) = 100000
        _Pixel ("Edge pixel size (px)", Float) = 4
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float4 screen : TEXCOORD0; fixed4 color : COLOR; };
            fixed4 _Color; float4 _Center; float _Radius; float _Pixel;
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.screen = ComputeScreenPos(o.pos);
                o.color = v.color;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                float2 p = i.screen.xy / i.screen.w * _ScreenParams.xy;
                float inside;
                if (_Pixel > 0)
                {
                    p = floor(p / _Pixel) * _Pixel + _Pixel * 0.5;
                    inside = step(distance(p, _Center.xy), _Radius);
                }
                else
                {
                    inside = 1 - smoothstep(_Radius - 0.75, _Radius + 0.75, distance(p, _Center.xy));
                }
                return fixed4(_Color.rgb, _Color.a * i.color.a * (1 - inside));
            }
            ENDCG
        }
    }
}
