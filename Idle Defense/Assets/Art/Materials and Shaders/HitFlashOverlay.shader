Shader "Custom/HitFlashOverlay"
{
    Properties
    {
        _BaseColor     ("Color", Color)           = (1,0.2,0.2,1)
        _FlashIntensity("Flash Intensity", Range(0,1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back


        Pass
        {
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // URP core helpers (gives us TransformObjectToHClip)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            float4 _BaseColor;
            float  _FlashIntensity;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half4 col = _BaseColor;
                col.a = (_FlashIntensity > 0.001) ? 1.0 : 0.0;

                return col;
            }

            ENDHLSL
        }
    }
}
