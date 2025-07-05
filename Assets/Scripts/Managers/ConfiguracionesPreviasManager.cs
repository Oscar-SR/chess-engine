using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using Ajedrez.Core;
using Ajedrez.UI;
using Ajedrez.Systems;

namespace Ajedrez.Managers
{
    public class ConfiguracionesPreviasManager : MonoBehaviour
    {
        [SerializeField] private ConfiguracionesPreviasUI configuracionesPreviasUI;
        [SerializeField] private TableroUI tableroUI;

        private Tablero tablero;

        // Start is called before the first frame update
        void Start()
        {
            tablero = new Tablero();
            tableroUI.Init(tablero, interactuable: false);
        }

        public void CambiarPosicionTablero(string fen)
        {
            try
            {
                if (string.IsNullOrEmpty(fen))
                {
                    tablero = new Tablero();
                }
                else
                {
                    tablero = new Tablero(fen);
                }

                configuracionesPreviasUI.LimpiarError();
            }
            catch (ArgumentException /*ex*/)
            {
                //Debug.LogError(ex.Message);
                tablero = null;
                configuracionesPreviasUI.MostrarError("La posición FEN del tablero es incorrecta");
            }

            tableroUI.RegenerarPiezas(tablero);
        }

        public void CambiarColores()
        {
            tableroUI.CambiarPerspectiva(tablero);
        }

        public void EmpezarPartida()
        {
            float duracion = configuracionesPreviasUI.ObtenerDuracion();
            float incremento = configuracionesPreviasUI.ObtenerIncremento();
            (Pieza.Color colorJugador1, Pieza.Color colorJugador2) = tableroUI.ObtenerColoresJugadores();
            TextAsset archivoTexto = Resources.Load<TextAsset>("Aperturas");
            string libroAperturas = archivoTexto.text;

            Jugador jugador1;
            if (configuracionesPreviasUI.ObtenerTipoJugador1() == Jugador.Tipo.Humano)
            {
                jugador1 = new JugadorHumano(configuracionesPreviasUI.ObtenerNombreJugador1(), colorJugador1, duracion);
            }
            else
            {
                jugador1 = new JugadorIA(configuracionesPreviasUI.ObtenerDificultadJugador1(), colorJugador1, duracion, tablero, libroAperturas, incremento);
            }

            Jugador jugador2;
            if (configuracionesPreviasUI.ObtenerTipoJugador2() == Jugador.Tipo.Humano)
            {
                jugador2 = new JugadorHumano(configuracionesPreviasUI.ObtenerNombreJugador2(), colorJugador2, duracion);
            }
            else
            {
                jugador2 = new JugadorIA(configuracionesPreviasUI.ObtenerDificultadJugador2(), colorJugador2, duracion, tablero, libroAperturas, incremento);
            }

            Partida partida = new Partida(tablero, jugador1, jugador2, duracion, incremento);
            PersistenciaSystem.Instancia.partida = partida;
            SceneManager.LoadScene("Partida");
        }

        public void VolverAlMenuPrincipal()
        {
            SceneManager.LoadScene("MenuPrincipal");
        }
    }
}