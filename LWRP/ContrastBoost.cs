using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(ContrastBoostRenderer), PostProcessEvent.AfterStack, "Custom/ContrastBoost")]
public sealed class ContrastBoost : PostProcessEffectSettings
{
    [Range(0f, 1f), Tooltip("Grayscale effect intensity.")]
    public FloatParameter blend = new FloatParameter { value = 1f };
    [Range(0.01f, 80f), Tooltip("Source luminance.")]
    public FloatParameter sourceLum = new FloatParameter { value = 75f };
    [Range(0.01f, 80f), Tooltip("Target luminance.")]
    public FloatParameter targetLum = new FloatParameter { value = 5f };
    [Tooltip("CSF Texture")]
    public TextureParameter csf = new TextureParameter { value = null };
    //[Range(0.01f, 80f), Tooltip("CSF")]
    //public TextureParameter csf = new TextureParameter { };
}

public sealed class ContrastBoostRenderer : PostProcessEffectRenderer<ContrastBoost>
{
    public override void Render(PostProcessRenderContext context)
    {
        var sheet = context.propertySheets.Get(Shader.Find("Hidden/Custom/ContrastBoost"));
        sheet.properties.SetFloat("_Blend", settings.blend);
        sheet.properties.SetFloat("_LumSource", settings.sourceLum);
        sheet.properties.SetFloat("_LumTarget", settings.targetLum);
        sheet.properties.SetTexture("_CSFLut", settings.csf);
        //sheet.properties.SetTexture

        int yuvlId = Shader.PropertyToID("YUVL");
        int LId = Shader.PropertyToID("L");

        context.command.GetTemporaryRT(yuvlId, context.width, context.height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBFloat);
        context.command.GetTemporaryRT(LId, context.width, context.height, 0, FilterMode.Bilinear, RenderTextureFormat.RFloat);

        UnityEngine.Rendering.RenderTargetIdentifier yuvlIdentifier = new UnityEngine.Rendering.RenderTargetIdentifier(yuvlId);
        UnityEngine.Rendering.RenderTargetIdentifier LIdentifier = new UnityEngine.Rendering.RenderTargetIdentifier(LId);

        context.command.BlitFullscreenTriangle(context.source, yuvlIdentifier, sheet, 1);
        context.command.BlitFullscreenTriangle(yuvlIdentifier, LIdentifier, sheet, 2);
        context.command.SetGlobalTexture("_YuvlTex", yuvlIdentifier);
        context.command.BlitFullscreenTriangle(LIdentifier, context.destination, sheet, 3);


        context.command.ReleaseTemporaryRT(yuvlId);
        context.command.ReleaseTemporaryRT(LId);
    }
}