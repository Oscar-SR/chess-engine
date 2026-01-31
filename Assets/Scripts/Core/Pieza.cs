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

        private readonly Type pieceType;
        private readonly Color pieceColor;

        public Piece(Type type, Color color)
        {
            pieceType = type;
            pieceColor = color;
        }

        public Piece(char symbol)
        {
            pieceType = GetType(symbol);
            pieceColor = GetColor(symbol);
        }

        public Type PieceType
        {
            get
            {
                return this.pieceType;
            }
        }

        public Color PieceColor
        {
            get
            {
                return this.pieceColor;
            }
        }

        public char GetSymbol(bool uppercase = false)
        {
            char symbol = pieceType switch
            {
                Type.Rook => 'R',
                Type.Knight => 'N',
                Type.Bishop => 'B',
                Type.Queen => 'Q',
                Type.King => 'K',
                Type.Pawn => 'P',
                _ => ' '
            };

            if (uppercase)
                return symbol;

            return pieceColor == Color.Black ? char.ToLower(symbol) : symbol;
        }

        public static Type GetType(char symbol)
        {
            symbol = char.ToUpper(symbol);

            return symbol switch
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
        
        public static Color GetColor(char symbol)
        {
            return char.IsUpper(symbol) ? Color.White : Color.Black;
        }
    }
}
