using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Ajedrez.Core;
using Ajedrez.Utilities;

namespace Ajedrez.Managers
{
    public class PerftManager : MonoBehaviour
    {
        private const string ROJO = "#B3472B";
        private const string VERDE = "#79AD51";
        private const string CIAN = "#6BCDAE";
        private const string GRIS = "#808080";
        
        [SerializeField] private TextMeshProUGUI textoPerft;
        [SerializeField] private TMP_InputField entradaFEN;
        [SerializeField] private TMP_InputField entradaLimiteProfundidad;
        [SerializeField] private Button botonPerft;
        [SerializeField] private Button botonDivide;
        [SerializeField] private Button botonDetenerEjecucion;
        [SerializeField] private RectTransform contenidoScrollView;

        private Tablero tablero;
        private CancellationTokenSource cancellationTokenSource;
        private CancellationToken cancellationToken;
        private SynchronizationContext unityContext;

        void Start()
        {
            // Capturar el contexto del hilo principal
            unityContext = SynchronizationContext.Current;
        }

        public void EjecutarTarea(string tarea)
        {
            DesactivarBotonesEjecucion();

            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }

            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;

            string fen = string.IsNullOrEmpty(entradaFEN.text)
                ? entradaFEN.placeholder.GetComponent<TMP_Text>().text
                : entradaFEN.text;

            int limiteProfundidad = (int.TryParse(entradaLimiteProfundidad.text, out int resultado) && resultado > 0) ? resultado : int.MaxValue;

            tablero = new Tablero(fen);

            switch(tarea)
            {
                case "TareaPerft":
                {
                    Task.Run(() => TareaPerft(cancellationToken, limiteProfundidad));
                    break;
                }

                case "TareaDivide":
                {
                    Task.Run(() => TareaDivide(cancellationToken, limiteProfundidad));
                    break;
                }

                default:
                {
                    UnityEngine.Debug.LogError("Tarea no encontrada");
                    ActivarBotonesEjecucion();
                    break;
                }
            }
        }

        public void DetenerEjecucion()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }

            ActivarBotonesEjecucion();
        }

        private void DesactivarBotonesEjecucion()
        {
            botonPerft.interactable = false;
            botonDivide.interactable = false;
            botonDetenerEjecucion.interactable = true;
        }

        private void ActivarBotonesEjecucion()
        {
            botonPerft.interactable = true;
            botonDivide.interactable = true;
            botonDetenerEjecucion.interactable = false;
        }

        private void VaciarTextPerft()
        {
            unityContext.Post(_ =>
            {
                textoPerft.text = "";
                LayoutRebuilder.ForceRebuildLayoutImmediate(contenidoScrollView);
            }, null);
        }

        private void EscribirTextPerft(string texto)
        {
            // Actualizar la UI en el hilo principal
            unityContext.Post(_ =>
            {
                textoPerft.text += texto;
                LayoutRebuilder.ForceRebuildLayoutImmediate(contenidoScrollView);
            }, null);
        }

        private async Task TareaPerft(CancellationToken token, int limiteProfundidad)
        {
            int profundidad = 1;
            VaciarTextPerft();

            while ((profundidad <= limiteProfundidad) && !token.IsCancellationRequested)
            {
                Stopwatch cronometro = new Stopwatch();

                cronometro.Start();
                int numPosiciones = Perft(profundidad);
                cronometro.Stop();

                EscribirTextPerft(
                    $"Profundidad: <color={VERDE}>{profundidad}</color> <color={GRIS}>ply</color>  " +
                    $"Búsqueda: <color={ROJO}>{numPosiciones:N0}</color> <color={GRIS}>posiciones</color>  " +
                    $"Tiempo: <color={CIAN}>{cronometro.ElapsedMilliseconds:N0}</color> <color={GRIS}>milisegundos</color>\n"
                );

                if (token.IsCancellationRequested)
                {
                    EscribirTextPerft($"<color={ROJO}>Ejecución cancelada</color>");
                    return;
                }

                profundidad++;
                await Task.Yield();
            }

            EscribirTextPerft($"<color={VERDE}>Test finalizado</color>");
            unityContext.Post(_ => ActivarBotonesEjecucion(), null); // Activar desde el hilo principal
        }

        private int Perft(int profundidad)
        {
            if (profundidad == 0)
                return 1;

            (List<Movimiento> movimientos, _) = tablero.GenerarMovimientosLegales();
            int numPosiciones = 0;

            foreach (Movimiento movimiento in movimientos)
            {                
                tablero.HacerMovimiento(movimiento);
                numPosiciones += Perft(profundidad - 1);
                tablero.DeshacerMovimiento(movimiento);

                if (cancellationToken.IsCancellationRequested)
                    break;
            }

            return numPosiciones;
        }

        private async Task TareaDivide(CancellationToken token, int limiteProfundidad)
        {
            (List<Movimiento> movimientos, _) = tablero.GenerarMovimientosLegales();
            int totalNodos = 0;
            VaciarTextPerft();

            foreach (Movimiento movimiento in movimientos)
            {
                tablero.HacerMovimiento(movimiento);
                int nodos = Perft(limiteProfundidad - 1);
                tablero.DeshacerMovimiento(movimiento);

                EscribirTextPerft($"{movimiento.ToLAN()}: <color={CIAN}>{nodos}</color>\n");

                if (token.IsCancellationRequested)
                {
                    EscribirTextPerft($"<color={ROJO}>Ejecución cancelada</color>");
                    return;
                }

                totalNodos += nodos;
                await Task.Yield();
            }

            EscribirTextPerft($"\nTotal de nodos: <color={CIAN}>{totalNodos}</color>\n<color={VERDE}>Test finalizado</color>");
            unityContext.Post(_ => ActivarBotonesEjecucion(), null); // Activar desde el hilo principal
        }
    }
}
