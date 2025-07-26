namespace Ajedrez.Core
{
    public readonly struct Pieza
    {
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

        public Pieza(char simbolo)
        {
            tipoPieza = ObtenerTipo(simbolo);
            colorPieza = ObtenerColor(simbolo);
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

        public char ObtenerSimbolo(bool siempreMayuscula = false)
        {
            char simbolo = tipoPieza switch
            {
                Tipo.Torre => 'R',
                Tipo.Caballo => 'N',
                Tipo.Alfil => 'B',
                Tipo.Reina => 'Q',
                Tipo.Rey => 'K',
                Tipo.Peon => 'P',
                _ => ' '
            };

            if (siempreMayuscula)
                return simbolo;

            return colorPieza == Color.Negras ? char.ToLower(simbolo) : simbolo;
        }

        public static Tipo ObtenerTipo(char simbolo)
        {
            simbolo = char.ToUpper(simbolo);

            return simbolo switch
            {
                'R' => Tipo.Torre,
                'N' => Tipo.Caballo,
                'B' => Tipo.Alfil,
                'Q' => Tipo.Reina,
                'K' => Tipo.Rey,
                'P' => Tipo.Peon,
                _ => Tipo.Nada
            };
        }
        
        public static Color ObtenerColor(char simbolo)
        {
            return char.IsUpper(simbolo) ? Color.Blancas : Color.Negras;
        }
    }
}
