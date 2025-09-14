Shader "UI/ScreenBlurCorrect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurSize ("Blur Size", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _BlurSize;
            float4 _MainTex_ST;

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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 texel = float2(_BlurSize/_ScreenParams.x, _BlurSize/_ScreenParams.y); // convert to UV offset
                float4 sum = float4(0,0,0,0);

                sum += tex2D(_MainTex, i.uv + texel * float2(-1,-1)) * 0.0625;
                sum += tex2D(_MainTex, i.uv + texel * float2(0,-1)) * 0.125;
                sum += tex2D(_MainTex, i.uv + texel * float2(1,-1)) * 0.0625;

                sum += tex2D(_MainTex, i.uv + texel * float2(-1,0)) * 0.125;
                sum += tex2D(_MainTex, i.uv) * 0.25;
                sum += tex2D(_MainTex, i.uv + texel * float2(1,0)) * 0.125;

                sum += tex2D(_MainTex, i.uv + texel * float2(-1,1)) * 0.0625;
                sum += tex2D(_MainTex, i.uv + texel * float2(0,1)) * 0.125;
                sum += tex2D(_MainTex, i.uv + texel * float2(1,1)) * 0.0625;

                return sum;
            }
            ENDCG
        }
    }
}
