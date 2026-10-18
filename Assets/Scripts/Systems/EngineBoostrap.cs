using UnityEngine;
using Chess.Utilities;

public static class EngineBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeEngine()
    {
        ChessUtils.Initialize();
        BitboardUtils.Initialize();
        ZobristHashing.Initialize();
        Debug.Log("Engine initialized.");
    }
}