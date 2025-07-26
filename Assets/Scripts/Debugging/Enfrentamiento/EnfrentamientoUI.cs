using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Ajedrez.Debugging.Enfrentamiento
{
    public class EnfrentamientoUI : MonoBehaviour
    {
        [SerializeField] private EnfrentamientoManager enfrentamientoManager;
        [SerializeField] private TMP_Text textoJugadores;
        [SerializeField] private TMP_Text textoEstadisticas;
        [SerializeField] private TMP_Text textoNombreBlancas;
        [SerializeField] private TMP_Text textoNombreNegras;
        [SerializeField] private TMP_Text textoDebug;
        [SerializeField] private TMP_InputField maxTiempoPensar;
        [SerializeField] private TMP_InputField maxMovimientosPartida;
        [SerializeField] private Button botonEmpezar;
        private const string colorVerde = "#26DA6F";

        // Start is called before the first frame update
        void Start()
        {
            // Establecer los ajustes de la ventana
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            QualitySettings.vSyncCount = 1;
            Application.runInBackground = true;
            textoJugadores.text = $"<color={colorVerde}>?</color> vs <color={colorVerde}>?</color>";
        }

        public void Init(int maxTiempoPensarInicial, int maxMovimientosPartidaInicial)
        {
            TMP_Text textoMaxTiempoPensar = (TMP_Text)maxTiempoPensar.placeholder;
            textoMaxTiempoPensar.text = maxTiempoPensarInicial.ToString();
            TMP_Text textoMaxMovimientosPartida = (TMP_Text)maxMovimientosPartida.placeholder;
            textoMaxMovimientosPartida.text = maxMovimientosPartidaInicial.ToString();
        }

        public void EscribirNombresJugadores(JugadorRemoto jugador1, JugadorRemoto jugador2)
        {
            string nombre1 = jugador1 != null ? jugador1.Nombre : "?";
            string nombre2 = jugador2 != null ? jugador2.Nombre : "?";
            textoJugadores.text = $"<color={colorVerde}>{nombre1}</color> vs <color={colorVerde}>{nombre2}</color>";
        }

        public void ActualizarEstadisticas(int numPartidas, int indicePartida, JugadorRemoto jugador1, JugadorRemoto jugador2)
        {
            textoEstadisticas.text = $"Número de partida: {Mathf.Min(indicePartida + 1, numPartidas)} / {numPartidas}";

            ActualizarInformacionJugador(jugador1);
            ActualizarInformacionJugador(jugador2);

            void ActualizarInformacionJugador(JugadorRemoto jugador)
            {
                if (jugador != null)
                {
                    float profundidadMedia = jugador.TotalProfundidadBuscada / (float)Mathf.Max(1, jugador.NumMovimientos);
                    textoEstadisticas.text += $"\n{jugador.Nombre}: Victorias: {jugador.NumVictorias} Derrotas: {jugador.NumDerrotas} Empates: {jugador.NumEmpates} Profundidad media: {profundidadMedia:0.00}";
                }
                else
                {
                    textoEstadisticas.text += "\nJugador no conectado";
                }
            }
        }

        public void ValidarMaxTiempoPensar(string texto)
        {
            if (int.TryParse(texto, out int valor) && valor > 0)
            {
                enfrentamientoManager.MaxTiempoPensar = valor;
            }
            else
            {
                maxTiempoPensar.text = "";
            }
        }

        public void ValidarMaxMovimientosPartida(string texto)
        {
            if (int.TryParse(texto, out int valor) && valor > 0)
            {
                enfrentamientoManager.MaxMovimientosPartida = valor;
            }
            else
            {
                maxMovimientosPartida.text = "";
            }
        }

        public void ActivarBotonEmpezar()
        {
            botonEmpezar.interactable = true;
        }

        public void PulsarBotonEmpezar()
        {
            botonEmpezar.interactable = false;
            enfrentamientoManager.EmpezarNuevaPartida();
        }

        public void EscribirDebug(string mensaje)
        {
            textoDebug.text += mensaje;
        }

        public void EstablecerNombreBlancas(string nombre)
        {
            textoNombreBlancas.text = nombre;
        }

        public void EstablecerNombreNegras(string nombre)
        {
            textoNombreNegras.text = nombre;
        }
    }
}