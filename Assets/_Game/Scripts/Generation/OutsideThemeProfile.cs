using UnityEngine;

namespace MurinoHDR.Generation
{

[CreateAssetMenu(menuName = "Murino/Generation/Outside Theme Profile", fileName = "OutsideThemeProfile")]
public sealed class OutsideThemeProfile : ScriptableObject
{
    [SerializeField] private string _themeName = "Default";
    [SerializeField] private Material _skyboxMaterial;
    [SerializeField] private Color _ambientColor = new Color(0.55f, 0.58f, 0.62f);
    [SerializeField] private Color _windowTint = new Color(0.72f, 0.84f, 0.96f, 0.72f);
    [SerializeField] private int _particlePresetId;
    [SerializeField] private AudioClip _ambientLoop;

    public string ThemeName => _themeName;
    public Material SkyboxMaterial => _skyboxMaterial;
    public Color AmbientColor => _ambientColor;
    public Color WindowTint => _windowTint;
    public int ParticlePresetId => _particlePresetId;
    public AudioClip AmbientLoop => _ambientLoop;
}
}

