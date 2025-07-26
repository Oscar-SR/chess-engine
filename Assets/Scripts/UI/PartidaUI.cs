using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Ajedrez.Core;

namespace Ajedrez.UI
{
    public class PartidaUI : MonoBehaviour
    {
        private const float TIEMPO_ESPERA_RESULTADO = 3;

        [SerializeField] private Text textoSituacionTablero;
        [SerializeField] private Text textoTipoResultado;
        [SerializeField] private Image lineaDivisoria;
        [SerializeField] private TextMeshProUGUI textoTiempoJugadorAbajo;
        [SerializeField] private TextMeshProUGUI textoTiempoJugadorArriba;
        [SerializeField] private TextMeshProUGUI textoNombreJugadorAbajo;
        [SerializeField] private TextMeshProUGUI textoNombreJugadorArriba;
        [SerializeField] private GameObject panelResultado;
        [SerializeField] private Color colorTextoTiempo;
        [SerializeField] private Color colorTextoTiempoEscaso;

        private TextMeshProUGUI textoTiempoJugadorBlancas;
        private TextMeshProUGUI textoTiempoJugadorNegras;
        private TextMeshProUGUI textoNombreJugadorBlancas;
        private TextMeshProUGUI textoNombreJugadorNegras;

        public void Init(string nombreJugadorBlancas, string nombreJugadorNegras, float duracion, bool blancasAbajo = true)
        {
            if (blancasAbajo)
            {
                textoNombreJugadorBlancas = textoNombreJugadorAbajo;
                textoNombreJugadorNegras = textoNombreJugadorArriba;
                textoTiempoJugadorBlancas = textoTiempoJugadorAbajo;
                textoTiempoJugadorNegras = textoTiempoJugadorArriba;
            }
            else
            {
                textoNombreJugadorBlancas = textoNombreJugadorArriba;
                textoNombreJugadorNegras = textoNombreJugadorAbajo;
                textoTiempoJugadorBlancas = textoTiempoJugadorArriba;
                textoTiempoJugadorNegras = textoTiempoJugadorAbajo;
            }

            FormatearNombre(nombreJugadorBlancas, textoNombreJugadorAbajo);
            FormatearNombre(nombreJugadorNegras, textoNombreJugadorArriba);
            FormatearTiempo(duracion, textoTiempoJugadorAbajo);
            FormatearTiempo(duracion, textoTiempoJugadorArriba);
        }

        public void FormatearTiempoJugadorBlancas(float tiempo)
        {
            FormatearTiempo(tiempo, textoTiempoJugadorBlancas);
        }

        public void FormatearTiempoJugadorNegras(float tiempo)
        {
            FormatearTiempo(tiempo, textoTiempoJugadorNegras);
        }

        public void FormatearNombreJugadorBlancas(string nombre)
        {
            FormatearNombre(nombre, textoNombreJugadorBlancas);
        }

        public void FormatearNombreJugadorNegras(string nombre)
        {
            FormatearNombre(nombre, textoNombreJugadorNegras);
        }

        private void FormatearTiempo(float tiempo, TextMeshProUGUI textoTiempo)
        {
            int minutos = Mathf.FloorToInt(tiempo / 60f);
            int segundos = Mathf.FloorToInt(tiempo % 60f);
            string tiempoFormateado;

            // Si el tiempo es menor a 10 segundos, mostrar milisegundos
            if (tiempo < 10f)
            {
                // Obtener solo una cifra de milisegundos
                int milisegundos = Mathf.FloorToInt((tiempo % 1) * 10);
                // Formato: MM:SS.m
                tiempoFormateado = $"{minutos:00}:{segundos:00}.{milisegundos}";

                textoTiempo.color = colorTextoTiempoEscaso;
            }
            else
            {
                // Formato: MM:SS sin milisegundos
                tiempoFormateado = $"{minutos:00}:{segundos:00}";

                textoTiempo.color = colorTextoTiempo;
            }

            textoTiempo.text = tiempoFormateado;
        }

        private void FormatearNombre(string nombre, TextMeshProUGUI textoJugador)
        {
            textoJugador.text = nombre;
        }

        public void TransparentarTiempoJugadorBlancas()
        {
            textoTiempoJugadorBlancas.color = new Color(textoTiempoJugadorBlancas.color.r, textoTiempoJugadorBlancas.color.g, textoTiempoJugadorBlancas.color.b, 0.5f);
        }

        public void TransparentarTiempoJugadorNegras()
        {
            textoTiempoJugadorNegras.color = new Color(textoTiempoJugadorNegras.color.r, textoTiempoJugadorNegras.color.g, textoTiempoJugadorNegras.color.b, 0.5f);
        }

        public void FinalizarPartida(SituacionPartida.Tipo resultado)
        {
            lineaDivisoria.enabled = false;
            textoSituacionTablero.text = resultado.ObtenerDescripcion();
            StartCoroutine(MostrarResultadoTrasRetraso(resultado));
        }

        private IEnumerator MostrarResultadoTrasRetraso(SituacionPartida.Tipo resultado)
        {
            yield return new WaitForSeconds(TIEMPO_ESPERA_RESULTADO);

            Pieza.Color ganador = SituacionPartida.ObtenerGanador(resultado);
            if (ganador == Pieza.Color.Nada)
            {
                textoTipoResultado.text = "Tablas";
            }
            else
            {
                textoTipoResultado.text = $"Ganan las {ganador}";
            }

            panelResultado.SetActive(true);
        }

        public void BotonSalir()
        {
            SceneManager.LoadScene("MenuPrincipal");
        }
    }
}