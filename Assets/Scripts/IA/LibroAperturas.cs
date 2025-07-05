using System;
using System.Collections.Generic;
using Ajedrez.Core;

namespace Ajedrez.IA
{
    public class LibroAperturas
    {
        public const int MAX_MOVIMIENTOS_LIBRO = 8;

        readonly Dictionary<string, MovimientoLibro[]> movimientosPorPosicion;
        readonly Random rng;

        public readonly struct MovimientoLibro
        {
            private readonly string movimientoLAN;
            private readonly int numVecesJugado;

            public MovimientoLibro(string movimientoLAN, int numVecesJugado)
            {
                this.movimientoLAN = movimientoLAN;
                this.numVecesJugado = numVecesJugado;
            }

            public string MovimientoLAN => movimientoLAN;
            public int NumVecesJugado => numVecesJugado;
        }

        public LibroAperturas(string fichero)
        {
            rng = new Random();
            Span<string> entradas = fichero.Trim(new char[] { ' ', '\n' }).Split("pos").AsSpan(1);
            movimientosPorPosicion = new Dictionary<string, MovimientoLibro[]>(entradas.Length);

            for (int i = 0; i < entradas.Length; i++)
            {
                string[] entrada = entradas[i].Trim('\n').Split('\n');
                string posicionFen = entrada[0].Trim();
                Span<string> datosMovimientos = entrada.AsSpan(1);

                MovimientoLibro[] movimientosLibro = new MovimientoLibro[datosMovimientos.Length];

                for (int indiceMovimiento = 0; indiceMovimiento < movimientosLibro.Length; indiceMovimiento++)
                {
                    string[] datosMovimiento = datosMovimientos[indiceMovimiento].Split(' ');
                    movimientosLibro[indiceMovimiento] = new MovimientoLibro(datosMovimiento[0], int.Parse(datosMovimiento[1]));
                }

                movimientosPorPosicion.Add(posicionFen, movimientosLibro);
            }
        }

        public bool HayMovimiento(string posicionFen)
        {
            return movimientosPorPosicion.ContainsKey(EliminarContadorFEN(posicionFen));
        }

        private string EliminarContadorFEN(string fen)
        {
            string fenA = fen.Substring(0, fen.LastIndexOf(' '));
            return fenA.Substring(0, fenA.LastIndexOf(' '));
        }

        // pesoPotencia es un valor entre 0 y 1.
        // 0 => todos los movimientos tienen igual probabilidad
        // 1 => la probabilidad es proporcional al número de veces jugado
		public bool TryGetValue(string posicionFen, out string movimientoElegido, double pesoPotencia = 0.5)
        {
            // Busca los movimientos posibles para la posición (sin los contadores de movimientos del FEN)
            if (movimientosPorPosicion.TryGetValue(EliminarContadorFEN(posicionFen), out MovimientoLibro[] movimientosLibro))
            {
                // Asegura que pesoPotencia esté dentro del rango permitido (0 a 1)
                pesoPotencia = Math.Clamp(pesoPotencia, 0, 1);

                // Calcula el total de los pesos (jugadas ponderadas)
                int totalVecesJugados = 0;
                foreach (MovimientoLibro movimientoLibro in movimientosLibro)
                {
                    totalVecesJugados += CalcularPeso(movimientoLibro.NumVecesJugado);
                }

                // Calcula el peso normalizado de cada movimiento y suma los pesos
                double[] pesos = new double[movimientosLibro.Length];
                double sumaPesos = 0;
                for (int i = 0; i < movimientosLibro.Length; i++)
                {
                    double peso = CalcularPeso(movimientosLibro[i].NumVecesJugado) / (double)totalVecesJugados;
                    sumaPesos += peso;
                    pesos[i] = peso;
                }

                // Calcula la probabilidad acumulada de cada movimiento
                double[] probabilidadAcumulada = new double[movimientosLibro.Length];
                for (int i = 0; i < pesos.Length; i++)
                {
                    double probabilidad = pesos[i] / sumaPesos;
                    probabilidadAcumulada[i] = probabilidadAcumulada[Math.Max(0, i - 1)] + probabilidad;
                }

                // Selecciona el movimiento correspondiente según la probabilidad acumulada y en base a un número aleatorio entre 0 y 1
                double random = rng.NextDouble();
                for (int i = 0; i < movimientosLibro.Length; i++)
                {
                    if (random <= probabilidadAcumulada[i])
                    {
                        movimientoElegido = movimientosLibro[i].MovimientoLAN;
                        return true;
                    }
                }
            }

            movimientoElegido = "Nulo";
            return false;

            // Método local: calcula el peso de un movimiento según el número de veces jugado elevado al pesoPotencia
            int CalcularPeso(int numVecesJugado) => (int)Math.Ceiling(Math.Pow(numVecesJugado, pesoPotencia));
        }
    }
}