using System.Net.Sockets;
using Chess.Core;

namespace Chess.Debugging.Versus
{
    public class VersusPlayer : Player
    {
        private TcpClient client;
        private int winCount;
        private int lossCount;
        private int drawCount;
        private int moveCount;
        private int totalSearchDepth;

        public VersusPlayer(string name, Piece.Color pieceColor, TcpClient client)
            : base(name, pieceColor)
        {
            this.client = client;
        }

        public TcpClient Client
        {
            get => client;
            set => client = value;
        }

        public int WinCount
        {
            get => winCount;
            set => winCount = value;
        }

        public int LossCount
        {
            get => lossCount;
            set => lossCount = value;
        }

        public int DrawCount
        {
            get => drawCount;
            set => drawCount = value;
        }

        public int MoveCount
        {
            get => moveCount;
            set => moveCount = value;
        }

        public int TotalSearchDepth
        {
            get => totalSearchDepth;
            set => totalSearchDepth = value;
        }
    }
}