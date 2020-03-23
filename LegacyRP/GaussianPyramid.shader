Shader "Custom/Blur" {
	Properties{
		_MainTex("Texture", 2D) = "white" {}
		_SourceTex("Texture", 2D) = "white" {}
		_Jump("Jump", Range(0, 1024)) = 0

		_Level0("GuassianLevel0", 2D) = "red" {}
		_Level1("GuassianLevel1", 2D) = "green" {}
		_Level2("GuassianLevel2", 2D) = "blue" {}
		_Level3("GuassianLevel3", 2D) = "white" {}

		_CSFLut("CSFLut", 2D) = "white" {}
		_Rho("Rho", Range(0.1, 32)) = 24
		_LumSource("SourceLum", Range(0.001, 300)) = 90
		_LumTarget("TergetLum", Range(0.001, 300)) = 9
	}

		CGINCLUDE
			#include "UnityCG.cginc"

			sampler2D _MainTex, _SourceTex;
			sampler2D _CSFLut;

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

			float3 Sample(float2 uv) {
				return tex2D(_MainTex, uv).rgb;
			}

			half3 SampleBox(float2 uv, float delta) {
				float4 o = _MainTex_TexelSize.xyxy * float2(-delta, delta).xxyy;
				half3 s =
					Sample(uv + o.xy) + Sample(uv + o.zy) +
					Sample(uv + o.xw) + Sample(uv + o.zw);
				return s * 0.25f;
			}

			half3 SampleGauss1D(float2 uv, float jump, float2 dim) {
				// K = [0.05, 0.25, 0.40, 0.25, 0.05];
				float2 o = _MainTex_TexelSize.xy * float2(jump, jump) * dim * 0.5;
				float s =
					tex2D(_MainTex, uv - o * 2	).r * 0.05 +
					tex2D(_MainTex, uv - o		).r * 0.25 +
					tex2D(_MainTex, uv			).r * 0.4 +
					tex2D(_MainTex, uv + o		).r * 0.25 +
					tex2D(_MainTex, uv + o * 2	).r * 0.05;
				return s;
			}

			half3 SampleGauss2D(float2 uv, float jump) {
				// K = [0.05, 0.25, 0.40, 0.25, 0.05];
				float2 o = _MainTex_TexelSize.xy * float2(jump, jump) * 0.5;
				float s =

					tex2D(_MainTex, uv + o * float2(-2, 2)).r * 0.0025 +
					tex2D(_MainTex, uv + o * float2(-1, 2)).r * 0.0125 +
					tex2D(_MainTex, uv + o * float2(0, 2)).r * 0.02 +
					tex2D(_MainTex, uv + o * float2(1, 2)).r * 0.0125 +
					tex2D(_MainTex, uv + o * float2(2, 2)).r * 0.0025 +

					tex2D(_MainTex, uv + o * float2(-2, 1)).r * 0.0125 +
					tex2D(_MainTex, uv + o * float2(-1, 1)).r * 0.0625 +
					tex2D(_MainTex, uv + o * float2(0, 1)).r * 0.1 +
					tex2D(_MainTex, uv + o * float2(1, 1)).r * 0.0625 +
					tex2D(_MainTex, uv + o * float2(2, 1)).r * 0.0125 +

					tex2D(_MainTex, uv + o * float2(-2, 0)).r * 0.02 +
					tex2D(_MainTex, uv + o * float2(-1, 0)).r * 0.1 +
					tex2D(_MainTex, uv + o * float2(0, 0)).r * 0.16 +
					tex2D(_MainTex, uv + o * float2(1, 0)).r * 0.1 +
					tex2D(_MainTex, uv + o * float2(2, 0)).r * 0.02 +

					tex2D(_MainTex, uv + o * float2(-2, -1)).r * 0.0125 +
					tex2D(_MainTex, uv + o * float2(-1, -1)).r * 0.0625 +
					tex2D(_MainTex, uv + o * float2(0, -1)).r * 0.1 +
					tex2D(_MainTex, uv + o * float2(1, -1)).r * 0.0625 +
					tex2D(_MainTex, uv + o * float2(2, -1)).r * 0.0125 +

					tex2D(_MainTex, uv + o * float2(-2, -2)).r * 0.0025 +
					tex2D(_MainTex, uv + o * float2(-1, -2)).r * 0.0125 +
					tex2D(_MainTex, uv + o * float2(0, -2)).r * 0.02 +
					tex2D(_MainTex, uv + o * float2(1, -2)).r * 0.0125 +
					tex2D(_MainTex, uv + o * float2(2, -2)).r * 0.0025;

					
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

			float Remap01(float x, float minIn, float maxIn)
			{
				return (x - minIn) / (maxIn - minIn);
			}

			float2 RhoAndLogLumToCSFCoordinates(float rho, float logLum)
			{
				float x = Remap01(rho, 1, 32); // values in the LUT are generated for frequencies 1:32
				float y = Remap01(logLum, -5, 3); // values in the LUT are generated for range -5:3
				return float2(x, y);
			}

			float SampleCSF(float rho, float logLum)
			{
				float2 uv = RhoAndLogLumToCSFCoordinates(rho, logLum);
				return tex2D(_CSFLut, uv).r;
			}

			float KulikowskiBoostG(float l_in, float G_in, float l_out, float rho)
			{
				float G_ts = SampleCSF(rho, l_in);
				float G_td = SampleCSF(rho, l_out);
				return max(G_in - G_ts + G_td, 0.00000001f) / G_in;
			}

		ENDCG

		// -------------------   PASSES   ------------------------------

		SubShader{
			Cull Off
			ZTest Always
			ZWrite Off
			
			Pass {
				// Main pass 0
				CGPROGRAM
					#pragma vertex VertexProgram
					#pragma fragment FragmentProgram	

					sampler2D _Level0, _Level1, _Level2, _Level3;
					float _LumSource, _LumTarget, _Rho;

					float4 FragmentProgram(Interpolators i) : SV_Target {
											   
						float g0 = tex2D(_Level0, i.uv).r;
						float g1 = tex2D(_Level1, i.uv).r;
						float g2 = tex2D(_Level2, i.uv).r;
						float g3 = tex2D(_Level3, i.uv).r;
						
						float P_in[4];

						P_in[0] = g0 - g1;
						P_in[1] = g1 - g2;
						P_in[2] = g2 - g3;
						P_in[3] = g3;
						
						float l_in = g3;
						float l_out = g3;

						for (int iter = 2; iter >= 0; iter--)
						{
							float C_in = P_in[iter];

							float l_source = log10(pow(10, l_in) * _LumSource);
							float l_target = log10(pow(10, l_out) * _LumTarget);

							float G_est = abs(C_in);

							float rho = _Rho;
							float m = min(KulikowskiBoostG(l_source, G_est, l_target, rho), 2);

							float C_out = C_in * m;

							l_out = l_out + C_out;
							l_in = l_in + P_in[iter];
						}
											   
						float3 y_out = pow(10, l_out);

						float3 yuvOut = tex2D(_MainTex, i.uv).rgb;
						yuvOut.r = y_out;

						return float4(yuv2rgb(yuvOut), 1);
					}
				ENDCG
			}
			
			Pass {
				// PreviewLog 1
				CGPROGRAM
					#pragma vertex VertexProgram
					#pragma fragment FragmentProgram

					float4 FragmentProgram(Interpolators i) : SV_Target {

						float l = tex2D(_MainTex, i.uv).r;
						float y = pow(10, l);
						return float4(y,y,y,1);
					}
				ENDCG
			}

			Pass {
				// RGB to YUVL 2
				CGPROGRAM
					#pragma vertex VertexProgram
					#pragma fragment FragmentProgram

					float4 FragmentProgram(Interpolators i) : SV_Target {

						float3 rgbClamped = clamp(Sample(i.uv), 0.01, 1);
						float3 yuv = rgb2Yuv(rgbClamped);
						return float4(yuv.rgb, log10(yuv.r));
					}
				ENDCG
			}

			Pass {
				// YUVL to L 3
				CGPROGRAM
					#pragma vertex VertexProgram
					#pragma fragment FragmentProgram

					float FragmentProgram(Interpolators i) : SV_Target {
						return tex2D(_MainTex, i.uv).w;
					}
				ENDCG
			}

			Pass {
				// Gaussian Blur X pass 4
				CGPROGRAM
					#pragma vertex VertexProgram
					#pragma fragment FragmentProgram

					float FragmentProgram(Interpolators i) : SV_Target {
						return float(SampleGauss1D(i.uv, _Jump, float2(1,0)).r);
					}
				ENDCG
			}

			Pass {
				// Gaussian Blur Y pass 5
				CGPROGRAM
					#pragma vertex VertexProgram
					#pragma fragment FragmentProgram

					half4 FragmentProgram(Interpolators i) : SV_Target {
						return float(SampleGauss1D(i.uv, _Jump, float2(0,1)).r);
					}
				ENDCG
			}

			Pass {
				// Gaussian Blur 2D pass 6
				CGPROGRAM
					#pragma vertex VertexProgram
					#pragma fragment FragmentProgram
					#pragma glsl_no_optimize

					half4 FragmentProgram(Interpolators i) : SV_Target {
						return float(SampleGauss2D(i.uv, _Jump).r);
					}
				ENDCG
			}



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