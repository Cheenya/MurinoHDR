using UnityEngine;

namespace MurinoHDR.Generation
{

public sealed class GeneratedFloorInfo : MonoBehaviour
{
    [SerializeField] private int _seed;
    [SerializeField] private string _validationSummary = string.Empty;

    public int Seed => _seed;
    public string ValidationSummary => _validationSummary;

    public void Configure(int seed, string validationSummary)
    {
        _seed = seed;
        _validationSummary = validationSummary;
    }
}
}
