using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ajedrez.Core;

namespace Ajedrez.Systems
{
    public class PersistenciaSystem : MonoBehaviour
    {
        public static PersistenciaSystem Instancia { get; private set; }

        public Partida partida;

        private void Awake()
        {
            // Asegura que solo haya una instancia
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }

            Instancia = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool HayPartida()
        {
            return partida != null;
        }
    }
}