Shader "Hidden/Custom/ContrastBoost"
{
	HLSLINCLUDE

		#include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

		TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
		float _Blend;
		sampler2D _YuvlTex;
		sampler2D _CSFLut;
		float4 _MainTex_TexelSize;

		float _LumSource;
		float _LumTarget;
		float _Rho;

		float getLuminance(float3 color)
		{
			return dot(color.rgb, float3(0.2126729, 0.7151522, 0.0721750));
		}

		float3 rgb2yuv(float3 rgb)
		{
			float3x3 m_rgb2yuv = float3x3(	0.299f, 0.587f, 0.114f,
											-0.147f, -0.289f, 0.437f,
											0.615f, -0.515f, -0.100f);
			float3 yuv = mul(m_rgb2yuv, rgb);
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

		float normpdf(in float x, in float sigma)
		{
			return 0.39894 * exp(-0.5 * x * x / (sigma * sigma)) / sigma;
		}

		float Gauss2Dk9c1(float2 uv, float sigma)
		{
			//declare stuff
			const int mSize = 9;
			float2 o = _MainTex_TexelSize.xy * 0.5;
			const int kSize = (mSize - 1) / 2;
			float kernel[mSize];
			float final_colour = float3(0, 0, 0);

			//create the 1-D kernel
			float Z = 0.0;
			for (int j = 0; j <= kSize; ++j)
			{
				kernel[kSize + j] = kernel[kSize - j] = normpdf(float(j), sigma);
			}

			//get the normalization factor (as the gaussian has been clamped)
			for (int j = 0; j < mSize; ++j)
			{
				Z += kernel[j];
			}

			//read out the texels
			for (int i = -kSize; i <= kSize; ++i)
			{
				for (int j = -kSize; j <= kSize; ++j)
				{
					final_colour += kernel[kSize + j] * kernel[kSize + i] * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(i, j) * o);
				}
			}
			return final_colour / (Z * Z);
		}

		float Gauss2Dk5c1(float2 uv, float sigma)
		{
			//declare stuff
			const int mSize = 5;
			float2 o = _MainTex_TexelSize.xy * 0.5;
			const int kSize = (mSize - 1) / 2;
			float kernel[mSize];
			float final_colour = float3(0, 0, 0);

			//create the 1-D kernel
			float Z = 0.0;
			for (int j = 0; j <= kSize; ++j)
			{
				kernel[kSize + j] = kernel[kSize - j] = normpdf(float(j), sigma);
			}

			//get the normalization factor (as the gaussian has been clamped)
			for (int j = 0; j < mSize; ++j)
			{
				Z += kernel[j];
			}


			//read out the texels
			for (int i = -kSize; i <= kSize; ++i)
			{
				for (int j = -kSize; j <= kSize; ++j)
				{
					final_colour += kernel[kSize + j] * kernel[kSize + i] * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(i, j) * o);
				}
			}

			return final_colour / (Z * Z);
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




		float4 Frag(VaryingsDefault i) : SV_Target
		{
			float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
			float luminance = getLuminance(color.rgb);
			color.rgb = lerp(color.rgb, luminance.xxx, _Blend.xxx);
			return color;
		}

		float4 FragYUVL(VaryingsDefault i) : SV_Target
		{
			float3 rgbClamped = clamp(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord).rgb, 0.00001, 1);
			float3 yuv = rgb2yuv(rgbClamped);
			return float4(yuv.rgb, log10(yuv.r));
		}

		float FragL(VaryingsDefault i) : SV_Target
		{
			return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord).w;
		}

		float4 FragMain(VaryingsDefault i) : SV_Target
		{
			float g0 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
			float g1 = Gauss2Dk5c1(i.texcoord, 2);
			float g2 = Gauss2Dk9c1(i.texcoord, 4);

			float P_in[3];

			P_in[0] = g0 - g1;
			P_in[1] = g1 - g2;
			P_in[2] = g2;

			float l_in = g2;
			float l_out = g2;

			for (int iter = 1; iter >= 0; iter--)
			{
				float C_in = P_in[iter];

				float l_source = log10(pow(10, l_in) * _LumSource);
				float l_target = log10(pow(10, l_out) * _LumTarget);

				float G_est = abs(C_in);

				float rho = _Rho;
				float m = min(KulikowskiBoostG(l_source, G_est, l_target, rho), 2);
				//m = 2;

				float C_out = C_in * m;

				l_out = l_out + C_out;
				l_in = l_in + P_in[iter];
			}

			float y_out = pow(10, l_out);
			float3 yuvOut = tex2D(_YuvlTex, i.texcoord).rgb;

			float y_blended = lerp(clamp(yuvOut.r, 0.00001, 1), y_out, _Blend.x);

			yuvOut.r = y_blended;

			return float4(yuv2rgb(yuvOut), 1);

			//return P_in[1];

			//return pow(10,tex2D(_YuvlTex, i.texcoord));
			//return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord).r;
		}

	ENDHLSL

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			HLSLPROGRAM

				#pragma vertex VertDefault
				#pragma fragment Frag

			ENDHLSL
		}

		Pass
		{
			HLSLPROGRAM

				#pragma vertex VertDefault
				#pragma fragment FragYUVL

			ENDHLSL
		}

		Pass
		{
			HLSLPROGRAM

				#pragma vertex VertDefault
				#pragma fragment FragL

			ENDHLSL
		}

		Pass
		{
			HLSLPROGRAM

				#pragma vertex VertDefault
				#pragma fragment FragMain

			ENDHLSL
		}
	}
}