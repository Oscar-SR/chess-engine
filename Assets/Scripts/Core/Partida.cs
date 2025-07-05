using System;

namespace Ajedrez.Core
{
    public enum TipoResultado : byte
    {
        SinResultado,
        VictoriaBlancas,
        VictoriaNegras,
        Tablas
    }

    public class Partida
    {
        private Tablero tablero;
        private Jugador jugador1;
        private Jugador jugador2;
        private float duracion;
        private float incremento;
        /*
        private TipoResultado resultado;
        private ulong duracion;
        private uint numeroMovimientosRealizados;
        private uint numeroJaques;
        private byte numeroTurnosInactivos;
        */

        public Partida(Tablero tablero, Jugador jugador1, Jugador jugador2, float duracion, float incremento)
        {
            this.tablero = tablero;
            this.jugador1 = jugador1;
            this.jugador2 = jugador2;
            this.duracion = duracion;
            this.incremento = incremento;
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
            }
        }

        public Jugador Jugador1
        {
            get
            {
                return this.jugador1;
            }
            set
            {
                this.jugador1 = value;
            }
        }

        public Jugador Jugador2
        {
            get
            {
                return this.jugador2;
            }
            set
            {
                this.jugador2 = value;
            }
        }

        public float Duracion
        {
            get
            {
                return duracion;
            }
            set
            {
                duracion = value;
            }
        }

        public float Incremento
        {
            get
            {
                return incremento;
            }
            set
            {
                incremento = value;
            }
        }
    }
}