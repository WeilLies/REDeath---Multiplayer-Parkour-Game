using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

[System.Serializable, VolumeComponentMenu("Post-processing/Custom/HSV Posterize")]
public sealed class HSVPosterizeVolume : CustomPostProcessVolumeComponent,
                                            IPostProcessComponent
{
    public ClampedFloatParameter intensity = new ClampedFloatParameter(1f, 0f, 1f);
    public ClampedFloatParameter steps = new ClampedFloatParameter(8f, 2f, 256f);
    public ClampedFloatParameter noiseDither = new ClampedFloatParameter(0.5f, 0f, 1f);

    Material m_Material;

    public override CustomPostProcessInjectionPoint injectionPoint
        => CustomPostProcessInjectionPoint.AfterPostProcess;

    public bool IsActive()
        => m_Material != null && intensity.value > 0f;

    public override void Setup()
    {
        m_Material = new Material(Shader.Find("Hidden/HSVPosterize"))
        {
            hideFlags = HideFlags.HideAndDontSave
        };
    }

    public override void Render(CommandBuffer cmd,
                                   HDCamera hdCamera,
                                   RTHandle source,
                                   RTHandle destination)
    {
        m_Material.SetFloat("_Steps", steps.value);
        m_Material.SetFloat("_Intensity", intensity.value);
        m_Material.SetFloat("_NoiseDither", noiseDither.value);
        m_Material.SetTexture("_InputTexture", source);

        HDUtils.DrawFullScreen(cmd, m_Material, destination);
    }

    public override void Cleanup()
    {
        CoreUtils.Destroy(m_Material);
    }
}