Shader "FoldCanvas/One-Sided Section Solid"
{
    Properties
    {
        _Color ("Color", Color) = (0.94, 0.66, 0.24, 1)
        _SectionPlaneX ("Object-Space Section X", Float) = 0.003
        _SectionBandWidth ("Section Band Half-Width", Float) = 0.0006
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Cull Back
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _SectionPlaneX;
            float _SectionBandWidth;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 positionOS : TEXCOORD0;
            };

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.positionOS = input.vertex.xyz;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                clip(
                    _SectionBandWidth -
                    abs(input.positionOS.x - _SectionPlaneX));
                return _Color;
            }
            ENDCG
        }
    }

    Fallback Off
}
