using System;
using Ajedrez.Managers;
using Ajedrez.Utilities;

namespace Ajedrez.Core
{
    public abstract class Jugador
    {
        public enum Tipo
        {
            Humano,
            IA
        }

        protected string nombre;
        protected Pieza.Color colorPiezas;
        protected float tiempoRestante; // En segundos

        protected Jugador(string nombre, Pieza.Color colorPiezas, float tiempoRestante)
        {
            this.nombre = nombre;
            this.colorPiezas = colorPiezas;
            this.tiempoRestante = tiempoRestante;
        }
        
        public string Nombre
        {
            get { return nombre; }
        }

        public Pieza.Color ColorPiezas
        {
            get { return colorPiezas; }
        }

        public float TiempoRestante
        {
            get { return tiempoRestante; }
            set { tiempoRestante = Math.Max(0, value); }
        }
    }
}
