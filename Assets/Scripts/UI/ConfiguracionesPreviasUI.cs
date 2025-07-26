using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;
using Ajedrez.Core;
using Ajedrez.Managers;

namespace Ajedrez.UI
{
    public class ConfiguracionesPreviasUI : MonoBehaviour
    {
        public enum DuracionPartida
        {
            UnMinuto = 1,
            TresMinutos = 3,
            CincoMinutos = 5,
            DiezMinutos = 10,
            QuinceMinutos = 15,
            TreintaMinutos = 30,
            SesentaMinutos = 60
        }

        public enum Incremento
        {
            Ninguno = 0,
            UnSegundo = 1,
            DosSegundos = 2,
            TresSegundos = 3,
            CincoSegundos = 5,
            DiezSegundos = 10,
            TreintaSegundos = 30
        }

        [SerializeField] private TMP_Dropdown selectorTiempo;
        [SerializeField] private TMP_Dropdown selectorIncremento;
        [SerializeField] private TMP_Dropdown selectorDificultad1;
        [SerializeField] private TMP_Dropdown selectorDificultad2;
        [SerializeField] private TMP_Dropdown selectorJugador1;
        [SerializeField] private TMP_Dropdown selectorJugador2;
        [SerializeField] private TMP_InputField nombreJugador1;
        [SerializeField] private TMP_InputField nombreJugador2;
        [SerializeField] private TMP_InputField posicionFEN;
        [SerializeField] private TMP_Text textoError;
        [SerializeField] private TMP_Text textoJugador1;
        [SerializeField] private TMP_Text textoJugador2;
        [SerializeField] private Button botonEmpezar;
        [SerializeField] private ConfiguracionesPreviasManager configuracionesPreviasManager;

        // Start is called before the first frame update
        void Start()
        {
            selectorJugador1.onValueChanged.AddListener(OnSelectorJugador1Changed);
            selectorJugador2.onValueChanged.AddListener(OnSelectorJugador2Changed);
            selectorTiempo.onValueChanged.AddListener(OnDuracionChanged);
            selectorIncremento.onValueChanged.AddListener(OnIncrementoChanged);
            posicionFEN.onValueChanged.AddListener(OnFenCambiado);
            nombreJugador1.onEndEdit.AddListener(OnNombreJugador1EndEdit);
            nombreJugador2.onEndEdit.AddListener(OnNombreJugador2EndEdit);
            selectorDificultad1.onValueChanged.AddListener(OnDificultadJugador1Changed);
            selectorDificultad2.onValueChanged.AddListener(OnDificultadJugador2Changed);
            CargarDropdowns();
        }

        private void OnSelectorJugador1Changed(int indice)
        {
            Jugador.Tipo tipoSeleccionado = (Jugador.Tipo)indice;

            if (tipoSeleccionado == Jugador.Tipo.Humano)
            {
                nombreJugador1.gameObject.SetActive(true);
                selectorDificultad1.gameObject.SetActive(false);
                configuracionesPreviasManager.CargarJugadorHumano1();
            }
            else
            {
                selectorDificultad1.gameObject.SetActive(true);
                nombreJugador1.gameObject.SetActive(false);
                configuracionesPreviasManager.CargarJugadorIA1();
            }
        }

        private void OnSelectorJugador2Changed(int indice)
        {
            Jugador.Tipo tipoSeleccionado = (Jugador.Tipo)indice;

            if (tipoSeleccionado == Jugador.Tipo.Humano)
            {
                nombreJugador2.gameObject.SetActive(true);
                selectorDificultad2.gameObject.SetActive(false);
                configuracionesPreviasManager.CargarJugadorHumano2();
            }
            else
            {
                selectorDificultad2.gameObject.SetActive(true);
                nombreJugador2.gameObject.SetActive(false);
                configuracionesPreviasManager.CargarJugadorIA2();
            }
        }

        private void OnFenCambiado(string fen)
        {
            configuracionesPreviasManager.CambiarPosicionTablero(fen);
        }

        public void MostrarError(string mensaje)
        {
            textoError.text = mensaje;
            botonEmpezar.interactable = false;
        }

        public void LimpiarError()
        {
            textoError.text = "";
            botonEmpezar.interactable = true;
        }

        private void CargarDropdowns()
        {
            selectorTiempo.ClearOptions();
            selectorIncremento.ClearOptions();
            selectorDificultad1.ClearOptions();
            selectorDificultad2.ClearOptions();
            selectorJugador1.ClearOptions();
            selectorJugador2.ClearOptions();

            // Crear lista de opciones legibles
            List<TMP_Dropdown.OptionData> opciones = new List<TMP_Dropdown.OptionData>();
            foreach (DuracionPartida duracion in Enum.GetValues(typeof(DuracionPartida)))
            {
                opciones.Add(new TMP_Dropdown.OptionData(((int)duracion).ToString()));
            }
            selectorTiempo.AddOptions(opciones);

            opciones.Clear();
            foreach (Incremento incremento in Enum.GetValues(typeof(Incremento)))
            {
                opciones.Add(new TMP_Dropdown.OptionData(((int)incremento).ToString()));
            }
            selectorIncremento.AddOptions(opciones);

            opciones.Clear();
            foreach (ConfiguracionIA.TipoDificultad dificultad in Enum.GetValues(typeof(ConfiguracionIA.TipoDificultad)))
            {
                opciones.Add(new TMP_Dropdown.OptionData(dificultad.ToString()));
            }
            selectorDificultad1.AddOptions(opciones);
            selectorDificultad2.AddOptions(opciones);

            opciones.Clear();
            foreach (Jugador.Tipo jugador in Enum.GetValues(typeof(Jugador.Tipo)))
            {
                opciones.Add(new TMP_Dropdown.OptionData(jugador.ToString()));
            }
            selectorJugador1.AddOptions(opciones);
            selectorJugador2.AddOptions(opciones);
        }

        private void CambiarColores()
        {
            string temp = textoJugador1.text;
            textoJugador1.text = textoJugador2.text;
            textoJugador2.text = temp;
            configuracionesPreviasManager.CambiarColores();
        }

        private void OnDuracionChanged(int indice)
        {
            // Obtener el texto mostrado en el dropdown, que es el número de minutos
            string textoSeleccionado = selectorTiempo.options[indice].text;

            // Convertir minutos a segundos
            if (int.TryParse(textoSeleccionado, out int minutos))
            {
                configuracionesPreviasManager.CambiarDuracion(minutos * 60f);
            }
        }

        private void OnIncrementoChanged(int indice)
        {
            // Obtener el texto mostrado en el dropdown, que es el número de segundos
            string textoSeleccionado = selectorIncremento.options[indice].text;

            if (int.TryParse(textoSeleccionado, out int segundos))
            {
                configuracionesPreviasManager.CambiarIncremento(segundos);
            }
        }

        private void OnNombreJugador1EndEdit(string nombre)
        {
            if (!string.IsNullOrWhiteSpace(nombre))
            {
                configuracionesPreviasManager.CambiarNombreJugador1(nombre);
            }
        }

        private void OnNombreJugador2EndEdit(string nombre)
        {
            if (!string.IsNullOrWhiteSpace(nombre))
            {
                configuracionesPreviasManager.CambiarNombreJugador2(nombre);
            }
        }

        private void OnDificultadJugador1Changed(int indice)
        {
            ConfiguracionIA.TipoDificultad dificultad = (ConfiguracionIA.TipoDificultad)indice;
            configuracionesPreviasManager.CambiarDificultadJugador1(dificultad);
        }

        private void OnDificultadJugador2Changed(int indice)
        {
            ConfiguracionIA.TipoDificultad dificultad = (ConfiguracionIA.TipoDificultad)indice;
            configuracionesPreviasManager.CambiarDificultadJugador2(dificultad);
        }

        public void BotonEmpezar()
        {
            configuracionesPreviasManager.EmpezarPartida();
        }

        public void BotonVolver()
        {
            configuracionesPreviasManager.VolverAlMenuPrincipal();
        }
    }
}