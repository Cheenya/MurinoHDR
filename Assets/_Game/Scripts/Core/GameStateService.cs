using UnityEngine;

namespace MurinoHDR.Core;

public enum GameState
{
    Boot,
    Playing,
    Paused
}

public sealed class GameStateService : MonoBehaviour
{
    public GameState CurrentState { get; private set; } = GameState.Boot;

    public void SetState(GameState state)
    {
        CurrentState = state;
        Debug.Log($"[CORE] State -> {state}");
    }
}
