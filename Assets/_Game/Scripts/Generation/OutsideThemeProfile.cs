using UnityEngine;

namespace MurinoHDR.Generation
{

[CreateAssetMenu(menuName = "Murino/Generation/Outside Theme Profile", fileName = "OutsideThemeProfile")]
public sealed class OutsideThemeProfile : ScriptableObject
{
    [SerializeField] private string _themeName = "Default";
    [SerializeField] private Material _skyboxMaterial;
    [SerializeField] private Color _ambientColor = new Color(0.55f, 0.58f, 0.62f);
    [SerializeField] private Color _directionalLightColor = new Color(0.82f, 0.84f, 0.9f);
    [SerializeField] private float _directionalLightIntensity = 0.18f;
    [SerializeField] private float _ambientIntensity = 1f;
    [SerializeField] private Color _windowTint = new Color(0.72f, 0.84f, 0.96f, 0.72f);
    [SerializeField] private Color _particleTint = new Color(0.8f, 0.85f, 0.92f, 0.5f);
    [SerializeField] private float _particleRate = 10f;
    [SerializeField] private int _particlePresetId;
    [SerializeField] private AudioClip _ambientLoop;

    public string ThemeName => _themeName;
    public Material SkyboxMaterial => _skyboxMaterial;
    public Color AmbientColor => _ambientColor;
    public Color DirectionalLightColor => _directionalLightColor;
    public float DirectionalLightIntensity => _directionalLightIntensity;
    public float AmbientIntensity => _ambientIntensity;
    public Color WindowTint => _windowTint;
    public Color ParticleTint => _particleTint;
    public float ParticleRate => _particleRate;
    public int ParticlePresetId => _particlePresetId;
    public AudioClip AmbientLoop => _ambientLoop;

    public void Configure(
        string themeName,
        Material skyboxMaterial,
        Color ambientColor,
        Color directionalLightColor,
        float directionalLightIntensity,
        float ambientIntensity,
        Color windowTint,
        Color particleTint,
        float particleRate,
        int particlePresetId,
        AudioClip ambientLoop)
    {
        _themeName = string.IsNullOrEmpty(themeName) ? "Default" : themeName;
        _skyboxMaterial = skyboxMaterial;
        _ambientColor = ambientColor;
        _directionalLightColor = directionalLightColor;
        _directionalLightIntensity = directionalLightIntensity;
        _ambientIntensity = ambientIntensity;
        _windowTint = windowTint;
        _particleTint = particleTint;
        _particleRate = particleRate;
        _particlePresetId = particlePresetId;
        _ambientLoop = ambientLoop;
    }
}
}

