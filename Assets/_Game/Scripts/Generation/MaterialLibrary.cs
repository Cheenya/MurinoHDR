using UnityEngine;

namespace MurinoHDR.Generation
{

[CreateAssetMenu(menuName = "Murino/Generation/Material Library", fileName = "MaterialLibrary")]
public sealed class MaterialLibrary : ScriptableObject
{
    [SerializeField] private Material _wallMat;
    [SerializeField] private Material _floorMat;
    [SerializeField] private Material _ceilingMat;
    [SerializeField] private Material _glassMat;
    [SerializeField] private Material _trimMat;

    public Material WallMat => _wallMat;
    public Material FloorMat => _floorMat;
    public Material CeilingMat => _ceilingMat;
    public Material GlassMat => _glassMat;
    public Material TrimMat => _trimMat;
}
}
