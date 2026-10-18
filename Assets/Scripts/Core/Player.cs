using System;
using Chess.Managers;
using Chess.Utilities;

namespace Chess.Core
{
    public abstract class Player
    {
        public enum Type
        {
            Human,
            AI
        }

        protected string name;
        protected Piece.Color pieceColor;

        protected Player(string name, Piece.Color pieceColor)
        {
            this.name = name;
            this.pieceColor = pieceColor;
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public virtual Piece.Color PieceColor
        {
            get { return pieceColor; }
            set { pieceColor = value; }
        }
    }
}