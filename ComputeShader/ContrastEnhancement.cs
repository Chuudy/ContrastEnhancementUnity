using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ContrastEnhancement : MonoBehaviour
{
    public Texture InputTexture;
    public ComputeShader shader;

    public bool toggle = true;

    float[] kernel5;
    float[] kernel9;
    float Z5;
    float Z9;

    RenderTexture RT;
    RenderTexture YUVL;
    Renderer rend;

    // Start is called before the first frame update
    void Start()
    {
        GenerateKernel(ref kernel5, ref Z5, 5, 2);
        GenerateKernel(ref kernel9, ref Z9, 9, 4);

        Debug.Log(Z5);
        Debug.Log(Z9);

        RT = new RenderTexture(InputTexture.width, InputTexture.height, 24);
        RT.enableRandomWrite = true;
        RT.Create();

        YUVL = new RenderTexture(InputTexture.width, InputTexture.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        YUVL.enableRandomWrite = true;
        YUVL.Create();

        rend = GetComponent<Renderer>();
        rend.enabled = true;

    }

    // Update is called once per frame
    void Update()
    {
        if (toggle)
        {
            Graphics.Blit(InputTexture, YUVL);
            
            shader.SetFloats("kernel5", kernel5);
            shader.SetFloat("Z5", Z5);
            shader.SetFloats("kernel9", kernel9);
            shader.SetFloat("Z9", Z9);

            int getLuminanceKernelHandle = shader.FindKernel("CSGetLuminance");
            int blurKernelHandle = shader.FindKernel("CSBlur");
            int blurTestKernelHandle = shader.FindKernel("CSBlurTest9");

            shader.SetTexture(getLuminanceKernelHandle, "Result", RT);
            shader.SetTexture(getLuminanceKernelHandle, "YUVL", YUVL);
            //shader.Dispatch(getLuminanceKernelHandle, InputTexture.width / 8, InputTexture.height / 8, 1);

            shader.SetTexture(blurKernelHandle, "Result", RT);
            shader.SetTexture(blurKernelHandle, "YUVL", YUVL);
            //shader.Dispatch(blurKernelHandle, InputTexture.width / 8, InputTexture.height / 8, 1);

            shader.SetTexture(blurTestKernelHandle, "Result", RT);
            shader.SetTexture(blurTestKernelHandle, "YUVL", YUVL);
            shader.Dispatch(blurTestKernelHandle, InputTexture.width / 8, InputTexture.height / 8, 1);
        }

        else
            Graphics.Blit(InputTexture, RT);


        rend.sharedMaterial.mainTexture = RT;
    }

    float Normpdf(float x, float sigma)
    {
        return 0.39894f * Mathf.Exp(-0.5f * x * x / (sigma * sigma)) / sigma;
    }

    void GenerateKernel(ref float[] kernel, ref float Z, int mSize, float sigma)
    {
        int kSize = (mSize - 1) / 2;
        kernel = new float[mSize*4];

        Z = 0.0f;
        for (int j = 0; j <= kSize; ++j)
        {
            kernel[kSize*4 + j*4] = kernel[kSize*4 - j*4] = Normpdf(j, sigma);
        }

        for (int k = 0; k < mSize*4; k+=4)
        {
            Z += kernel[k];
        }
    }
}
