using UnityEngine;

namespace MurinoHDR.Audio;

public sealed class AudioManager : MonoBehaviour
{
    public void PlayOneShot(string eventId)
    {
        Debug.Log($"[AUDIO] PlayOneShot: {eventId}");
    }
}
