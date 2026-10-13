using UnityEngine;

public static class SettingsKeys
{
    public const string Language = "Language";
    public const string Sensitivity = "Sensitivity";
    public const string FullscreenMode = "FullscreenMode";
    public const string ScreenResolution = "ScreenResolution";
    public const string FPSLock = "FPSLock";
    public const string VSync = "VSync";
    public const string TextureQuality = "TextureQuality";
    public const string ShadowQuality = "ShadowQuality";
    public const string MasterVolume = "MasterVolume";
    public const string MusicVolume = "MusicVolume";
    public const string SFXVolume = "SFXVolume";
    public const string UIVolume = "UIVolume";
    public const string OutputDevice = "OutputDevice";

    public enum AllSettingsName
    {
        Language,
        Sensitivity, 
        FullscreenMode,
        ScreenResolution,
        FPSLock,
        VSync,
        TextureQuality,
        ShadowQuality,
        MasterVolume,
        MusicVolume,
        SFXVolume, 
        UIVolume,
        OutputDevice
    }
    public static string ToKey(this AllSettingsName name)
    {
        switch (name)
        {
            case AllSettingsName.Language: return Language;
            case AllSettingsName.Sensitivity: return Sensitivity;
            case AllSettingsName.FullscreenMode: return FullscreenMode;
            case AllSettingsName.ScreenResolution: return ScreenResolution;
            case AllSettingsName.FPSLock: return FPSLock;
            case AllSettingsName.VSync: return VSync;
            case AllSettingsName.TextureQuality: return TextureQuality;
            case AllSettingsName.ShadowQuality: return ShadowQuality;
            case AllSettingsName.MasterVolume: return MasterVolume;
            case AllSettingsName.MusicVolume: return MusicVolume;
            case AllSettingsName.SFXVolume: return SFXVolume;
            case AllSettingsName.UIVolume: return UIVolume;
            case AllSettingsName.OutputDevice: return OutputDevice;
            default: return name.ToString();
        }
    }
}
