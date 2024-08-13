Shader "Custom/SelectiveEnvironmentLighting"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _UseEnvironmentLight ("Use Environment Light", Float) = 1.0
        _Emission ("Emission", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows nolightmap nodynlightmap noambient novertexlight

        half _UseEnvironmentLight;
        fixed4 _Emission;
        sampler2D _MainTex;
        fixed4 _Color;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;

            if (_UseEnvironmentLight < 0.5)
            {
                // Completely bypass environment lighting effects
                o.Emission = _Emission.rgb;
                o.Albedo = c.rgb;
                o.Metallic = 0;
                o.Smoothness = 0;
                o.Occlusion = 1;

                // Set other properties that can reduce environmental lighting effects
                //o.Glossiness = 0;
                //o.Specular = 0;
            }
            else
            {
                // Use standard lighting including environment
                o.Emission = 0; // No additional emission when using environment lighting
            }
        }
        ENDCG
    }

    FallBack "Diffuse"
}
