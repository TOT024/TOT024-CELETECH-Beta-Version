Shader "CMC/UniversalCamoShader_ApparelMasked_Double"
{
    Properties
    {
        _MainTex("Base (RGB)", 2D) = "white" {}
        _CamoTex("Camo Pattern", 2D) = "white" {} 
        _ApparelMask("Apparel Mask", 2D) = "black" {} 
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.5

        _ColorOne("ColorOne", Color) = (1,1,1,1)
        _ColorTwo("ColorTwo", Color) = (1,1,1,1)
        _ColorThree("ColorThree", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "IgnoreProjector"="True" }
        LOD 200
        Pass
        {
            Name "FORWARD"
            Tags { "LightMode"="ForwardBase" }
            Blend SrcAlpha OneMinusSrcAlpha 

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR; 
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uvCamo : TEXCOORD1;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            sampler2D _CamoTex;
            float4 _CamoTex_ST;
            sampler2D _ApparelMask; 

            float4 _ColorOne;
            float4 _ColorTwo;
            float4 _ColorThree;
            fixed _Cutoff;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv; 
                o.uvCamo = TRANSFORM_TEX(v.uv, _CamoTex); 
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 mainColor = tex2D(_MainTex, i.uv);

                clip(mainColor.a - _Cutoff);

                fixed4 appMask = tex2D(_ApparelMask, i.uv);
                fixed4 camoData = tex2D(_CamoTex, i.uvCamo);
                
                float u = camoData.r;
                float v = camoData.g;
                float w = camoData.b;
                float x = 1 - u - v - w;
                float4 camoColor = _ColorOne * u + _ColorTwo * v + _ColorThree * w + float4(1,1,1,1) * x;
                float maskFactor = appMask.r;
                float3 finalRGB = lerp(mainColor.rgb, camoColor.rgb * mainColor.rgb, maskFactor);
                return fixed4(finalRGB, mainColor.a) * i.color;
            }
            ENDCG
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

            struct v2f { 
                V2F_SHADOW_CASTER;
                float2 uv : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed _Cutoff;

            v2f vert(appdata_base v)
            {
                v2f o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                o.uv = v.texcoord;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                clip(tex.a - _Cutoff);

                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    FallBack Off
}