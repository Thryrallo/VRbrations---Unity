Shader "VRBrations/Main" {
	Properties{
		_CameraFOV("Camera FOV", Float) = 59.78
		_InShaderAudioLinkMultiplier("Audio Link Multiplier",Range(0,2)) = 1
	}
		CustomEditor "Thry.VRBrations.VRCToysUI"
		SubShader{
			Tags { "Queue" = "Overlay+20000" "RenderType" = "Overlay" }
			Pass {
				Name "FORWARD"
				Tags { "LightMode" = "ForwardBase" }
				Blend SrcAlpha OneMinusSrcAlpha
				Cull Off
				ZTest Always
				ZWrite Off

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fwdbase
				#pragma only_renderers d3d9 d3d11 glcore gles
				#pragma target 3.0

				#include "UnityCG.cginc"

				#define SCALE 0.02

				#define X_COUNT 10

				float _InShaderAudioLinkMultiplier;
			uniform float _CameraFOV;

			struct VertexInput {
				float4 vertex : POSITION;
				float2 texcoord0 : TEXCOORD0;
			};
			struct VertexOutput {
				float4 pos : SV_POSITION;
				float2 uv0 : TEXCOORD0;
				float3 worldPos : TEXCOOORD1;
				float4 data : TEXCOORD2;
			};

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
				float4 pos = fovFits * float4(1 - (uv.x * SCALE * X_COUNT * 2), -1 + (uv.y * SCALE * 2), 0, 1);
				pos.y = (_ProjectionParams.x < 0) * pos.y; //flip y if projection is flipped
				return pos;
#endif
			}

			VertexOutput vert(VertexInput v) {
				VertexOutput o = (VertexOutput)0;
				o.uv0 = v.texcoord0;
				o.worldPos = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
				o.pos = TransformToScreenPixel(o.uv0.xy);

				return o;
			}

			UNITY_DECLARE_TEX2D(_AudioTexture);

			float4 frag(VertexOutput i, float facing : VFACE) : COLOR {
				float3 data = (float3)0;
				int index = (int)(i.uv0.x * X_COUNT);

				half testh;
				half testw = testh = 0.;
				_AudioTexture.GetDimensions(testw, testh);

				float uvScaleX = 0.5f / testw;
				float uvScaleY = 0.5f / testh;

				float audioLinkPresent = testh != 16 && testw != 16;

				float4 audioLink = (float4)0;
				audioLink.x = audioLinkPresent * UNITY_SAMPLE_TEX2D(_AudioTexture, float2(uvScaleX, uvScaleY * 1)) * _InShaderAudioLinkMultiplier;
				audioLink.y = audioLinkPresent * UNITY_SAMPLE_TEX2D(_AudioTexture, float2(uvScaleX, uvScaleY * 3)) * _InShaderAudioLinkMultiplier;
				audioLink.z = audioLinkPresent * UNITY_SAMPLE_TEX2D(_AudioTexture, float2(uvScaleX, uvScaleY * 5)) * _InShaderAudioLinkMultiplier;
				audioLink.w = audioLinkPresent * UNITY_SAMPLE_TEX2D(_AudioTexture, float2(uvScaleX, uvScaleY * 7)) * _InShaderAudioLinkMultiplier;

				data += (index == 0) * float3(0.69, 0.01, 0.69);
				data.xyz += (index == 1) * float3(audioLink.x, audioLink.y, audioLinkPresent);
				data.xy += (index == 2) * audioLink.zw;

				return float4(data,1);
			}
			ENDCG
		}
	}
}