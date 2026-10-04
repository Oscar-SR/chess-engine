using UnityEngine;
using Ajedrez.Utilities;

public static class EngineBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeEngine()
    {
        AjedrezUtils.Initialize();
        BitboardUtils.Initialize();
        ZobristHashing.Initialize();
        Debug.Log("Engine initialized.");
    }
}