using UnityEngine;

namespace MurinoHDR.Generation
{

public sealed class GeneratedDoorAnchor : MonoBehaviour
{
    [SerializeField] private GeneratedDoorVisualType _visualType;

    public GeneratedDoorVisualType VisualType => _visualType;

    public void Configure(GeneratedDoorVisualType visualType)
    {
        _visualType = visualType;
    }
}
}
