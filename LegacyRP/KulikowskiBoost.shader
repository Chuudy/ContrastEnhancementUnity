Shader "Custom/Kulikowski" {
	Properties{
		_MainTex("Texture", 2D) = "white" {}
		_BaseLevelTex("BaseLevel", 2D) = "white" {}
		_CSFLut("CSFLut", 2D) = "white" {}
		_Rho("Rho", Range(0.1, 32)) = 4
		_LIn("LuminanceIn", Range(0.001, 300)) = 8
		_LOut("LuminanceOut", Range(0.001, 300)) = 80
	}

		CGINCLUDE
			#include "UnityCG.cginc"

			sampler2D _MainTex, _BaseLevelTex, _CSFLut;
			float4 _MainTex_TexelSize;
			float _Rho;
			float _LIn, _LOut;

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

			float Remap(float x, float minIn, float maxIn, float minOut, float maxOut)
			{
				return (x - minIn) / (maxIn - minIn)*(maxOut - minOut) + minOut;
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
				return max(G_in - G_ts + G_td, 0.001f) / G_in;
			}

		ENDCG


		SubShader{
			Cull Off
			ZTest Always
			ZWrite Off
			
			Pass { 
				// Main pass 0
				CGPROGRAM
					#pragma vertex VertexProgram
					#pragma fragment FragmentProgram
				
					half4 FragmentProgram(Interpolators i) : SV_Target {

						half3 C = Sample(i.uv);
						half3 G = abs(C);

						half Y = tex2D(_BaseLevelTex, i.uv).r;

						float l_in = log10(Y*_LIn);
						float l_out = log10(Y*_LOut);

						float rho = _Rho; // -- TODO -- Change to value corresponding to cpd

						float m = min(KulikowskiBoostG(l_in, G, l_out, rho),2);

						//half3 res = pow(10, log10(C) * m);
						half3 res = C * m;
					
						return half4(res, 1);
					}
				ENDCG
				}
	}
}