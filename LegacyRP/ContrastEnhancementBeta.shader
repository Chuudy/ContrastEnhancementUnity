Shader "Custom/ContrastEnhancementBeta" 
{
	Properties{
		_MainTex("Texture", 2D) = "white" {}
		_YuvlTex("YUVL Texture", 2D) = "white" {}
		_CSFLut("CSFLut", 2D) = "white" {}

		_Rho("Rho", Range(0.1, 32)) = 24
		_EnhancementMultiplier("EnhancementMultiplier", Range(0, 2)) = 1
		_LumSource("Source Lum", Range(0.001, 300)) = 80
		_LumTarget("Terget Lum", Range(0.001, 300)) = 8

		_Sensitivity("Sensitivity", Range(0.1,20)) = 8.6

		_pixelSizeFactorMultiplier("Pixel Size Factor", Range(0.1,10)) = 0.5

		_modulationToggle("Modulation Toggle", Int) = 0
	}

	CGINCLUDE
	#include "UnityCG.cginc"

	sampler2D _MainTex;
	sampler2D _CSFLut;
	sampler2D _CameraDepthTexture;
	float4 _MainTex_TexelSize;

	float _kernel5[5];
	float _kernel9[9];
	float _Z9;
	float _Z5;

	float _Sensitivity = 8.6;
	float _pixelSizeFactorMultiplier = 0.5;

	int _modulationToggle;

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

	float4 Gauss2Dk9c4(float2 uv, float sigma)
	{
		//declare stuff
		const int mSize = 9;
		float2 o = _MainTex_TexelSize.xy * 0.5;
		const int kSize = (mSize - 1) / 2;
		float kernel[mSize];
		float3 final_colour = float3(0, 0, 0);

		//create the 1-D kernel
		float Z = 0.0;
		for (int j = 0; j <= kSize; ++j)
		{
			kernel[kSize + j] = kernel[kSize - j] = normpdf(float(j), sigma);
		}

		//get the normalization factor (as the gaussian has been clamped)
		for (int k = 0; k < mSize; ++k)
		{
			Z += kernel[k];
		}

		//read out the texels
		for (int i = -kSize; i <= kSize; ++i)
		{
			for (int j = -kSize; j <= kSize; ++j)
			{
				final_colour += kernel[kSize + j] * kernel[kSize + i] * tex2D(_MainTex, uv + float2(i, j) * o).rgb;
			}
		}

		return float4(final_colour / (Z * Z), 1.0);
	}

	float4 Gauss2Dk5c4(float2 uv, float sigma)
	{
		//declare stuff
		const int mSize = 5;
		float2 o = _MainTex_TexelSize.xy * 0.5;
		const int kSize = (mSize - 1) / 2;
		float kernel[mSize];
		float3 final_colour = float3(0, 0, 0);

		//create the 1-D kernel
		float Z = 0.0;
		for (int j = 0; j <= kSize; ++j)
		{
			kernel[kSize + j] = kernel[kSize - j] = normpdf(float(j), sigma);
		}

		//get the normalization factor (as the gaussian has been clamped)
		for (int k = 0; k < mSize; ++k)
		{
			Z += kernel[k];
		}


		//read out the texels
		for (int i = -kSize; i <= kSize; ++i)
		{
			for (int j = -kSize; j <= kSize; ++j)
			{
				final_colour += kernel[kSize + j] * kernel[kSize + i] * tex2D(_MainTex, uv + float2(i, j) * o).rgb;
			}
		}

		return float4(final_colour / (Z * Z), 1.0);
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
		for (int k = 0; k < mSize; ++k)
		{
			Z += kernel[k];
		}
		
		//read out the texels
		for (int i = -kSize; i <= kSize; ++i)
		{
			for (int j = -kSize; j <= kSize; ++j)
			{
				final_colour += kernel[kSize + j] * kernel[kSize + i] * tex2D(_MainTex, uv + float2(i, j) * o);
			}
		}
		return final_colour / (Z * Z);
	}

	float Gauss2Dk9c1_Opt(float2 uv)
	{
		//declare stuff
		const int mSize = 9;
		float2 o = _MainTex_TexelSize.xy * _pixelSizeFactorMultiplier;
		const int kSize = (mSize - 1) / 2;
		float final_colour = 0;

		//read out the texels
		for (int i = -kSize; i <= kSize; ++i)
		{
			for (int j = -kSize; j <= kSize; ++j)
			{
				final_colour += _kernel9[kSize + j] * _kernel9[kSize + i] * tex2D(_MainTex, uv + float2(i, j) * o);
			}
		}
		return final_colour / (_Z9 * _Z9);
	}

	float2 Gauss2Dk9c2_Opt(float2 uv)
	{
		//declare stuff
		const int mSize = 9;
		float2 o = _MainTex_TexelSize.xy * _pixelSizeFactorMultiplier;
		const int kSize = (mSize - 1) / 2;
		float2 final_colour = float2(0,0);

		//read out the texels
		for (int i = -kSize; i <= kSize; ++i)
		{
			for (int j = -kSize; j <= kSize; ++j)
			{
				final_colour += _kernel9[kSize + j] * _kernel9[kSize + i] * tex2D(_MainTex, uv + float2(i, j) * o);
			}
		}
		return final_colour / (_Z9 * _Z9);
	}

	float Gauss2Dk5c1_Opt(float2 uv)
	{
		//declare stuff
		const int mSize = 5;
		float2 o = _MainTex_TexelSize.xy * _pixelSizeFactorMultiplier;
		const int kSize = (mSize - 1) / 2;
		float final_colour = 0;

		//read out the texels
		for (int i = -kSize; i <= kSize; ++i)
		{
			for (int j = -kSize; j <= kSize; ++j)
			{
				final_colour += _kernel5[kSize + j] * _kernel5[kSize + i] * tex2D(_MainTex, uv + float2(i, j) * o);
			}
		}
		return final_colour / (_Z5 * _Z5);
	}

	float2 Gauss2Dk5c2_Opt(float2 uv)
	{
		//declare stuff
		const int mSize = 5;
		float2 o = _MainTex_TexelSize.xy * _pixelSizeFactorMultiplier;
		const int kSize = (mSize - 1) / 2;
		float2 final_colour = float2(0, 0);

		//read out the texels
		for (int i = -kSize; i <= kSize; ++i)
		{
			for (int j = -kSize; j <= kSize; ++j)
			{
				final_colour += _kernel5[kSize + j] * _kernel5[kSize + i] * tex2D(_MainTex, uv + float2(i, j) * o);
			}
		}
		return final_colour / (_Z5 * _Z5);
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
		for (int k = 0; k < mSize; ++k)
		{
			Z += kernel[k];
		}


		//read out the texels
		for (int i = -kSize; i <= kSize; ++i)
		{
			for (int j = -kSize; j <= kSize; ++j)
			{
				final_colour += kernel[kSize + j] * kernel[kSize + i] * tex2D(_MainTex, uv + float2(i, j) * o);
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
		float y = 1-Remap01(logLum, -5, 3); // values in the LUT are generated for range -5:3
		return float2(x, y);
	}

	float SampleCSF(float rho, float logLum)
	{
		float2 uv = RhoAndLogLumToCSFCoordinates(rho, logLum);
		return tex2D(_CSFLut, uv).r * _Sensitivity;
	}

	float log2michelson(float G)
	{
		return (pow(10,(2 * G)) - 1) / (pow(10,(2 * G)) + 1);
	}

	float michelson2log(float C)
	{
		return 0.5*log10((C + 1)/(1 - C));
	}

	float beta_function(float Y, float C)
	{
		return  (0.00964028930664063*log10(Y) + 0.00110626220703125*C + (-0.0015)*(pow(log10(Y),2)) + 0.0271167755126953);
	}

	float beta_function_new(float Y, float C)
	{
		float pars1 = 0.00832040405273438;
		float pars2 = -0.000186920166015625;
		float pars3 = -0.0015;
		float pars4 = 0.00003;
		float pars5 = 0.0360622406005859;

		return  (pars1*log10(Y) + pars2 * C + pars3 * (pow(log10(Y), 2)) + pars4 * pow(C, 2) + pars5);
	}
	
	float contrast_function(float Y, float beta)
	{
		return ((0.00964028930664063*log10(Y) + (-0.0015)*(pow(log10(Y), 2)) + 0.0271167755126953 - beta) / (-0.00110626220703125));
	}

	float contrast_function_new(float Y, float beta)
	{
		float pars1 = 0.00832040405273438;
		float pars2 = -0.000186920166015625;
		float pars3 = -0.0015;
		float pars4 = 0.00003;
		float pars5 = 0.0360622406005859;

		float A = pars4;
		float B = pars2;
		float K = pars1 * log10(Y) + pars3 * (pow(log10(Y), 2)) + pars5 - beta;

		return (-B + sqrt(pow(B,2) - 4 * A*K)) / (2 * A);
	}

	float betaBoostG(float Y_in, float Y_out, float G_est)
	{
		float C_in = log2michelson(G_est) * 2 * 100;
		if (C_in <= 1)
			return 1;
		float beta = beta_function_new(Y_in, C_in);
		/*if (beta > 0.1)*/
			//return 1;
		float C_out = contrast_function_new(Y_out, beta);
		float G_est_out = michelson2log(C_out / 200);
		float m = G_est_out / max(0.0001, G_est);
		return m;
	}

	float KulikowskiBoostG(float l_in, float G_in, float l_out, float rho)
	{
		float G_ts = 1/SampleCSF(rho, l_in);
		float G_td = 1/SampleCSF(rho, l_out);
		return max(G_in - G_ts + G_td, 0.00000001f) / G_in;
	}

	ENDCG


		SubShader
	{
		Cull Off
		ZTest Always
		ZWrite Off

		Pass  // Main pass 0
		{
			CGPROGRAM
				#pragma vertex VertexProgram
				#pragma fragment FragmentProgram

				float _LumSource, _LumTarget, _Rho, _EnhancementMultiplier;
				sampler2D _YuvlTex;

				float4 FragmentProgram(Interpolators i) : SV_Target
				{
					float2 g0composite = tex2D(_MainTex, i.uv);
					float2 g1composite = Gauss2Dk5c2_Opt(i.uv);
					float2 g2composite = Gauss2Dk9c2_Opt(i.uv);

					float g0 = g0composite.r;
					//float g1 = Gauss2Dk5c1(i.uv, 2);
					float g1 = g1composite.r;
					//float g2 = Gauss2Dk9c1(i.uv, 4);
					float g2 = g2composite.r;
			
					float P_in[3];
					P_in[0] = g0 - g1;
					P_in[1] = g1 - g2;
					P_in[2] = g2;

					float2 LL2Composites[2];
					LL2Composites[0] = g1composite;
					LL2Composites[1] = g2composite;
									   
					float l_in = g2;
					float l_out = g2;

					//int iter = 1;
					//float y = sqrt(max(0, LL2Composites[iter].g - LL2Composites[iter].r * LL2Composites[iter].r));
					////float y = sqrt(max(0, LL2Composites[iter].g - LL2Composites[iter].g * LL2Composites[iter].g));
					//y = abs(P_in[iter]);
					////float y = pow(10,sqrt(g1composite.g));
					//return float4(y, y, y, y);

					for (int iter = 1; iter >= 0; iter--)
					{
						float C_in = P_in[iter];

						float l_source = log10(pow(10, l_in) * _LumSource);
						float l_target = log10(pow(10, l_out) * _LumTarget);

						//float G_est = abs(C_in);
						float G_est = sqrt(max(0, LL2Composites[iter].g - LL2Composites[iter].r * LL2Composites[iter].r));

						float rho = _Rho;
						//float m = min(KulikowskiBoostG(l_source, G_est, l_target, rho), 2) * _EnhancementMultiplier;
						float m = min(betaBoostG(l_source, l_target, G_est), 2);
						
						float modulation = 1 - (1 / (1 + pow(2.71828, -100 * (G_est - 0.0728))));
						
						//_modulationToggle = 0;
						if(_modulationToggle == 1)
							m = m*modulation + (1 - modulation);

						float C_out = C_in * m;

						l_out = l_out + C_out;
						l_in = l_in + P_in[iter];
					}

					float3 y_out = pow(10, l_out);

					float3 yuvIn = tex2D(_YuvlTex, i.uv).rgb;

					//// old method
					//float3 yuvOut = yuvIn;
					//yuvOut.r = y_out;
					//return float4(yuv2rgb(yuvOut), 1);

					// new method
					float3 rgbIn = yuv2rgb(yuvIn);
					float maxFactor = 1 / max(max(rgbIn.r, rgbIn.g), rgbIn.b);
					float multiplier = min((y_out / yuvIn.r), maxFactor);
					return float4(rgbIn * multiplier, 1);
				}
			ENDCG
		}

		Pass // RGB to YUVL 1
		{
			CGPROGRAM
			#pragma vertex VertexProgram
			#pragma fragment FragmentProgram

			float4 FragmentProgram(Interpolators i) : SV_Target {

				float3 rgbClamped = clamp(tex2D(_MainTex,i.uv), 0.00001, 1);
				float3 yuv = rgb2yuv(rgbClamped);
				return float4(yuv.rgb, log10(yuv.r));
			}
		ENDCG
		}

		Pass // YUVL to L 2
		{						
			CGPROGRAM
				#pragma vertex VertexProgram
				#pragma fragment FragmentProgram

				float2 FragmentProgram(Interpolators i) : SV_Target {
					float val = tex2D(_MainTex, i.uv).w;
					return float2(val,val*val);
				}
			ENDCG
		}

		Pass // DepthPass
		{
			CGPROGRAM
				#pragma vertex VertexProgram
				#pragma fragment FragmentProgram

				float4 FragmentProgram(Interpolators i) : SV_Target {
					//get depth from depth texture
					float depth = tex2D(_CameraDepthTexture, i.uv).r;
					//linear depth between camera and far clipping plane
					//depth = Linear01Depth(depth);
					
					return depth;
				}
			ENDCG
		}
	}	
}