using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class GaussianPyramid : MonoBehaviour
{
    public enum DebugEnum
    {
        NoDebug,
        GaussianPyramid,
        LaPlacePyramid
    }

    public enum GaussPyramidEnum
    {
        FastGausian2D,
        FastGausianDoublePass
    }

    public enum HMD
    {
        ValveIndex,
        OculusRiftCV1
    }

    [Header("Sahders")]
    public Shader gaussianShader;
    public Shader kulikowskiBoostShader;

    [Header("HMD Setup")]
    public HMD hmd;

    [Header("Algorith parameters")]
    [Range(0, 3)]
    public int levels = 4;
    public GaussPyramidEnum gaussianPyramidMode;
    [Range(0.001f, 300)]
    public float luminanceTarget =8;
    [Range(0.001f, 300)]
    public float luminanceSource = 80;
    [Range(0.01f, 4)]
    public float rhoMultiplier = 1;

    [Header("Debug parameters")]
    public DebugEnum debugMode;
    [Range(0, 3)]
    public int previewLevel = 0;


    private float resolutionPpd = 16;

    private RenderTexture[] gaussianPyramid = new RenderTexture[5];
    private RenderTexture[] laPlacePyramid = new RenderTexture[5];

    private Material gausianPyramidMat;
    private Material kulikowskiBoostMat;

    private const int MainPass = 0;

    private const int PreviewLogPass = 1;
    private const int Rgb2YuvlPass = 2;
    private const int Yuvl2LPass = 3;

    private const int GausianBlurPassX = 4;
    private const int GausianBlurPassY = 5;
    private const int GausianBlur2DPass = 6;

    private const int RGBtoYPass = 5;
    private const int LaPlaceDiffPass = 6;
    private const int LaPlaceSumPass = 7;
    private const int YtoRGBPass = 8;
    private const int BoxDownPass = 0;
    private const int BoxUpPass = 1;
    private const int DiffPass = 2;

    public bool toggle = true;

    RenderTexture level0, levelX_temp, levelX;

    public SteamVR_Input_Sources rightHand = SteamVR_Input_Sources.RightHand;

    private void OnDestroy()
    {
        RenderTexture.ReleaseTemporary(levelX_temp);
        for (int i = 0; i < levels; ++i)
        {
            RenderTexture.ReleaseTemporary(gaussianPyramid[i]);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
            toggle = !toggle;

        if(SteamVR_Input.GetStateDown("GrabPinch", rightHand))
        {
            toggle = !toggle;
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {

        // Toggle contrast enhancement
        if(!toggle)
        {
            Graphics.Blit(source, destination);
            return;
        }

        // Init resolution
        if (hmd == HMD.OculusRiftCV1)
            resolutionPpd = 12;

        if (hmd == HMD.ValveIndex)
            resolutionPpd = 18;

        // Init Materials if don't exist
        if (gausianPyramidMat == null)
        {
            gausianPyramidMat = new Material(gaussianShader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }
        if (kulikowskiBoostMat == null)
        {
            kulikowskiBoostMat = new Material(kulikowskiBoostShader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        // STEP 1
        // RGB to LUVY conversion
        RenderTexture YUVLTexture = new RenderTexture(source.width, source.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
        YUVLTexture.useMipMap = true;
        YUVLTexture.Create();
        Graphics.Blit(source, YUVLTexture, gausianPyramidMat, Rgb2YuvlPass);
               

        // STEP 2
        // Gaussian Pyramid creation
        RenderTexture level0 = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
        Graphics.Blit(YUVLTexture, level0, gausianPyramidMat, Yuvl2LPass);
        gaussianPyramid[0] = level0;

        RenderTexture levelX_temp = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
        for (int i = 1; i < levels; ++i)
        {
            float jump = Mathf.Pow(2f, (float)(i - 1));
            gausianPyramidMat.SetFloat("_Jump", jump);
            RenderTexture levelX = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);

            if (gaussianPyramidMode == GaussPyramidEnum.FastGausianDoublePass)
            {
                Graphics.Blit(gaussianPyramid[i - 1], levelX_temp, gausianPyramidMat, GausianBlurPassX);  // Blur X
                Graphics.Blit(levelX_temp, levelX, gausianPyramidMat, GausianBlurPassY);  // Blur Y
            }
            if(gaussianPyramidMode == GaussPyramidEnum.FastGausian2D)
            {
                Graphics.Blit(gaussianPyramid[i - 1], levelX, gausianPyramidMat, GausianBlur2DPass);
            }
            gaussianPyramid[i] = levelX;
        }


        // STEP 3
        // Building Laplacian Pyramid and enhancing contrast
        gausianPyramidMat.SetFloat("_LumSource", luminanceSource);
        gausianPyramidMat.SetFloat("_LumTarget", luminanceTarget);
        gausianPyramidMat.SetFloat("_Rho", rhoMultiplier);
        gausianPyramidMat.SetTexture("_Level0", gaussianPyramid[0]);
        gausianPyramidMat.SetTexture("_Level1", gaussianPyramid[1]);
        gausianPyramidMat.SetTexture("_Level2", gaussianPyramid[2]);
        gausianPyramidMat.SetTexture("_Level3", gaussianPyramid[3]);

        Graphics.Blit(YUVLTexture, destination, gausianPyramidMat, MainPass);

        if (debugMode == DebugEnum.GaussianPyramid)
        {
            Graphics.Blit(gaussianPyramid[previewLevel], destination, gausianPyramidMat, PreviewLogPass);
        }

        // LAST STEP
        // Temporary texture clearance
        for (int i = 0; i < levels; ++i)
        {
            RenderTexture.ReleaseTemporary(gaussianPyramid[i]);
        }
        RenderTexture.ReleaseTemporary(levelX_temp);
        YUVLTexture.Release();
        

        return;

        //Init Textures
        if(level0 == null)
        {
            level0 = RenderTexture.GetTemporary(source.width, source.height, 0);
            levelX_temp = RenderTexture.GetTemporary(source.width, source.height, 0);
            for (int i = 1; i < levels; ++i)
            {
                levelX = RenderTexture.GetTemporary(source.width, source.height, 0);
                gaussianPyramid[i] = levelX;
            }
        }

        // Init resolution
        if (hmd == HMD.OculusRiftCV1)
            resolutionPpd = 12;

        // Convert RGV to Y AND
        // Create level0 of the Gaussian pyramid and store it in the array
        Graphics.Blit(source, level0, gausianPyramidMat, RGBtoYPass);
        gaussianPyramid[0] = level0;

        // Genearting levels of gaussian pyramid
        //if (gaussianPyramidMode == GaussPyramidEnum.Downsampling)
        for (int i = 1; i < levels; ++i)
        {
            int scale = (int)Mathf.Pow(2f, i);
            RenderTexture levelX = RenderTexture.GetTemporary(gaussianPyramid[i - 1].width / 2, gaussianPyramid[i - 1].height / 2, 0);
            Graphics.Blit(gaussianPyramid[i-1], levelX);
            gaussianPyramid[i] = levelX;
        }
        //else if(gaussianPyramidMode == GaussPyramidEnum.FastGausian)
        {
            for (int i = 1; i < levels; ++i)
            {
                float jump = Mathf.Pow(2f, (float)(i-1));
                gausianPyramidMat.SetFloat("_Jump", jump);
                RenderTexture levelX = gaussianPyramid[i];
                Graphics.Blit(gaussianPyramid[i - 1], levelX_temp, gausianPyramidMat, GausianBlurPassX);  // Blur X
                Graphics.Blit(levelX_temp, levelX, gausianPyramidMat, GausianBlurPassY);  // Blur Y
                gaussianPyramid[i] = levelX;
            }
        }

        // Converting Gaussian Pyramid to LaPlace pyramid leaving base level as it is
        for (int i = 0; i < levels - 1; ++i)
        {
            RenderTexture temp = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGBFloat);
            gausianPyramidMat.SetTexture("_SourceTex", gaussianPyramid[i + 1]);
            Graphics.Blit(gaussianPyramid[i], temp, gausianPyramidMat, LaPlaceDiffPass);
            laPlacePyramid[i] = temp;
        }
        RenderTexture baseLevel = RenderTexture.GetTemporary(source.width, source.height, 0);
        Graphics.Blit(gaussianPyramid[levels-1], baseLevel);
        laPlacePyramid[levels-1] = baseLevel;

        // Kulikowski Contrast Boost
        kulikowskiBoostMat.SetTexture("_BaseLevelTex", laPlacePyramid[levels - 1]);
        kulikowskiBoostMat.SetFloat("_LIn", luminanceTarget);
        kulikowskiBoostMat.SetFloat("_LOut", luminanceSource);
        RenderTexture tempTex = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGBHalf);
        for(int i = 0; i < levels - 1; ++i)
        {
            float rho = resolutionPpd * Mathf.Pow(2f, -(1 + i)); 
            kulikowskiBoostMat.SetFloat("_Rho", rho * rhoMultiplier);
            Graphics.Blit(laPlacePyramid[i], tempTex, kulikowskiBoostMat);
            Graphics.Blit(tempTex, laPlacePyramid[i]);
        }
        RenderTexture.ReleaseTemporary(tempTex);

        //Summing back the boosted frequncy bands to get Y OUT
        RenderTexture yOut = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGBHalf);
        Graphics.Blit(laPlacePyramid[levels - 1], yOut);
        tempTex = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGBHalf);
        for (int i = 0; i < levels - 1; ++i)
        {
            gausianPyramidMat.SetTexture("_SourceTex", laPlacePyramid[i]);
            Graphics.Blit(yOut, tempTex, gausianPyramidMat, LaPlaceSumPass);
            Graphics.Blit(tempTex, yOut);
        }
        RenderTexture.ReleaseTemporary(tempTex);

        //Converting grayscale to RGB
        tempTex = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGBHalf);
        Graphics.Blit(yOut, tempTex);
        gausianPyramidMat.SetTexture("_SourceTex", tempTex);
        Graphics.Blit(source, yOut, gausianPyramidMat, YtoRGBPass);
        RenderTexture.ReleaseTemporary(tempTex);


        // Render out the final outcome or debug
        if (previewLevel > levels)
            previewLevel = levels;

        //if (debugMode == DebugEnum.GaussianPyramid)
        //{
        //    Graphics.Blit(gaussianPyramid[previewLevel - 1], destination);
        //}
        //else if(debugMode == DebugEnum.LaPlacePyramid)
        //{
        //    Graphics.Blit(laPlacePyramid[previewLevel - 1], destination);
        //}
        //else if (debugMode == DebugEnum.NoDebug)
        //{
        //    Graphics.Blit(yOut, destination);
        //}

        // Clean up
        RenderTexture.ReleaseTemporary(yOut);
        for (int i = 0; i < levels; ++i)
        {
            RenderTexture.ReleaseTemporary(laPlacePyramid[i]);
        }


        //RenderTexture oneDimGaussian = RenderTexture.GetTemporary(source.width, source.height, 0);
        //Graphics.Blit(source, oneDimGaussian, gausianPyramid, GausianBlurPassX);
        //Graphics.Blit(oneDimGaussian, destination, gausianPyramid, GausianBlurPassX);
        //RenderTexture.ReleaseTemporary(oneDimGaussian);


        //int width = source.width / 2;
        //int height = source.height / 2;

        //RenderTexture currentDestination = textures[0] = RenderTexture.GetTemporary(width, height, 0);

        //Graphics.Blit(source, currentDestination, gausianPyramid, BoxDownPass);
        //RenderTexture currentSource = currentDestination;

        //int i = 1;
        //for (; i<levels; ++i)
        //{
        //    width /= 2;
        //    height /= 2;
        //    if (height < 2)
        //        break;
        //    currentDestination = textures[i] = RenderTexture.GetTemporary(width, height, 0);
        //    Graphics.Blit(currentSource, currentDestination, gausianPyramid, BoxDownPass);
        //    currentSource = currentDestination;
        //}

        //for (i -= 2; i >= 0; i--)
        //{
        //    currentDestination = textures[i];
        //    textures[i] = null;
        //    Graphics.Blit(currentSource, currentDestination, gausianPyramid, BoxUpPass);
        //    RenderTexture.ReleaseTemporary(currentSource);
        //    currentSource = currentDestination;
        //}

        //gausianPyramid.SetTexture("_SourceTex", source);
        //Graphics.Blit(currentSource, destination, gausianPyramid, DiffPass);
        //RenderTexture.ReleaseTemporary(currentSource);
    }
}
