Shader "VRBrations/Sensor" {
    Properties {
        _depthcam ("Sensor Camera", 2D) = "black" {}
		_pixelPosition("Position", Vector) = (0,0,0,0)
		_CameraFOV("Camera FOV", Float) = 59.78

		_InShaderMultiplier("Multiplier",Range(0,2)) = 1

		_OverideDepth("Overide Depth", Vector) = (0,1,0,0)
		_OverideWidth("Overide Width", Vector) = (0,1,0,0)

		[Toggle(GEOM_TYPE_BRANCH)]_CheckPenetratorOrface("Use Penetrator when available",Int) = 0

    }
	CustomEditor "Thry.VRBrations.VRCToysUI"
    SubShader {
        Tags { "Queue"="Overlay+20000" "RenderType"="Overlay"}
        Pass { 
			Tags{ "LightMode" = "ForwardBase" }
            Cull Off
            ZTest Always
            ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma target 3.0

			#pragma shader_feature GEOM_TYPE_BRANCH
			#define TEST_PIXELS 0 //Testing pixels by witing red and green channels to x and y

			#include "UnityCG.cginc"

			#define SENSOR_WIDTH 4
			#define SENSOR_HEIGHT 4
			#define PADDING 0

            uniform sampler2D _depthcam; uniform float4 _depthcam_ST; uniform float4 _depthcam_TexelSize;
            uniform float4 _pixelPosition;
			uniform float _CameraFOV;

			float4 _OverideDepth;
			float4 _OverideWidth;

			float _InShaderMultiplier;

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

			float GetActiveCameraFOV() {
				float t = unity_CameraProjection._m11;
				const float Rad2Deg = 180 / UNITY_PI;
				return atan(1.0f / t) * 2.0 * Rad2Deg;
			}

			float4 TransformToScreenPixel(float2 uv) {
#if defined(USING_STEREO_MATRICES)
				return float4(0, 0, 0, 0);
#else
				float fovFits = abs(GetActiveCameraFOV() - _CameraFOV) < 0.0015;
				float2 sensorSize = 2 * (float2(SENSOR_WIDTH, SENSOR_HEIGHT) + PADDING * 2) * float2(_ScreenParams.z - 1, _ScreenParams.w - 1);
				float2  p = uv * sensorSize - 1.0; //Scaling sensor
				float4 pos = fovFits * float4(p.x + sensorSize.x * _pixelPosition.x, p.y + sensorSize.y * _pixelPosition.y, 0, 1); //moving to pixel position
				pos.y = (_ProjectionParams.x < 0) * pos.y; //flip y if projection is flipped
				return pos;
#endif
			}

            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
				o.worldPos = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
				o.pos = TransformToScreenPixel(o.uv0.xy);

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

				int2 pixel = int2(floor(i.uv0.x * (SENSOR_WIDTH + PADDING * 2) - PADDING), floor(i.uv0.y * (SENSOR_HEIGHT + PADDING * 2) - PADDING));

				//Add data
				data += (pixel.x == 0) * (pixel.y == 0) * float3(0.69, 0.01, 0.69);
				data += (pixel.x == 1) * (pixel.y == 0) * float3(i.data.x, i.data.y, 0);
				data += (pixel.x == 2) * (pixel.y == 0) * float3(i.data.z, i.data.w, 0);

#if TEST_PIXELS
				data = float3(float(pixel.x) / SENSOR_WIDTH, float(pixel.y) / SENSOR_HEIGHT, 0);
#endif
				//Make padded areas black
				data = data * ((pixel.x < SENSOR_WIDTH) & (pixel.y < SENSOR_HEIGHT) & (pixel.x >= 0) & (pixel.y >= 0));
				return float4(data,1);
            }
            ENDCG
        }
    }
}
