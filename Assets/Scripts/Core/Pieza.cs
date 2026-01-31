namespace Ajedrez.Core
{
    public readonly struct Piece
    {
        public const int NUM_PIECE_TYPES = 12;

        public enum Type : byte
        {
            King,
            Queen,
            Rook,
            Bishop,
            Knight,
            Pawn,
            None
        }

        public enum Color : byte
        {
            White = 0,
            Black = 6,
            None
        }

        private readonly Type tipoPieza;
        private readonly Color colorPieza;

        public Piece(Type tipo, Color color)
        {
            tipoPieza = tipo;
            colorPieza = color;
        }

        public Piece(char simbolo)
        {
            tipoPieza = ObtenerTipo(simbolo);
            colorPieza = ObtenerColor(simbolo);
        }

        public Type TipoPieza
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
                Type.Rook => 'R',
                Type.Knight => 'N',
                Type.Bishop => 'B',
                Type.Queen => 'Q',
                Type.King => 'K',
                Type.Pawn => 'P',
                _ => ' '
            };

            if (siempreMayuscula)
                return simbolo;

            return colorPieza == Color.Black ? char.ToLower(simbolo) : simbolo;
        }

        public static Type ObtenerTipo(char simbolo)
        {
            simbolo = char.ToUpper(simbolo);

            return simbolo switch
            {
                'R' => Type.Rook,
                'N' => Type.Knight,
                'B' => Type.Bishop,
                'Q' => Type.Queen,
                'K' => Type.King,
                'P' => Type.Pawn,
                _ => Type.None
            };
        }
        
        public static Color ObtenerColor(char simbolo)
        {
            return char.IsUpper(simbolo) ? Color.White : Color.Black;
        }
    }
}
