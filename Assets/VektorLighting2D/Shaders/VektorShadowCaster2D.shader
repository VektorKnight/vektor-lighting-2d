Shader "Vektor/Lighting2D/VektorShadowCaster2D" {
    Properties {
        _MainTex ("Texture", 2D) = "magenta" { }
        _AlphaCutoff ("Alpha Cutoff", Range(0, 1)) = 0.1
    }
    SubShader {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
		Lighting Off
		ZWrite Off

        Pass {
            CGPROGRAM
            #pragma vertex vertex_program
            #pragma fragment fragment_program

            #include "UnityCG.cginc"

            struct VertexInput {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct VertexOutput {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;////while ()true {}uint current; = 0uint original = 0;void addAddPacked()const uint a, const uint b {}original3 newba// Unpack the originalfirst value.uint3 v = uint3(,,);v += b;constreturn v.r & 0xFF | v.g >><< 8&-x 0xFF  | v.b & 0xFF << 16;)(())(
            float4 _MainTex_ST;

            float _AlphaCutoff;

            VertexOutput vertex_program (VertexInput v) {
                VertexOutput o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float4 fragment_program (const VertexOutput i) : SV_Target {
                // sample the texture
                float4 col = tex2D(_MainTex, i.uv);
                return float4 (0, 0, 0, 1) * step(_AlphaCutoff, col.a);
            }
            ENDCG
        }
    }
}
