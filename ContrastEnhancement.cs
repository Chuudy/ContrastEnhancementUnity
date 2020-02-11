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
        YUVL2L
    }

    public Shader contrastEnhancementShader;
    private Material contrastEnhancementMaterial;

    public bool toggle = true;

    [Header("Algorith parameters")]
    [Range(0.001f, 300)]
    public float luminanceTarget = 8;
    [Range(0.001f, 300)]
    public float luminanceSource = 80;
    [Range(0.01f, 4)]
    public float rhoMultiplier = 1;

    private SteamVR_Input_Sources rightHand = SteamVR_Input_Sources.RightHand;

    private void Start()
    {
        
    }

    private void Update()
    {
        
            if (Input.GetKeyDown(KeyCode.T))
                toggle = !toggle;

            if (SteamVR_Input.GetStateDown("GrabPinch", rightHand))
            {
                toggle = !toggle;
            }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if(!toggle)
        {
            Graphics.Blit(source, destination);
            return;
        }

        if (contrastEnhancementMaterial == null)
        {
            contrastEnhancementMaterial = new Material(contrastEnhancementShader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        RenderTexture YUVL = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGBFloat);
        RenderTexture L = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.RFloat);

        Graphics.Blit(source, YUVL, contrastEnhancementMaterial, (int)RenderPass.RGB2YUVL);
        Graphics.Blit(YUVL, L, contrastEnhancementMaterial, (int)RenderPass.YUVL2L);

        contrastEnhancementMaterial.SetTexture("_YuvlTex", YUVL);
        contrastEnhancementMaterial.SetFloat("_LumTarget", luminanceTarget);
        contrastEnhancementMaterial.SetFloat("_LumSource", luminanceSource);

        Graphics.Blit(L, destination, contrastEnhancementMaterial, (int)RenderPass.MAIN);

        //Graphics.Blit(YUVL, destination);

        RenderTexture.ReleaseTemporary(YUVL);
        RenderTexture.ReleaseTemporary(L);

        //RenderTexture tmp = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.RFloat);
        //Graphics.Blit(source, tmp, testMaterial, 3);
        //Graphics.Blit(tmp, destination, testMaterial, 1);
        //Graphics.Blit(source, destination, testMaterial, 0);
        //Graphics.Blit(source, destination, testMaterial, 1);
        //Graphics.Blit(source, destination, contrastEnhancementMaterial, 2);
        //RenderTexture.ReleaseTemporary(tmp);
    }
}
