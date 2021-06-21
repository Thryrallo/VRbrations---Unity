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

				int isHeader = pixel.y < SENSOR_HEIGHT;
				int isSensor = pixel.x < SENSOR_WIDTH && pixel.y < SENSOR_HEIGHT;

				//Pixel wise representation:
				// CheckValue1 | CheckValue2 |
				// Data.x      | Data.y      | Data.z    | Data.w
				//             |             |           |
				// text12      | text34      | text56    | text78
				// text910     | text1112    | text1314  | text1516

				//Data Color References
				data += EncodeShort(0, 0, 175, pixel);
				data += EncodeShort(1, 0, 69, pixel);

				//Audio Link Data
				int isAudioLinkPresent;
				float4 audioLink = GetAudioLinkData(isAudioLinkPresent);
				data += EncodeBool(2, 0, isAudioLinkPresent, pixel);
				data += EncodeFloat(0, 1, audioLink.x, pixel);
				data += EncodeFloat(1, 1, audioLink.y, pixel);
				data += EncodeFloat(2, 1, audioLink.z, pixel);
				data += EncodeFloat(3, 1, audioLink.w, pixel);

				//For testing color accuracy
				/*data += (pixel.y == 1) * (pixel.x == 0) * float3(0.0f,0.1f,0.2f);
				data += (pixel.y == 1) * (pixel.x == 1) * float3(0.3f,0.4f,0.5f);
				data += (pixel.y == 1) * (pixel.x == 2) * float3(0.6f,0.7f,0.8f);
				data += (pixel.y == 1) * (pixel.x == 3) * float3(0.9f,1.0f,1.0f);*/

				float2 headerUV = saturate((i.uv0 + float2(-1,0)) * float2(25 / (SENSOR_WIDTH * PIXEL_WIDTH), -100 / (SENSOR_HEIGHT * PIXEL_HEIGHT)) + float2(1,1));
				float4 header = tex2D(_HeaderTexture, headerUV);
				data += header.rgb * header.a;

				//Pixel testing
				//data = float3(floatIndex / _ScreenParams.xy * float2(SENSOR_WIDTH, SENSOR_HEIGHT) * subPixel, 0);
				data += (isSensor == 0) * float3(0.05f, 0, 0.05f);

				float4 finalColor = float4(data, isHeader);
				
				return finalColor;
			}
			ENDCG
		}
	}
}
