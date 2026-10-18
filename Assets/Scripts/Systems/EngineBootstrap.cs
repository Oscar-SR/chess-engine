using UnityEngine;
using Chess.Utilities;

namespace Chess.Systems
{
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
}