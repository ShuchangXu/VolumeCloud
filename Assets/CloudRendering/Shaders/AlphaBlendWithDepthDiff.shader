Shader "Hidden/AlphaBlendWithDepthDiff"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _OpaqueColorTex ("Texture", 2D) = "white" {}
        _OpaqueDepthTex ("Texture",2D) = "white" {}
        _AllDepthTex ("Texture", 2D) = "white" {}
        _OccludeeVisibility ("Visibility of Occluded Objects", Range(0.01, 1)) = 0.35
        _OccludeeVisibleRange ("Visible Range of Occluded Objects", Range(0, 1)) = 0.6
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _MainTex_TexelSize;
            sampler2D _OpaqueColorTex;
            sampler2D _OpaqueDepthTex;
            sampler2D _AllDepthTex;
            float _OccludeeVisibility;
            float _OccludeeVisibleRange;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float depthOpaque01 = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_OpaqueDepthTex, i.uv));
                float depthCloud01 = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_AllDepthTex, i.uv));
                float depthDiff = saturate(depthOpaque01 - depthCloud01);
                fixed alpha = pow(smoothstep(0, _OccludeeVisibleRange * 0.1, depthDiff), _OccludeeVisibility);

                fixed4 opaqueColor = tex2D(_OpaqueColorTex, i.uv);
                fixed4 cloudColor = tex2D(_MainTex, i.uv);
                return lerp(opaqueColor, cloudColor, alpha);
            }
            ENDCG
        }
    }
}
