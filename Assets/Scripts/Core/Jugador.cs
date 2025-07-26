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

        protected Jugador(string nombre, Pieza.Color colorPiezas)
        {
            this.nombre = nombre;
            this.colorPiezas = colorPiezas;
        }

        public string Nombre
        {
            get { return nombre; }
            set { nombre = value; }
        }

        public virtual Pieza.Color ColorPiezas
        {
            get { return colorPiezas; }
            set { colorPiezas = value; }
        }
    }
}
