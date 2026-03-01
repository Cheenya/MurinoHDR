using MurinoHDR.Interaction;
using MurinoHDR.UI;
using UnityEngine;

namespace MurinoHDR.Player
{

public sealed class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private float _interactDistance = 3f;
    [SerializeField] private LayerMask _mask = ~0;

    private Camera _camera;

    private void Start()
    {
        _camera = GetComponentInChildren<Camera>();
    }

    private void Update()
    {
        if (_camera == null)
        {
            return;
        }

        var ray = new Ray(_camera.transform.position, _camera.transform.forward);
        if (Physics.Raycast(ray, out var hit, _interactDistance, _mask) && hit.collider.TryGetComponent<IInteractable>(out var interactable))
        {
            if (interactable.CanInteract(gameObject))
            {
                InteractionPromptUI.Instance?.Show($"ֽאזלטעו E: {interactable.GetPrompt(gameObject)}");
                if (Input.GetKeyDown(KeyCode.E))
                {
                    interactable.Interact(gameObject);
                }
                return;
            }
        }

        InteractionPromptUI.Instance?.Hide();
    }
}
}


