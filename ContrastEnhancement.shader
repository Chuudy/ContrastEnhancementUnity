Shader "Custom/Kulikowski" 
{
	Properties{
		_MainTex("Texture", 2D) = "white" {}
		_Jump("Jump", Range(0, 1024)) = 0
	}

	CGINCLUDE
	#include "UnityCG.cginc"

	sampler2D _MainTex;
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

	float normpdf(in float x, in float sigma)
	{
		return 0.39894 * exp(-0.5 * x * x / (sigma * sigma)) / sigma;
	}

	float4 Gauss2Dk9(float2 uv, float sigma)
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
		for (int j = 0; j < mSize; ++j)
		{
			Z += kernel[j];
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

	float4 Gauss2Dk5(float2 uv, float sigma)
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
		for (int j = 0; j < mSize; ++j)
		{
			Z += kernel[j];
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

	ENDCG


	SubShader{
		Cull Off
		ZTest Always
		ZWrite Off

		Pass  // Main pass 0
		{		
		CGPROGRAM
			#pragma vertex VertexProgram
			#pragma fragment FragmentProgram

			float4 FragmentProgram(Interpolators i) : SV_Target
			{
				return Gauss2Dk9(i.uv, 4);
			}
		ENDCG
		}

		Pass  // Main pass 1
		{
		CGPROGRAM
			#pragma vertex VertexProgram
			#pragma fragment FragmentProgram

			float4 FragmentProgram(Interpolators i) : SV_Target
			{
				return Gauss2Dk5(i.uv, 2);
			}
		ENDCG
		}

		Pass  // Main pass 2
		{
		CGPROGRAM
			#pragma vertex VertexProgram
			#pragma fragment FragmentProgram

			float4 FragmentProgram(Interpolators i) : SV_Target
			{
				return Gauss2Dk5(i.uv, 2)- Gauss2Dk9(i.uv, 4);
			}
		ENDCG
		}
	}	
}