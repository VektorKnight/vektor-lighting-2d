Shader "VektorLighting2D/RaySplat"
{
    Properties { }
    SubShader {
        Pass {
            Cull Back
            Lighting Off
            Zwrite Off
            Blend OneMinusDstColor One
        
            Tags
            {
		    	"RenderType" = "Transparent"
		    	"Queue" = "Transparent"
		    	"IgnoreProjector" = "True"
            }
            
            CGPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            #include "RayMarchCommon.cginc"

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float4 color: COLOR;
            };

            StructuredBuffer<Splat> RaySplats;

            uniform float _VektorLightScale;
            
            v2f vert (const uint id : SV_InstanceID) {
                v2f o;

                const Splat splat = RaySplats[id];
                const uint2 pixel = UnpackPixelID(splat.pixel_id);
                const float3 color = splat.color;

                const float2 pixel_norm = float2(
                    pixel.x * 2 / (_ScreenParams.x),
                    pixel.y * 2 / (_ScreenParams.y)
                );

                o.vertex = float4(pixel_norm.x * 2.0 - 1.0, -(pixel_norm.y * 2.0 - 1.0), 0.0, 1.0);
                o.color = float4(color, 1.0);
                
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target {
                return i.color;
            }
            ENDCG
        }
    }
}