Shader "VRBrations/Main" {
	Properties{
		_HeaderTexture("Header Texture", 2D) = "black" {}
		_InShaderAudioLinkMultiplier("Audio Link Multiplier",Range(0,2)) = 1
		_StencilRef("ID", Float) = 73
	}
		CustomEditor "Thry.VRBrations.VRCToysUI"
		SubShader{
			Tags { "Queue" = "Overlay+498" "RenderType" = "Overlay" }

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

				#define HEADER_HEIGHT 15

				#include "UnityCG.cginc"

				uniform float4 _pixelPosition;

				#include "vrbrations.cginc"

				float _InShaderAudioLinkMultiplier;

				sampler2D _HeaderTexture;

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
				o.pos = TransformToScreenMain(o.uv0);
				o.grabPos = ComputeGrabScreenPos(o.pos);
				return o;
			}

			float4 frag(VertexOutput i, float facing : VFACE) : COLOR {
				float3 data = (float3)0;
				int2 pixel = int2(floor(i.uv0.x * 100 / PIXEL_WIDTH), floor(i.uv0.y * 100 / PIXEL_HEIGHT));
				int2 dataIndex = int2(floor(pixel.x / 3), pixel.y);
				int dataSubIndex = uint(pixel.x) % 3;

				int isHeader = i.uv0.y < float(HEADER_HEIGHT) / 100;
				int isSensor = pixel.x < 12 && pixel.y < 7;

				//Data Color References
				data += (pixel.x == 0) * (pixel.y == 3) * float3(0, 0, 0);
				data += (pixel.x == 1) * (pixel.y == 3) * float3(0, 0, 1);
				data += (pixel.x == 2) * (pixel.y == 3) * float3(0, 1, 0);
				data += (pixel.x == 3) * (pixel.y == 3) * float3(0, 1, 1);
				data += (pixel.x == 4) * (pixel.y == 3) * float3(1, 0, 0);
				data += (pixel.x == 5) * (pixel.y == 3) * float3(1, 0, 1);
				data += (pixel.x == 6) * (pixel.y == 3) * float3(1, 1, 0);
				data += (pixel.x == 7) * (pixel.y == 3) * float3(1, 1, 1);
				data += EncodeShort(3, 3, 175, dataIndex, dataSubIndex);

				//Audio Link Data
				int isAudioLinkPresent;
				float4 audioLink = GetAudioLinkData(isAudioLinkPresent);
				data += EncodeBool(0, 2, 0, isAudioLinkPresent, dataIndex, dataSubIndex);
				data += EncodeFloat(0, 1, audioLink.x, dataIndex, dataSubIndex);
				data += EncodeFloat(1, 1, audioLink.y, dataIndex, dataSubIndex);
				data += EncodeFloat(2, 1, audioLink.z, dataIndex, dataSubIndex);
				data += EncodeFloat(3, 1, audioLink.w, dataIndex, dataSubIndex);

				float2 headerUV = saturate(i.uv0 * float2(-4, -400 / float(HEADER_HEIGHT)) + float2(2.5, 3.75));
				float4 header = tex2D(_HeaderTexture, headerUV);
				data += header.rgb * header.a;

				//Pixel testing
				//data = float3(floatIndex / _ScreenParams.xy * float2(SENSOR_WIDTH, SENSOR_HEIGHT) * subPixel, 0);
				data += (isSensor == 0) * float3(0.05f, 0, 0.05f);

				float4 finalColor = float4(data * 0.5f, isHeader);
				
				return finalColor;
			}
			ENDCG
		}
	}
}
