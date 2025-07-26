using System;
using System.Text;
using System.Collections.Generic;
using Ajedrez.Utilities;
using Ajedrez.IA;

namespace Ajedrez.Core
{
    public class Partida
    {
        private Tablero tablero;
        private Jugador jugadorBlancas;
        private Jugador jugadorNegras;
        private bool conTiempo;
        private Reloj reloj;
        private List<Movimiento> movimientosRealizados;
        private string fenInicioPartida;
        private SituacionPartida.Tipo situacion;
        private List<Movimiento> ultimosMovimientosLegales;
        private bool jaque;
        /*
        private uint numeroJaques;
        */

        public Partida()
        {
            conTiempo = false;
            movimientosRealizados = new List<Movimiento>();
            ultimosMovimientosLegales = new List<Movimiento>();
        }

        public Partida(Tablero tablero, Jugador jugadorBlancas, Jugador jugadorNegras, Reloj reloj)
        {
            conTiempo = true;
            this.tablero = tablero;
            this.jugadorBlancas = jugadorBlancas;
            this.jugadorNegras = jugadorNegras;
            this.reloj = reloj;
            fenInicioPartida = tablero.ToFEN();
            movimientosRealizados = new List<Movimiento>();
            ultimosMovimientosLegales = new List<Movimiento>();
            EvaluarSituacionActual();
        }

        public Partida(string fen, Jugador jugadorBlancas, Jugador jugadorNegras, Reloj reloj)
        {
            conTiempo = true;
            tablero = new Tablero(fen);
            this.jugadorBlancas = jugadorBlancas;
            this.jugadorNegras = jugadorNegras;
            this.reloj = reloj;
            fenInicioPartida = fen;
            movimientosRealizados = new List<Movimiento>();
            ultimosMovimientosLegales = new List<Movimiento>();
            EvaluarSituacionActual();
        }

        public Partida(Tablero tablero, Jugador jugadorBlancas, Jugador jugadorNegras, float duracion, float incremento)
        {
            conTiempo = true;
            this.tablero = tablero;
            this.jugadorBlancas = jugadorBlancas;
            this.jugadorNegras = jugadorNegras;
            reloj = new Reloj(duracion, incremento);
            fenInicioPartida = tablero.ToFEN();
            movimientosRealizados = new List<Movimiento>();
            ultimosMovimientosLegales = new List<Movimiento>();
            EvaluarSituacionActual();
        }

        public Partida(string fen, Jugador jugadorBlancas, Jugador jugadorNegras, float duracion, float incremento)
        {
            conTiempo = true;
            tablero = new Tablero(fen);
            this.jugadorBlancas = jugadorBlancas;
            this.jugadorNegras = jugadorNegras;
            reloj = new Reloj(duracion, incremento);
            fenInicioPartida = fen;
            movimientosRealizados = new List<Movimiento>();
            ultimosMovimientosLegales = new List<Movimiento>();
            EvaluarSituacionActual();
        }

        public Partida(Tablero tablero, Jugador jugadorBlancas, Jugador jugadorNegras)
        {
            conTiempo = false;
            this.tablero = tablero;
            this.jugadorBlancas = jugadorBlancas;
            this.jugadorNegras = jugadorNegras;
            fenInicioPartida = tablero.ToFEN();
            movimientosRealizados = new List<Movimiento>();
            ultimosMovimientosLegales = new List<Movimiento>();
            EvaluarSituacionActual();
        }

        public Partida(string fen, Jugador jugadorBlancas, Jugador jugadorNegras)
        {
            conTiempo = false;
            tablero = new Tablero(fen);
            this.jugadorBlancas = jugadorBlancas;
            this.jugadorNegras = jugadorNegras;
            fenInicioPartida = fen;
            movimientosRealizados = new List<Movimiento>();
            ultimosMovimientosLegales = new List<Movimiento>();
            EvaluarSituacionActual();
        }

        public Tablero Tablero
        {
            get
            {
                return this.tablero;
            }
            set
            {
                this.tablero = value;
                movimientosRealizados.Clear();
                if (tablero != null)
                {
                    fenInicioPartida = tablero.ToFEN();
                    EvaluarSituacionActual();
                }
            }
        }

        public Jugador JugadorBlancas
        {
            get
            {
                return this.jugadorBlancas;
            }
            set
            {
                this.jugadorBlancas = value;
            }
        }

        public Jugador JugadorNegras
        {
            get
            {
                return this.jugadorNegras;
            }
            set
            {
                this.jugadorNegras = value;
            }
        }

        public bool ConTiempo
        {
            get
            {
                return conTiempo;
            }
            set
            {
                conTiempo = value;
            }
        }

        public Reloj Reloj
        {
            get
            {
                if (!conTiempo)
                    throw new InvalidOperationException("Este método no puede usarse en una partida sin tiempo.");

                return reloj;
            }
            set
            {
                if (!conTiempo)
                    throw new InvalidOperationException("Este método no puede usarse en una partida sin tiempo.");

                reloj = value;

                if (jugadorBlancas is JugadorIA jugadorIABlancas)
                {
                    jugadorIABlancas.GestorTiempo = new GestorTiempo(value, Pieza.Color.Blancas);
                }

                if (jugadorNegras is JugadorIA jugadorIANegras)
                {
                    jugadorIANegras.GestorTiempo = new GestorTiempo(value, Pieza.Color.Negras);
                }
            }
        }

        public List<Movimiento> MovimientosRealizados
        {
            get
            {
                return movimientosRealizados;
            }
        }

        public String FenInicioPartida
        {
            get
            {
                return fenInicioPartida;
            }
        }

        public SituacionPartida.Tipo Situacion
        {
            get
            {
                return situacion;
            }
            set
            {
                situacion = value;
            }
        }

        public List<Movimiento> UltimosMovimientosLegales
        {
            get
            {
                return ultimosMovimientosLegales;
            }
        }

        public bool Jaque
        {
            get
            {
                return jaque;
            }
        }

        public void HacerMovimiento(Movimiento movimiento)
        {
            tablero.HacerMovimiento(movimiento);
            movimientosRealizados.Add(movimiento);
            // Hacer que se calcule la situacion de la partida, y mantener un seguimiento de la misma, así como de los últimosMovimientosLegales y de si hay jaque o no
            EvaluarSituacionActual();
        }

        private void EvaluarSituacionActual()
        {
            (ultimosMovimientosLegales, jaque) = tablero.GenerarMovimientosLegales();
            situacion = SituacionPartida.ObtenerSituacionPartida(tablero, ultimosMovimientosLegales.Count, jaque);
        }

        public string ToPGN(/*SituacionPartida.Tipo situacion*/)
        {
            //fenInicio = fenInicio.Replace("\n", "").Replace("\r", "");

            StringBuilder pgn = new();
            Tablero tableroInicial = new Tablero(fenInicioPartida);

            // Encabezados
            pgn.AppendLine($"[White \"{jugadorBlancas.Nombre}\"]");
            pgn.AppendLine($"[Black \"{jugadorNegras.Nombre}\"]");

            if (fenInicioPartida != Tablero.POSICION_INICIAL_FEN)
            {
                pgn.AppendLine($"[FEN \"{fenInicioPartida}\"]");
            }

            if (situacion is not SituacionPartida.Tipo.EnCurso)
            {
                pgn.AppendLine($"[Result \"{situacion}\"]");
            }

            for (int jugada = 0; jugada < movimientosRealizados.Count; jugada++)
            {
                string movimientoSAN = movimientosRealizados[jugada].ToSAN(tableroInicial);
                tableroInicial.HacerMovimiento(movimientosRealizados[jugada]);
                if (jugada % 2 == 0)
                {
                    pgn.Append((jugada / 2 + 1) + ". ");
                }
                pgn.Append(movimientoSAN + " ");
            }

            return pgn.ToString();
        }

        public void CargarTableroFEN(string fen)
        {
            tablero = new Tablero(fen);
            movimientosRealizados.Clear();
            fenInicioPartida = tablero.ToFEN();
            EvaluarSituacionActual();
        }

        public void IntercambiarColoresJugadores()
        {
            // Intercambiar los jugadores
            Jugador jugadorBlancasTemp = jugadorBlancas;
            jugadorBlancas = jugadorNegras;
            jugadorNegras = jugadorBlancasTemp;

            // Invertir colores
            jugadorBlancas.ColorPiezas = AjedrezUtils.InversoColor(jugadorBlancas.ColorPiezas);
            jugadorNegras.ColorPiezas = AjedrezUtils.InversoColor(jugadorNegras.ColorPiezas);
        }
    }
}