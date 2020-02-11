using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class ContrastEnhancement : MonoBehaviour
{
    public Shader testShader;
    private Material testMaterial;

    public bool toggle = true;

    public SteamVR_Input_Sources rightHand = SteamVR_Input_Sources.RightHand;

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

        if (testMaterial == null)
        {
            testMaterial = new Material(testShader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }
        testMaterial.SetFloat("_Jump", 1);

        //RenderTexture tmp = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.RFloat);
        //Graphics.Blit(source, tmp, testMaterial, 3);
        //Graphics.Blit(tmp, destination, testMaterial, 1);
        //Graphics.Blit(source, destination, testMaterial, 0);
        //Graphics.Blit(source, destination, testMaterial, 1);
        Graphics.Blit(source, destination, testMaterial, 2);
        //RenderTexture.ReleaseTemporary(tmp);
    }
}
