Shader "FoldCanvas/Two-Sided Unlit Texture"
{
    Properties
    {
        [MainTexture] _MainTex ("Texture", 2D) = "white" {}
        [MainColor] _Color ("Tint", Color) = (1, 1, 1, 1)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Geometry"
            "RenderType" = "Opaque"
        }

        Cull [_Cull]
        ZWrite On

        Pass
        {
            Name "Unlit"

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = UnityObjectToClipPos(input.positionOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            fixed4 Frag(
                Varyings input,
                fixed facing : VFACE) : SV_Target
            {
                float2 readableUv = input.uv;
                if (facing < 0)
                {
                    readableUv.x = 1.0 - readableUv.x;
                }

                return tex2D(_MainTex, readableUv) * _Color;
            }
            ENDCG
        }
    }

    Fallback "Unlit/Texture"
}
