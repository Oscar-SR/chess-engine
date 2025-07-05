using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ajedrez.Managers;
using Ajedrez.IA;

namespace Ajedrez.Core
{
    public class JugadorIA : Jugador
    {
        public enum TipoDificultad : byte
        {
            Facil,
            Medio,
            Dificil
        }

        private TipoDificultad dificultad;
        private Busqueda busqueda;
        private LibroAperturas libroAperturas;
        private GestorTiempo gestorTiempo;

        public JugadorIA(TipoDificultad dificultad, Pieza.Color color, float tiempoRestante, Tablero tablero, string ficheroAperturas, float incremento)
            : base("IA " + dificultad, color, tiempoRestante)
        {
            this.dificultad = dificultad;
            switch (dificultad)
            {
                case TipoDificultad.Facil:
                    busqueda = new Busqueda(tablero, 2);
                    break;

                case TipoDificultad.Medio:
                    busqueda = new Busqueda(tablero, 4);
                    break;

                case TipoDificultad.Dificil:
                    busqueda = new Busqueda(tablero);
                    break;

                default:
                    busqueda = new Busqueda(tablero);
                    break;
            }
            libroAperturas = new LibroAperturas(ficheroAperturas);
            gestorTiempo = new GestorTiempo(incremento);
        }

        public bool BuscarMovimientoLibro(string posicionFen, out string movimientoLAN)
        {
            return libroAperturas.TryGetValue(posicionFen, out movimientoLAN);
        }

        public int ObtenerTiempoBusqueda(uint movimientoNumero)
        {
            return gestorTiempo.CalcularTiempoBusqueda(tiempoRestante, movimientoNumero);
        }

        public Movimiento EmpezarBusqueda(List<Movimiento> movimientos)
        {
            return busqueda.EmpezarBusqueda(movimientos);
        }

        public void TerminarBusqueda()
        {
            busqueda.TerminarBusqueda();
        }

        public Busqueda.DiagnosticoBusqueda ObtenerDiagnostico()
        {
            return busqueda.Diagnostico;
        }
    }
}