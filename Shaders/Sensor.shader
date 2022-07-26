Shader "VRBrations/Sensor" {
    Properties {
        _depthcam ("Sensor Camera", 2D) = "black" {}
		_pixelPosition("Position", Vector) = (0,0,0,0)

		_InShaderMultiplier("Multiplier",Range(0,2)) = 1

		_OverideDepth("Overide Depth", Vector) = (0,1,0,0)
		_OverideWidth("Overide Width", Vector) = (0,1,0,0)

		_StencilRef("ID", Float) = 73

		_Text0("", Color) = (0,0,0,1)
		_Text1("", Color) = (0,0,0,1)
		_Text2("", Color) = (0,0,0,1)
		_Text3("", Color) = (0,0,0,1)
		_Text4("", Color) = (0,0,0,1)
		_Text5("", Color) = (0,0,0,1)
		_Text6("", Color) = (0,0,0,1)
		_Text7("", Color) = (0,0,0,1)

		[Toggle(GEOM_TYPE_BRANCH)]_CheckPenetratorOrface("Use Penetrator when available",Int) = 0

    }
	CustomEditor "Thry.VRBrations.VRCToysUI"
    SubShader {
        Tags { "Queue"="Overlay+499" "RenderType"="Overlay"}
        Pass { 
			Tags{ "LightMode" = "ForwardBase" }
            Cull Off
            ZTest Always
            ZWrite Off

			Stencil
			{
				Ref [_StencilRef]
				Comp Equal
				Pass Zero
			}
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma target 3.0

			#pragma shader_feature GEOM_TYPE_BRANCH
			#define TEST_PIXELS 0 //Testing pixels by witing red and green channels to x and y

			#include "UnityCG.cginc"

            uniform sampler2D _depthcam; uniform float4 _depthcam_ST; uniform float4 _depthcam_TexelSize;
            uniform float4 _pixelPosition;

			float4 _OverideDepth;
			float4 _OverideWidth;

			float _InShaderMultiplier;

			float4 _Text0;
			float4 _Text1;
			float4 _Text2;
			float4 _Text3;
			float4 _Text4;
			float4 _Text5;
			float4 _Text6;
			float4 _Text7;

			#include "vrbrations.cginc"

            struct VertexInput {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				uint vertexId : SV_VertexID;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
				float4 data : TEXCOORD1;
				float3 worldPos : TEXCOOORD2;
            };
			float4 GetCameraData(sampler2D tex, float2 size) {
				float depth = 0;
				float width = 0;
				float2 middle = 0;
				float uv_jumpX = 1.0 / size.x;
				float uv_jumpY = 1.0 / size.y;
				for (int x = 0; x < size.x; x++) {
					for (int y = 0; y < size.y; y++) {
						float2 uv = float2(uv_jumpX * x, uv_jumpY*y);
						float val = tex2Dlod(tex, float4(uv.x , uv.y, 0, 0));
						if (val > 0) {
							width += 1;
							middle.x += uv.x;
							middle.y += uv.y;
						}
						if (val > depth)
							depth = val;
					}
				}
				if(width == 0)
					return float4(depth, width / (size.x*size.y), 0, 0);
				return float4(depth,width/(size.x*size.y),middle.x/ width,middle.y/ width);
			}

#ifdef GEOM_TYPE_BRANCH
			#define DPS_HOLE_ID 0.01
			#define DPS_RING_ID 0.02
			#define DPS_NORMAL_ID 0.05
			#define DPS_PENETRATOR_ID 0.09
			int GetBestLights(inout float3 orificePositionTracker, inout float3 penetratorPositionTracker, inout float penetratorLength) {
				int foundOrfP = 0;
				int foundOrfN = 0;
				int foundPen = 0;
				for (int i = 0; i < 4; i++) {
					float range = (0.005 * sqrt(1000000 - unity_4LightAtten0[i])) / sqrt(unity_4LightAtten0[i]);
					if (length(unity_LightColor[i].rgb) < 0.01) {
						if (abs(fmod(range, 0.1) - DPS_HOLE_ID) < 0.005) {
							orificePositionTracker = float3(unity_4LightPosX0[i], unity_4LightPosY0[i], unity_4LightPosZ0[i]);
							foundOrfP = 1;
						}
						if (abs(fmod(range, 0.1) - DPS_RING_ID) < 0.005) {
							orificePositionTracker = float3(unity_4LightPosX0[i], unity_4LightPosY0[i], unity_4LightPosZ0[i]);
							foundOrfP = 1;
						}
						if (abs(fmod(range, 0.1) - DPS_NORMAL_ID) < 0.005) {
							foundOrfN = 1;
						}
						if (abs(fmod(range, 0.1) - DPS_PENETRATOR_ID) < 0.005) {
							float3 tempPenetratorPositionTracker = penetratorPositionTracker;
							penetratorPositionTracker = float3(unity_4LightPosX0[i], unity_4LightPosY0[i], unity_4LightPosZ0[i]);
							penetratorLength = distance(penetratorPositionTracker, mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz);
							if (distance(penetratorPositionTracker,mul(unity_ObjectToWorld,float4(0,0,0,1)).xyz) > length(tempPenetratorPositionTracker)) penetratorPositionTracker = tempPenetratorPositionTracker;
							else penetratorLength = unity_LightColor[i].a;
							foundPen = 1;
						}
					}
				}
				return foundOrfP * foundOrfN * foundPen;
			}
#endif
			void DoPenetratorStrength(inout float4 data) {
#ifdef GEOM_TYPE_BRANCH
				float penetratorLength = 0.1;
				float3 orificePositionTracker = float3(0, 0, -100);
				float3 penetratorPositionTracker = float3(0, 0, 100);

				int found = GetBestLights( orificePositionTracker, penetratorPositionTracker, penetratorLength);
				float penetratorDistance = distance(orificePositionTracker, penetratorPositionTracker);

				float dpsStrength = saturate( (penetratorLength - penetratorDistance) / penetratorLength);

				data.x = found * dpsStrength + (found ^ 1) * data.x;
#endif
			}

            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
				o.worldPos = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
				o.pos = TransformToScreenSensor(o.uv0.xy);

				o.data = GetCameraData(_depthcam, _depthcam_TexelSize.zw);
				DoPenetratorStrength(o.data);

				//Add overides
				o.data.x = _OverideDepth.x * _OverideDepth.y + (int(_OverideDepth.x) ^ 1) * o.data.x;
				o.data.y = _OverideWidth.x * _OverideWidth.y + (int(_OverideWidth.x) ^ 1) * o.data.y;

				o.data.x *= _InShaderMultiplier;
				o.data.y *= _InShaderMultiplier;

                return o;
            }
			
            float4 frag(VertexOutput i, float facing : VFACE) : COLOR {
				float3 data = (float3)0;
				int2 pixel = int2(floor(i.uv0.x * SENSOR_WIDTH), floor(i.uv0.y * SENSOR_HEIGHT));

				//Pixel wise representation:
				// CheckValue1 | CheckValue2 |
				// Data.x      | Data.y      | Data.z    | Data.w
				//             |             |           |
				// text12      | text34      | text56    | text78
				// text910     | text1112    | text1314  | text1516

				//Data Color References
				data += EncodeShort(0, 0, 175, pixel);
				data += EncodeShort(1, 0, 69, pixel);

				//Add data
				data += EncodeFloat(0, 1, i.data.x, pixel);
				data += EncodeFloat(1, 1, i.data.y, pixel);
				data += EncodeFloat(2, 1, i.data.z, pixel);
				data += EncodeFloat(3, 1, i.data.w, pixel);

				//text
				data += (pixel.y == 3) * (pixel.x == 0) * _Text0;
				data += (pixel.y == 3) * (pixel.x == 1) * _Text1;
				data += (pixel.y == 3) * (pixel.x == 2) * _Text2;
				data += (pixel.y == 3) * (pixel.x == 3) * _Text3;
				data += (pixel.y == 4) * (pixel.x == 0) * _Text4;
				data += (pixel.y == 4) * (pixel.x == 1) * _Text5;
				data += (pixel.y == 4) * (pixel.x == 2) * _Text6;
				data += (pixel.y == 4) * (pixel.x == 3) * _Text7;

#if TEST_PIXELS
				data = float3(float(pixel.x) / SENSOR_WIDTH, float(pixel.y) / SENSOR_HEIGHT, 0);
#endif
				return float4(data,1);
            }
            ENDCG
        }
    }
}
