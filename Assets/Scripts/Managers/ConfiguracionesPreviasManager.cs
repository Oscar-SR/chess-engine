using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using Ajedrez.Core;
using Ajedrez.UI;
using Ajedrez.IA;
using Ajedrez.Systems;

namespace Ajedrez.Managers
{
    public class ConfiguracionesPreviasManager : MonoBehaviour
    {
        [SerializeField] private ConfiguracionesPreviasUI configuracionesPreviasUI;
        [SerializeField] private TableroUI tableroUI;

        private Partida partida;
        private bool blancasAbajo = true;
        private TextAsset libroAperturas;

        // Start is called before the first frame update
        void Start()
        {
            libroAperturas = Resources.Load<TextAsset>("Aperturas");
            partida = new Partida(new Tablero(), new JugadorHumano("Jugador 1", Pieza.Color.Blancas), new JugadorHumano("Jugador 2", Pieza.Color.Negras), new Reloj(60f, 0f));
            tableroUI.Init(partida.Tablero);
        }

        public void CambiarPosicionTablero(string fen)
        {
            try
            {
                if (string.IsNullOrEmpty(fen))
                {
                    partida.Tablero = new Tablero();
                }
                else
                {
                    partida.Tablero = new Tablero(fen);
                }

                // Actualizar representación del tablero de los jugadores IA si los hay
                if (partida.JugadorBlancas is JugadorIA jugadorBlancasIA)
                {
                    jugadorBlancasIA.Tablero = partida.Tablero;
                }
                if (partida.JugadorNegras is JugadorIA jugadorNegrasIA)
                {
                    jugadorNegrasIA.Tablero = partida.Tablero;
                }

                configuracionesPreviasUI.LimpiarError();
            }
            catch (ArgumentException /*ex*/)
            {
                //Debug.LogError(ex.Message);
                partida.Tablero = null;
                configuracionesPreviasUI.MostrarError("La posición FEN del tablero es incorrecta");
            }
            
            // Actualizar UI
            tableroUI.CambiarPosicion(partida.Tablero);
        }

        public void CargarJugadorHumano1()
        {
            Pieza.Color color = blancasAbajo ? Pieza.Color.Blancas : Pieza.Color.Negras;
            JugadorHumano jugador = new JugadorHumano("Jugador 1", color);

            if (color == Pieza.Color.Blancas)
            {
                partida.JugadorBlancas = jugador;
            }
            else
            {
                partida.JugadorNegras = jugador;
            }
        }
        
        public void CargarJugadorIA1()
        {
            Pieza.Color color = blancasAbajo ? Pieza.Color.Blancas : Pieza.Color.Negras;
            JugadorIA jugador = new JugadorIA(color, partida.Tablero, partida.Reloj, ConfiguracionIA.CrearFacil(libroAperturas));

            if (color == Pieza.Color.Blancas)
            {
                partida.JugadorBlancas = jugador;
            }
            else
            {
                partida.JugadorNegras = jugador;
            }
        }

        public void CargarJugadorHumano2()
        {
            Pieza.Color color = blancasAbajo ? Pieza.Color.Negras : Pieza.Color.Blancas;
            JugadorHumano jugador = new JugadorHumano("Jugador 2", color);

            if (color == Pieza.Color.Blancas)
            {
                partida.JugadorBlancas = jugador;
            }
            else
            {
                partida.JugadorNegras = jugador;
            }
        }
        
        public void CargarJugadorIA2()
        {
            Pieza.Color color = blancasAbajo ? Pieza.Color.Negras : Pieza.Color.Blancas;
            JugadorIA jugador = new JugadorIA(color, partida.Tablero, partida.Reloj, ConfiguracionIA.CrearFacil(libroAperturas));

            if (color == Pieza.Color.Blancas)
            {
                partida.JugadorBlancas = jugador;
            }
            else
            {
                partida.JugadorNegras = jugador;
            }
        }

        public void CambiarColores()
        {
            tableroUI.CambiarPerspectiva(partida.Tablero);
            blancasAbajo = !blancasAbajo;
            partida.IntercambiarColoresJugadores();
        }

        public void CambiarDuracion(float duracion)
        {
            partida.Reloj = new Reloj(duracion, partida.Reloj.IncrementoPorMovimiento);
        }

        public void CambiarIncremento(float incremento)
        {
            partida.Reloj = new Reloj(partida.Reloj.DuracionInicial, incremento);
        }

        public void CambiarNombreJugador1(string nombre)
        {
            if (blancasAbajo)
            {
                partida.JugadorBlancas.Nombre = nombre;
            }
            else
            {
                partida.JugadorNegras.Nombre = nombre;
            }
        }

        public void CambiarNombreJugador2(string nombre)
        {
            if (blancasAbajo)
            {
                partida.JugadorNegras.Nombre = nombre;
            }
            else
            {
                partida.JugadorBlancas.Nombre = nombre;
            }
        }

        private ConfiguracionIA ObtenerConfiguracion(ConfiguracionIA.TipoDificultad dificultad)
        {
            switch (dificultad)
            {
                case ConfiguracionIA.TipoDificultad.Facil:
                    return ConfiguracionIA.CrearFacil(libroAperturas);
                case ConfiguracionIA.TipoDificultad.Media:
                    return ConfiguracionIA.CrearMedia(libroAperturas);
                case ConfiguracionIA.TipoDificultad.Maxima:
                    return ConfiguracionIA.CrearMaxima(libroAperturas);
                default:
                    return ConfiguracionIA.CrearPersonalizada(Busqueda.TipoBusqueda.PorTiempo, ConfiguracionIA.TIEMPO_DINAMICO, false, 0, null);
            }
        }

        public void CambiarDificultadJugador1(ConfiguracionIA.TipoDificultad dificultad)
        {
            if (blancasAbajo)
            {
                ((JugadorIA)partida.JugadorBlancas).ConfiguracionIA = ObtenerConfiguracion(dificultad);
            }
            else
            {
                ((JugadorIA)partida.JugadorNegras).ConfiguracionIA = ObtenerConfiguracion(dificultad);
            }
        }

        public void CambiarDificultadJugador2(ConfiguracionIA.TipoDificultad dificultad)
        {
            if (blancasAbajo)
            {
                ((JugadorIA)partida.JugadorNegras).ConfiguracionIA = ObtenerConfiguracion(dificultad);
            }
            else
            {
                ((JugadorIA)partida.JugadorBlancas).ConfiguracionIA = ObtenerConfiguracion(dificultad);
            }
        }

        public void EmpezarPartida()
        {
            PersistenciaSystem.Instancia.partida = partida;
            PersistenciaSystem.Instancia.blancasAbajo = blancasAbajo;
            SceneManager.LoadScene("Partida");
        }

        public void VolverAlMenuPrincipal()
        {
            SceneManager.LoadScene("MenuPrincipal");
        }
    }
}