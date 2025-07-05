namespace Ajedrez.Core
{
    public readonly struct Pieza {
        public const int NUM_TIPOS_PIEZAS = 12;

        public enum Tipo : byte
        {
            Rey,
            Reina,
            Torre,
            Alfil,
            Caballo,
            Peon,
            Nada
        }

        public enum Color : byte
        {
            Blancas = 0,
            Negras = 6,
            Nada
        }

        private readonly Tipo tipoPieza;
        private readonly Color colorPieza;

        public Pieza(Tipo tipo, Color color)
        {
            tipoPieza = tipo;
            colorPieza = color;
        }

        public Tipo TipoPieza
        {
            get
            {
                return this.tipoPieza;
            }
        }

        public Color ColorPieza
        {
            get
            {
                return this.colorPieza;
            }
        }
    }
}
