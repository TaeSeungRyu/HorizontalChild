Shader "Game/WorldLandVertexColor"
{
    // 정점색을 그대로 출력하는 양면(Cull Off) Unlit 셰이더 (URP).
    // 조명/노멀/뒷면 컬링과 무관하게 색이 보인다 — 세계지도용.
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Cull Off

        Pass
        {
            Name "Unlit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half4 color : COLOR;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                return half4(IN.color.rgb, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/Unlit"
}
