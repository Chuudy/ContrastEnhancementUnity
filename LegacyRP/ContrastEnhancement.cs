using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class ContrastEnhancement : MonoBehaviour
{
    enum RenderPass
    {
        MAIN,
        RGB2YUVL,
        YUVL2L,
        DEPTH
    }

    public Shader contrastEnhancementShader;
    public Texture lutTexture;
    private Material contrastEnhancementMaterial;

    public GameObject linearDriver;
    private Valve.VR.InteractionSystem.LinearMapping driver;

    public bool toggle = true;

    [Header("Algorith parameters")]
    [Range(0.001f, 300)]
    public float luminanceTarget = 8;
    [Range(0.001f, 300)]
    public float luminanceSource = 80;
    [Range(0.01f, 4)]
    public float rhoMultiplier = 1;
    [Range(0f, 2)]
    public float enhancementMultiplier = 1;
    [Range(0.1f, 20f)]
    public float sensitivity = 8.6f;
    [Range(0.1f, 10f)]
    public float pixelSizeFactorMultiplier = 0.5f;

    private SteamVR_Input_Sources rightHand = SteamVR_Input_Sources.RightHand;

    private float[] kernel5;
    private float[] kernel9;
    private float Z5;
    private float Z9;

    private void Start()
    {
        Camera cam = GetComponent<Camera>();
        cam.depthTextureMode = cam.depthTextureMode | DepthTextureMode.Depth;

        ////// Generating kernels and divisors for gaussian blur
        GenerateKernel(ref kernel5, ref Z5, 5, 2);
        GenerateKernel(ref kernel9, ref Z9, 9, 4);

        foreach (var val in kernel5)
        {
            Debug.Log(val);
        }

        ////// check if shader and texture exist
        if (contrastEnhancementShader == null)
            Debug.LogError("Post process shader not assigned in the script");
        if (lutTexture == null)
            Debug.LogError("Texture not assigned in the script");

        ////// create a ne material and assign shader
        if (contrastEnhancementMaterial == null)
        {
            contrastEnhancementMaterial = new Material(contrastEnhancementShader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        ////// assign lut texture to the material
        contrastEnhancementMaterial.SetTexture("_CSFLut", lutTexture);

        if (linearDriver == null)
            Debug.LogError("LinearDriverNotAssigned");
        else
            driver = linearDriver.GetComponent<Valve.VR.InteractionSystem.LinearMapping>();
    }

    private void Update()
    {
        
        if (Input.GetKeyDown(KeyCode.T))
            toggle = !toggle;

        if (SteamVR_Input.GetStateDown("GrabPinch", rightHand))
        {
            toggle = !toggle;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();

        if(driver != null)
            enhancementMultiplier = Mathf.Lerp(0, 2, driver.value);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if(!toggle)
        {
            Graphics.Blit(source, destination);
            return;
        }

        ////// kernels setup 
        contrastEnhancementMaterial.SetFloatArray("_kernel5", kernel5);
        contrastEnhancementMaterial.SetFloatArray("_kernel9", kernel9);
        contrastEnhancementMaterial.SetFloat("_Z5", Z5);
        contrastEnhancementMaterial.SetFloat("_Z9", Z9);
        contrastEnhancementMaterial.SetFloat("_pixelSizeFactorMultiplier", pixelSizeFactorMultiplier);
        
        contrastEnhancementMaterial.SetFloat("_Sensitivity", sensitivity);        
        contrastEnhancementMaterial.SetFloat("_EnhancementMultiplier", enhancementMultiplier);        

        RenderTexture YUVL = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGBFloat);
        RenderTexture LL2 = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.RGFloat);

        Graphics.Blit(source, YUVL, contrastEnhancementMaterial, (int)RenderPass.RGB2YUVL);
        Graphics.Blit(YUVL, LL2, contrastEnhancementMaterial, (int)RenderPass.YUVL2L);

        contrastEnhancementMaterial.SetTexture("_YuvlTex", YUVL);
        contrastEnhancementMaterial.SetFloat("_LumTarget", luminanceTarget);
        contrastEnhancementMaterial.SetFloat("_LumSource", luminanceSource);

        Graphics.Blit(LL2, destination, contrastEnhancementMaterial, (int)RenderPass.MAIN);

        //Graphics.Blit(source, destination, contrastEnhancementMaterial, (int)RenderPass.DEPTH);

        //Graphics.Blit(YUVL, destination);

        RenderTexture.ReleaseTemporary(YUVL);
        RenderTexture.ReleaseTemporary(LL2);

        //RenderTexture tmp = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.RFloat);
        //Graphics.Blit(source, tmp, testMaterial, 3);
        //Graphics.Blit(tmp, destination, testMaterial, 1);
        //Graphics.Blit(source, destination, testMaterial, 0);
        //Graphics.Blit(source, destination, testMaterial, 1);
        //Graphics.Blit(source, destination, contrastEnhancementMaterial, 2);
        //RenderTexture.ReleaseTemporary(tmp);
    }

    float Normpdf(float x, float sigma)
    {
        return 0.39894f * Mathf.Exp(-0.5f * x * x / (sigma * sigma)) / sigma;
    }

    void GenerateKernel(ref float[] kernel, ref float Z, int mSize, float sigma)
    {
        int kSize = (mSize - 1) / 2;
        kernel = new float[mSize];

        Z = 0.0f;
        for (int j = 0; j <= kSize; ++j)
        {
            kernel[kSize + j] = kernel[kSize - j] = Normpdf(j, sigma);
        }

        for (int k = 0; k < mSize; ++k)
        {
            Z += kernel[k];
        }
    }
}
