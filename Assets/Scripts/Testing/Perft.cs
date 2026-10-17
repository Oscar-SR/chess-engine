using System.Collections.Generic;
using System.Threading;
using Ajedrez.Core;

namespace Ajedrez.Testing
{
    public static class Perft
    {
        public static int Run(int depth, Board board, CancellationToken cancellationToken)
        {
            if (depth == 0)
                return 1;

            (List<Move> moves, _) = board.GenerateLegalMoves();
            int positionCount = 0;

            foreach (Move move in moves)
            {                
                board.MakeMove(move);
                positionCount += Run(depth - 1, board, cancellationToken);
                board.UndoMove(move);

                if (cancellationToken.IsCancellationRequested)
                    break;
            }

            return positionCount;
        }
    }
}