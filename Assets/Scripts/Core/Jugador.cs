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
        protected Piece.Color colorPiezas;

        protected Jugador(string nombre, Piece.Color colorPiezas)
        {
            this.nombre = nombre;
            this.colorPiezas = colorPiezas;
        }

        public string Nombre
        {
            get { return nombre; }
            set { nombre = value; }
        }

        public virtual Piece.Color ColorPiezas
        {
            get { return colorPiezas; }
            set { colorPiezas = value; }
        }
    }
}
