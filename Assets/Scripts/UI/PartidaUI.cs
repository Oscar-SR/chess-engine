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
        [SerializeField] private TextMeshProUGUI textoTiempoJugador1;
        [SerializeField] private TextMeshProUGUI textoTiempoJugador2;
        [SerializeField] private TextMeshProUGUI textoNombreJugador1;
        [SerializeField] private TextMeshProUGUI textoNombreJugador2;
        [SerializeField] private GameObject panelResultado;
        [SerializeField] private Color colorTextoTiempo;
        [SerializeField] private Color colorTextoTiempoEscaso;

        private TextMeshProUGUI textoTiempoJugadorBlancas;
        private TextMeshProUGUI textoTiempoJugadorNegras;
        private TextMeshProUGUI textoNombreJugadorBlancas;
        private TextMeshProUGUI textoNombreJugadorNegras;

        public void Init(string nombreJugador1, string nombreJugador2, float duracion, bool blancasAbajo = true)
        {
            if (blancasAbajo)
            {
                textoNombreJugadorBlancas = textoNombreJugador1;
                textoNombreJugadorNegras = textoNombreJugador2;
                textoTiempoJugadorBlancas = textoTiempoJugador1;
                textoTiempoJugadorNegras = textoTiempoJugador2;
            }
            else
            {
                textoNombreJugadorBlancas = textoNombreJugador2;
                textoNombreJugadorNegras = textoNombreJugador1;
                textoTiempoJugadorBlancas = textoTiempoJugador2;
                textoTiempoJugadorNegras = textoTiempoJugador1;
            }

            FormatearNombre(nombreJugador1, textoNombreJugador1);
            FormatearNombre(nombreJugador2, textoNombreJugador2);
            FormatearTiempo(duracion, textoTiempoJugador1);
            FormatearTiempo(duracion, textoTiempoJugador2);
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

        public void ActivarLineaDivisoria()
        {
            lineaDivisoria.enabled = true;
        }

        public void DesactivarLineaDivisoria()
        {
            lineaDivisoria.enabled = false;
        }

        public void MostrarSituacionTablero(Tablero.TipoSituacionTablero situacionTablero)
        {
            textoSituacionTablero.text = ObtenerTexto(situacionTablero);
        }

        public void FinalizarPartida(Tablero.TipoSituacionTablero situacionTablero, Pieza.Color ganador)
        {
            lineaDivisoria.enabled = false;
            textoSituacionTablero.text = ObtenerTexto(situacionTablero);
            StartCoroutine(MostrarResultadoTrasRetraso(situacionTablero, ganador));
        }

        private string ObtenerTexto(Tablero.TipoSituacionTablero situacionTablero)
        {
            return situacionTablero switch
            {
                Tablero.TipoSituacionTablero.Normal => "",
                Tablero.TipoSituacionTablero.Jaque => "¡Jaque!",
                Tablero.TipoSituacionTablero.JaqueMate => "¡Jaque Mate!",
                Tablero.TipoSituacionTablero.ReyAhogado => "Rey ahogado",
                Tablero.TipoSituacionTablero.TriplePosicionRepetida => "Triple repetición\n de posición",
                Tablero.TipoSituacionTablero.Regla50Movimientos => "Regla de los\n50 movimientos",
                Tablero.TipoSituacionTablero.MaterialInsuficiente => "Material insuficiente",
                Tablero.TipoSituacionTablero.SinTiempo => "¡Sin tiempo!",
                _ => ""
            };
        }

        private IEnumerator MostrarResultadoTrasRetraso(Tablero.TipoSituacionTablero situacionTablero, Pieza.Color ganador)
        {
            yield return new WaitForSeconds(TIEMPO_ESPERA_RESULTADO);

            panelResultado.SetActive(true);

            switch (situacionTablero)
            {
                case Tablero.TipoSituacionTablero.JaqueMate:
                case Tablero.TipoSituacionTablero.SinTiempo:
                    {
                        textoTipoResultado.text = $"Ganan las {ganador}";
                        break;
                    }

                case Tablero.TipoSituacionTablero.ReyAhogado:
                case Tablero.TipoSituacionTablero.TriplePosicionRepetida:
                case Tablero.TipoSituacionTablero.Regla50Movimientos:
                case Tablero.TipoSituacionTablero.MaterialInsuficiente:
                    {
                        textoTipoResultado.text = "Tablas";
                        break;
                    }
            }
        }

        public void BotonSalir()
        {
            SceneManager.LoadScene("MenuPrincipal");
        }
    }
}