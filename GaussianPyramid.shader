Shader "Custom/Blur" {
	Properties{
		_MainTex("Texture", 2D) = "white" {}
		_SourceTex("Texture", 2D) = "white" {}
		_Jump("Jump", Range(0, 1024)) = 0
	}

		CGINCLUDE
			#include "UnityCG.cginc"

			sampler2D _MainTex, _SourceTex;
			float4 _MainTex_TexelSize;
			float _Jump;

			struct VertexData {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Interpolators {
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			Interpolators VertexProgram(VertexData v) {
				Interpolators i;
				i.pos = UnityObjectToClipPos(v.vertex);
				i.uv = v.uv;
				return i;
			}

			half3 Sample(float2 uv) {
				return tex2D(_MainTex, uv).rgb;
			}

			half3 SampleBox(float2 uv, float delta) {
				float4 o = _MainTex_TexelSize.xyxy * float2(-delta, delta).xxyy;
				half3 s =
					Sample(uv + o.xy) + Sample(uv + o.zy) +
					Sample(uv + o.xw) + Sample(uv + o.zw);
				return s * 0.25f;
			}

			half3 SampleGauss(float2 uv, float jump, float2 dim) {
				// K = [0.05, 0.25, 0.40, 0.25, 0.05];
				float2 o = _MainTex_TexelSize.xy * float2(jump, jump) * dim;
				half3 s =
					Sample(uv - o * 2) * 0.05 +
					Sample(uv - o * 1) * 0.25 +
					Sample(uv) * 0.40 +
					Sample(uv + o * 1) * 0.25 +
					Sample(uv + o * 2) * 0.05;;
				return s;
			}

			float Remap(float x, float minIn, float maxIn, float minOut, float maxOut)
			{
				return (x - minIn) / (maxIn - minIn)*(maxOut - minOut) + minOut;
			}

			float3 rgb2Yuv(float3 rgb)
			{
				float3x3 m_rgb2yuv = float3x3(	0.299f, 0.587f, 0.114f, 
												-0.147f, -0.289f, 0.437f, 
												0.615f, -0.515f, -0.100f);
				float3 yuv = mul(m_rgb2yuv,rgb);
				return yuv;
			}

			float3 yuv2rgb(float3 yuv)
			{
				float3x3 m_yuv2rgb = float3x3(	1.000f, 0.000f, 1.13983f,
												1.000f, -0.39465f, -0.58060f,
												1.000f, 2.03211f, 0.000f);
				float3 rgb = mul(m_yuv2rgb, yuv);
				return rgb;
			}

		ENDCG


		SubShader{
			Cull Off
			ZTest Always
			ZWrite Off
			
			Pass { 
				// Downsample pass 0
				CGPROGRAM
					#pragma vertex VertexProgram
					#pragma fragment FragmentProgram
				
					half4 FragmentProgram(Interpolators i) : SV_Target {
						return half4(SampleBox(i.uv, 1), 1);
					}
				ENDCG
			}

			Pass {
				// Upsample pass 1
				CGPROGRAM
					#pragma vertex VertexProgram
					#pragma fragment FragmentProgram

					half4 FragmentProgram(Interpolators i) : SV_Target {
						return half4(SampleBox(i.uv, 0.5), 1);
					}
				ENDCG
			}

			Pass {
				// Difference pass 2
				CGPROGRAM
					#pragma vertex VertexProgram
					#pragma fragment FragmentProgram

					half4 FragmentProgram(Interpolators i) : SV_Target {
						half4 c = tex2D(_SourceTex, i.uv);
						c.rgb += SampleBox(i.uv, 0.5);
						return c;
					}
				ENDCG
			}

			Pass {
				// Gaussian Blur X pass 3
				CGPROGRAM
					#pragma vertex VertexProgram
					#pragma fragment FragmentProgram
				
					half4 FragmentProgram(Interpolators i) : SV_Target {
						return half4(SampleGauss(i.uv, _Jump, float2(1,0)), 1);
					}
				ENDCG
			}

			Pass {
				// Gaussian Blur Y pass 4
				CGPROGRAM
					#pragma vertex VertexProgram
					#pragma fragment FragmentProgram

					half4 FragmentProgram(Interpolators i) : SV_Target {
						return half4(SampleGauss(i.uv, _Jump, float2(0,1)), 1);
					}
				ENDCG
				}

			Pass {
				// RGB to luminance pass 5
				CGPROGRAM
					#pragma vertex VertexProgram
					#pragma fragment FragmentProgram

					half4 FragmentProgram(Interpolators i) : SV_Target {
						/*half3 y = dot(Sample(i.uv), half3(0.212656, 0.715158, 0.072186));
						return clamp(half4(y, 1),0,1);*/

						float y = rgb2Yuv(Sample(i.uv)).r;
						return clamp(half4(y, y, y, 1), 0, 1);
					}
				ENDCG
				}
			
			Pass {
				// LaPlace Difference 6
				CGPROGRAM
					#pragma vertex VertexProgram
					#pragma fragment FragmentProgram

					half4 FragmentProgram(Interpolators i) : SV_Target {
						half3 c0 = Sample(i.uv);
						half3 c1 = tex2D(_SourceTex, i.uv).rgb;						
						return half4(c0 - c1, 1);
					}
				ENDCG
				}

			Pass {
				// LaPlace Sumation 7
				CGPROGRAM
					#pragma vertex VertexProgram
					#pragma fragment FragmentProgram

					half4 FragmentProgram(Interpolators i) : SV_Target {
						half3 c0 = Sample(i.uv);
						half3 c1 = tex2D(_SourceTex, i.uv).rgb;
						return half4(c0 + c1, 1);
					}
				ENDCG
				}

			Pass{
				// Y to RGB conversion Sumation 8
				CGPROGRAM
					#pragma vertex VertexProgram
					#pragma fragment FragmentProgram

					half4 FragmentProgram(Interpolators i) : SV_Target {
						half3 rgbIn = Sample(i.uv);
						half3 yuvIn = rgb2Yuv(rgbIn);
						half yOut = tex2D(_SourceTex, i.uv).r;
						half3 rgbOut = yuv2rgb(float3(yOut, yuvIn.g, yuvIn.b));

						/*return half4(yuvIn.r, yuvIn.g, yuvIn.b, 1);*/
						return half4(rgbOut, 1);
					}
				ENDCG
				}
	}
}