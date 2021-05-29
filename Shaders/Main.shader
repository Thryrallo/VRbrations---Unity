Shader "VRBrations/Main" {
	Properties{
		_InShaderAudioLinkMultiplier("Audio Link Multiplier",Range(0,2)) = 1
		_StencilRef("ID", Float) = 73
	}
		CustomEditor "Thry.VRBrations.VRCToysUI"
		SubShader{
			Tags { "Queue" = "Overlay+498" "RenderType" = "Overlay" }

			GrabPass
		{
			"_VRbrationsBG"
		}

			Pass {
				Name "FORWARD"
				Tags { "LightMode" = "ForwardBase" }
				Blend SrcAlpha OneMinusSrcAlpha
				Cull Off
				ZTest Always
				ZWrite Off

			Stencil
			{
				Ref [_StencilRef]
				Comp Always
				Pass Replace
			}

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fwdbase
				#pragma only_renderers d3d9 d3d11 glcore gles
				#pragma target 3.0

				#define SENSOR_WIDTH 4
				#define SENSOR_HEIGHT 8

				#define RESOLUTION_WIDTH_SCREEN 4
				#define RESOLUTION_HEIGHT_SCREEN 1

				#define RESOLUTION_DEVISTIONS_X 12
				#define RESOLUTION_DEVISTIONS_Y 2

				#include "UnityCG.cginc"

				uniform float4 _pixelPosition;

				#include "vrbrations.cginc"

				float _InShaderAudioLinkMultiplier;

				sampler2D _VRbrationsBG;

				UNITY_DECLARE_TEX2D(_AudioTexture);

				float4 GetAudioLinkData(inout int isAudioLinkPresent) {
					half testh;
					half testw = testh = 0.;
					_AudioTexture.GetDimensions(testw, testh);

					float uvScaleX = 0.5f / testw;
					float uvScaleY = 0.5f / testh;

					isAudioLinkPresent = testh != 16 && testw != 16;

					float4 audioLink = (float4)0;
					audioLink.x = isAudioLinkPresent * UNITY_SAMPLE_TEX2D(_AudioTexture, float2(uvScaleX, uvScaleY * 1)) * _InShaderAudioLinkMultiplier;
					audioLink.y = isAudioLinkPresent * UNITY_SAMPLE_TEX2D(_AudioTexture, float2(uvScaleX, uvScaleY * 3)) * _InShaderAudioLinkMultiplier;
					audioLink.z = isAudioLinkPresent * UNITY_SAMPLE_TEX2D(_AudioTexture, float2(uvScaleX, uvScaleY * 5)) * _InShaderAudioLinkMultiplier;
					audioLink.w = isAudioLinkPresent * UNITY_SAMPLE_TEX2D(_AudioTexture, float2(uvScaleX, uvScaleY * 7)) * _InShaderAudioLinkMultiplier;
					return audioLink;
				}

			struct VertexInput {
				float4 vertex : POSITION;
				float2 texcoord0 : TEXCOORD0;
			};
			struct VertexOutput {
				float4 pos : SV_POSITION;
				float2 uv0 : TEXCOORD0;
				float4 data : TEXCOORD1;
				float4 grabPos : TEXCOORD2;
			};

			VertexOutput vert(VertexInput v) {
				VertexOutput o = (VertexOutput)0;
				o.uv0 = v.texcoord0;
				o.pos = float4(o.uv0.xy * float2(-2,2) + float2(1,-1) , 0, 1);
				o.grabPos = ComputeGrabScreenPos(o.pos);
				return o;
			}

			float4 frag(VertexOutput i, float facing : VFACE) : COLOR {
				float3 data = (float3)0;
				int resolutionWriterOffset = float(RESOLUTION_WIDTH_SCREEN) / 100 * _ScreenParams.x + 1;
				int2 pixel = int2(floor(i.uv0.x * _ScreenParams.x - resolutionWriterOffset), floor(i.uv0.y * _ScreenParams.y));
				int2 dataIndex = int2(floor((i.uv0.x * _ScreenParams.x - resolutionWriterOffset) / 3), floor(i.uv0.y * _ScreenParams.y));
				int dataSubIndex = uint(floor(i.uv0.x * _ScreenParams.x - resolutionWriterOffset)) % 3;

				int2 resolutionIndex = int2(i.uv0.x / (float(RESOLUTION_WIDTH_SCREEN) / RESOLUTION_DEVISTIONS_X / 100), i.uv0.y / (float(RESOLUTION_HEIGHT_SCREEN) / RESOLUTION_DEVISTIONS_Y / 100));

				int isSensor = (dataIndex.x < SENSOR_WIDTH && dataIndex.y < SENSOR_HEIGHT) || (resolutionIndex.x < RESOLUTION_DEVISTIONS_X && resolutionIndex.y < RESOLUTION_DEVISTIONS_Y);

				//Resolution Color References
				data += (resolutionIndex.x == 0) * (resolutionIndex.y == 0) * float3(0, 0, 0);
				data += (resolutionIndex.x == 1) * (resolutionIndex.y == 0) * float3(0, 0, 1);
				data += (resolutionIndex.x == 2) * (resolutionIndex.y == 0) * float3(0, 1, 0);
				data += (resolutionIndex.x == 3) * (resolutionIndex.y == 0) * float3(0, 1, 1);
				data += (resolutionIndex.x == 4) * (resolutionIndex.y == 0) * float3(1, 0, 0);
				data += (resolutionIndex.x == 5) * (resolutionIndex.y == 0) * float3(1, 0, 1);
				data += (resolutionIndex.x == 6) * (resolutionIndex.y == 0) * float3(1, 1, 0);
				data += (resolutionIndex.x == 7) * (resolutionIndex.y == 0) * float3(1, 1, 1);
				int2 resolutionIndexIntPos = int2(resolutionIndex.x / 3, resolutionIndex.y);
				int resolutionIndexSubIndex = resolutionIndex.x % 3;
				data += EncodeShort(3, 0, 175, resolutionIndexIntPos, resolutionIndexSubIndex);
				data += EncodeInt(0, 1, _ScreenParams.x, resolutionIndexIntPos, resolutionIndexSubIndex);
				data += EncodeInt(2, 1, _ScreenParams.y, resolutionIndexIntPos, resolutionIndexSubIndex);

				//Data Color References
				data += (pixel.x == 0) * (pixel.y == 0) * float3(0, 0, 0);
				data += (pixel.x == 1) * (pixel.y == 0) * float3(0, 0, 1);
				data += (pixel.x == 2) * (pixel.y == 0) * float3(0, 1, 0);
				data += (pixel.x == 3) * (pixel.y == 0) * float3(0, 1, 1);
				data += (pixel.x == 4) * (pixel.y == 0) * float3(1, 0, 0);
				data += (pixel.x == 5) * (pixel.y == 0) * float3(1, 0, 1);
				data += (pixel.x == 6) * (pixel.y == 0) * float3(1, 1, 0);
				data += (pixel.x == 7) * (pixel.y == 0) * float3(1, 1, 1);
				data += EncodeShort(3, 0, 175, dataIndex, dataSubIndex);

				//Audio Link Data
				int isAudioLinkPresent;
				float4 audioLink = GetAudioLinkData(isAudioLinkPresent);
				data += EncodeBool(0, 3, 0, isAudioLinkPresent, dataIndex, dataSubIndex);
				data += EncodeFloat(0, 2, audioLink.x, dataIndex, dataSubIndex);
				data += EncodeFloat(1, 2, audioLink.y, dataIndex, dataSubIndex);
				data += EncodeFloat(2, 2, audioLink.z, dataIndex, dataSubIndex);
				data += EncodeFloat(3, 2, audioLink.w, dataIndex, dataSubIndex);

				//Pixel testing
				//data = float3(floatIndex / _ScreenParams.xy * float2(SENSOR_WIDTH, SENSOR_HEIGHT) * subPixel, 0);

				half4 bgcolor = tex2Dproj(_VRbrationsBG, i.grabPos);
				bgcolor.rgb = bgcolor.rgb / max(1, (max(max(bgcolor.r, bgcolor.g), bgcolor.b)));

				float bloomRemoval = 1 - saturate(float(i.uv0.y * 100 - RESOLUTION_HEIGHT_SCREEN - 5) / 20);
				float4 finalColor = float4(data, 1) * isSensor + float4(bgcolor.rgb, bloomRemoval) * (1 - isSensor);
				
				return finalColor;
			}
			ENDCG
		}
	}
}
