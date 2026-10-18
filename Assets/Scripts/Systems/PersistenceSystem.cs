using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Chess.Core;

namespace Chess.Systems
{
    public class PersistenceSystem : MonoBehaviour
    {
        public static PersistenceSystem Instance { get; private set; }

        public Game game;
        public bool whiteOnBottom;

        private void Awake()
        {
            // Ensure only one instance exists
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool HasGame()
        {
            return game != null;
        }
    }
}