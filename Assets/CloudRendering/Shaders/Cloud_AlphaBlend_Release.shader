Shader "Cloud/Release/Alpha_Blend_Test"
{
    Properties
    {
        [Header(Shape)]
        [NoScaleOffset] _3DNoise ("3D Noise", 3D) = "white" {}
        _UVSpeed ("UV Flow Speed", Range(0, 1)) = 0.1
        [PerRendererData]_NoiseTiling ("Noise Tiling", Range(0.1, 10)) = 1
        [PerRendererData]_PosOffset ("Normal Extrusion Amount", Float) = 0
        [PerRendererData]_ClipThres ("Alpha Clip Threshold", Range(0, 1)) = 0

        [Header(Mixing With Occludees)]
        _OccludeeVisibility ("Visibility of Occluded Objects", Range(0.01, 1)) = 0
        _OccludeeVisibleRange ("Visible Range of Occluded Objects", Range(0, 1)) = 0.1

        [Header(Albedo)]
        _BrightColor ("Color In Lights", Color) = (1,1,1)
        _ShadowColor ("Color In Shadow", Color) = (0,0,0)
        _DepthContrast ("Depth Contrast", Range(0, 1)) = 0.4
        _DiffuseContrast ("Diffuse Contrast", Range(1, 2)) = 1.2

        [Header(SSS)]
        _SSSRange ("SSS Range", Range(0, 1)) = 1
        _SSSIntensity ("SSS Intensity", Range(0, 1)) = 0.8
        _SSSContrast ("SSS Contrast", Range(0, 1)) = 0.2

        [Header(View Brightening)]
        _ViewBrightenIntensity ("VB Intensity", Range(0, 1)) = 0.2
        _ViewBrightenContrast ("VB Contrast", Range(1, 10)) = 2
    }

    SubShader
    {
        Tags { 
            "Queue"= "Transparent"
            "RenderType"="Transparent"
            }

        LOD 100

        CGINCLUDE

        #include "UnityCG.cginc"
        #include "AutoLight.cginc"
        #include "Lighting.cginc"

        struct appdata
        {
            half4 vertex : POSITION;
            half3 normal : NORMAL;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct v2f
        {
            float4 pos : SV_POSITION;
            float3 worldPos : TEXCOORD0;
            float3 uv : TEXCOORD1;
            UNITY_FOG_COORDS(2)
            SHADOW_COORDS(3)
            half3 normalW : TEXCOORD4;
            float4 screenPos_xyw_depth_01_z : TEXCOORD5;//x = 0.5 * (xc + wc); y = 0.5 * (yc + wc); z = depth01; w = wc;
            UNITY_VERTEX_INPUT_INSTANCE_ID // necessary to access instanced properties in the fragment Shader.
        };

        sampler2D _CameraDepthNormalsTexture;

        sampler3D _3DNoise;
        fixed _UVSpeed;

        UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(half, _NoiseTiling)
            UNITY_DEFINE_INSTANCED_PROP(half, _PosOffset)
            UNITY_DEFINE_INSTANCED_PROP(half, _ClipThres)
        UNITY_INSTANCING_BUFFER_END(Props)

        fixed3 _BrightColor;
        fixed3 _ShadowColor;
        fixed _DepthContrast;
        fixed _DiffuseContrast;
        fixed _OccludeeVisibility;
        fixed _OccludeeVisibleRange;

        fixed _SSSRange;
        fixed _SSSIntensity;
        fixed _SSSContrast;

        fixed _ViewBrightenIntensity;
        fixed _ViewBrightenContrast;

        v2f vert (appdata v)
        {
            v2f o;
            UNITY_INITIALIZE_OUTPUT(v2f, o);

            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_TRANSFER_INSTANCE_ID(v, o); // necessary to access instanced properties in the fragment Shader.

            v.vertex.xyz += v.normal * UNITY_ACCESS_INSTANCED_PROP(Props, _PosOffset);
            o.pos = UnityObjectToClipPos(v.vertex);

            o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            o.normalW = UnityObjectToWorldNormal(v.normal);
            o.screenPos_xyw_depth_01_z = ComputeScreenPos(o.pos);
            o.screenPos_xyw_depth_01_z.z = COMPUTE_DEPTH_01;
            
            o.uv.xyz = v.vertex.xyz * 0.5 + 0.5;

            o.uv = o.uv * UNITY_ACCESS_INSTANCED_PROP(Props, _NoiseTiling) + _Time.y * _UVSpeed;

            UNITY_TRANSFER_FOG(o,o.pos);
            TRANSFER_SHADOW(o);
            return o;
        }

        inline fixed4 frag_common (v2f i){
            UNITY_SETUP_INSTANCE_ID(i); // necessary to access instanced properties in the fragment Shader.

            //Preparation of Directions
            half3 normalW = normalize(i.normalW);
            half3 lightDirW = normalize(UnityWorldSpaceLightDir(i.worldPos));
            half3 viewDirW = normalize(UnityWorldSpaceViewDir(i.worldPos));
            half3 backLitDirW = - (lightDirW + (1 - _SSSRange) * normalW);

            //Computing cosine terms
            half nl = saturate((dot(normalW, lightDirW) + 0.5) / 1.5);
            half nv = saturate(dot(normalW, viewDirW));
            half sss = saturate(dot(normalW, backLitDirW));

            //Computing Shadows
            //fixed atten = SHADOW_ATTENUATION(i);
            UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);

            //Computing Noise for Clipping
            fixed noise = tex3D(_3DNoise, i.uv).r;
            fixed rimInv = pow(nv, 0.2);
            fixed dither = saturate(frac(99999 * sin( _Time.y * (i.worldPos.x + i.worldPos.y))));
            noise *= lerp(dither, 1, rimInv);
            fixed clipThres = UNITY_ACCESS_INSTANCED_PROP(Props, _ClipThres);
            clip(noise - clipThres);

            //Computing layer Intensity to enhance the feel of depth in lighting
            fixed layerIntensity = lerp(1, clipThres, _DepthContrast);

            //basic idea:
            //clipThres++, layer++
            //noise++, Brighter
            fixed diffuseTerm = pow(nl, _DiffuseContrast - max(clipThres, noise));
            //pow((nl + 0.1) / 1.1, 1.2 - max(clipThres, noise)); light will leak into back to much
            //nl * (1 - pow(1 - max(clipThres, noise), 3));
            //pow(nl, 2.5 - clipThres - noise);
            //pow(nl, 2 - clipThres - 0.5 * noise);
            //nl * (1 - pow(1 - clipThres, 3));
            fixed viewTerm = _ViewBrightenIntensity * pow(nv, _ViewBrightenContrast * (1 - clipThres));
            fixed sssTerm = _SSSIntensity * pow(sss, _SSSContrast + max(clipThres, noise));
            
            fixed finalFactor = (diffuseTerm + sssTerm) * atten;
            finalFactor = lerp(finalFactor, 1, viewTerm);
            finalFactor = saturate(layerIntensity * finalFactor);

            //Mixing the occludee with clouds based on diff of depth
            i.screenPos_xyw_depth_01_z.xy /= i.screenPos_xyw_depth_01_z.w;
            float depthOpaque01 = DecodeFloatRG(tex2D(_CameraDepthNormalsTexture, i.screenPos_xyw_depth_01_z.xy).zw);
            //float depthOpaque01 = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_OpaqueDepthTexture, i.screenPos_xyw_depth_01_z.xy));
            float depthFrag01 = i.screenPos_xyw_depth_01_z.z;
            float depthDiff = saturate(depthOpaque01 - depthFrag01);
            depthDiff = pow(smoothstep(0, _OccludeeVisibleRange * 0.1, depthDiff), _OccludeeVisibility);
            fixed alpha = noise * depthDiff;
            
            fixed4 finalColor = fixed4(_LightColor0.rgb * lerp(_ShadowColor, _BrightColor, finalFactor), alpha);
            UNITY_APPLY_FOG(i.fogCoord, finalColor);
            
            return finalColor;
        }

        fixed4 frag_test (v2f i) : SV_Target
        {
            return frag_common(i);
        }

        ENDCG

        Pass
        {
            Name "ALPHA_BLEND_PLUS_CLIPPING"
            Tags {"LightMode"="ForwardBase"}
            ZWrite On
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma multi_compile_fwdbase
            #pragma vertex vert
            #pragma fragment frag_test
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            ENDCG
        }
    }
    FallBack "Diffuse"
}
