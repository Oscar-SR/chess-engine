using System;
using System.Collections.Generic;
using Ajedrez.Utilities;

namespace Ajedrez.Core
{
    public class Tablero
    {
        public const string POSICION_INICIAL_FEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        public const int MAX_PLYS_INACTIVO = 50;

        private ulong[] bitboards;
        private Stack<(Piece, int)> piezasMuertas;
        private BoardState estadoActual;
        private Stack<BoardState> historialEstados;
        private RepetitionStack historialPosicionesRepetidas; // Mantiene un historial de las últimas posiciones hasheadas, hasta el último movimiento irreversible
        private uint numMovimientosTotales;

        public Tablero()
        {
            CargarFEN(POSICION_INICIAL_FEN);
        }

        public Tablero(string fen)
        {
            CargarFEN(fen);
        }

        public string ToFEN(bool incluirPeonAlPaso = true)
        {
            string fen = "";

            // 1 Generar la parte del tablero (filas 8 a 1)
            for (int fila = 7; fila >= 0; fila--)
            {
                int casillasVacias = 0;

                for (int columna = 0; columna < AjedrezUtils.TAM_TABLERO; columna++)
                {
                    int indice = AjedrezUtils.CoordenadasAIndice(fila, columna);
                    Piece pieza = ObtenerPieza(indice);

                    if (pieza.PieceType == Piece.Type.None)
                    {
                        casillasVacias++;
                    }
                    else
                    {
                        if (casillasVacias > 0)
                        {
                            fen += casillasVacias.ToString();
                            casillasVacias = 0;
                        }

                        fen += pieza.GetSymbol();
                    }
                }

                if (casillasVacias > 0)
                {
                    fen += casillasVacias.ToString();
                }

                if (fila > 0)
                {
                    fen += "/";
                }
            }

            // 2 Turno
            fen += estadoActual.Turn == Piece.Color.White ? " w " : " b ";

            // 3 Enroques
            string enroques = "";
            if (estadoActual.IsCastlingAvailable(BoardState.WHITE_KINGSIDE_CASTLING)) enroques += "K";
            if (estadoActual.IsCastlingAvailable(BoardState.WHITE_QUEENSIDE_CASTLING)) enroques += "Q";
            if (estadoActual.IsCastlingAvailable(BoardState.BLACK_KINGSIDE_CASTLING)) enroques += "k";
            if (estadoActual.IsCastlingAvailable(BoardState.BLACK_QUEENSIDE_CASTLING)) enroques += "q";
            fen += string.IsNullOrEmpty(enroques) ? "-" : enroques;
            fen += " ";

            // 4 Peón al paso
            if (incluirPeonAlPaso && estadoActual.HasVulnerablePawn)
            {
                (int fila, int columna) = AjedrezUtils.IndiceACoordenadas(estadoActual.VulnerablePawnSquare);
                char colCaracter = AjedrezUtils.ColumnaANombre(columna);
                if (fila == 3)
                {
                    fila--;
                }
                else
                {
                    fila++;
                }
                char filaCaracter = AjedrezUtils.FilaANombre(fila);
                fen += $"{colCaracter}{filaCaracter}";
            }
            else
            {
                fen += "-";
            }

            // 5 Número de plys inactivo
            fen += $" {estadoActual.InactivePlyCount}";

            // 6 Número total de jugadas
            fen += $" {numMovimientosTotales}";

            return fen;
        }

        public void CargarFEN(string fen)
        {
            if (string.IsNullOrWhiteSpace(fen))
                throw new ArgumentException("La cadena FEN está vacía o es nula.", nameof(fen));

            string[] partes = fen.Trim().Split(' ');
            if (partes.Length != 6)
                throw new ArgumentException($"La cadena FEN debe tener 6 partes separadas por espacios. Tiene {partes.Length}.", nameof(fen));

            // Inicializar estructuras de datos
            bitboards = new ulong[12];
            piezasMuertas = new Stack<(Piece, int)>();
            historialEstados = new Stack<BoardState>(capacity: 64);
            historialPosicionesRepetidas = new RepetitionStack();

            string piezasFEN = partes[0];
            string turnoFEN = partes[1];
            string enroquesFEN = partes[2];
            string peonAlPasoFEN = partes[3];
            string plysFEN = partes[4];
            string movimientosFEN = partes[5];

            // 1. Piezas en el tablero
            string[] filas = piezasFEN.Split('/');
            if (filas.Length != 8)
                throw new ArgumentException("La sección de piezas debe contener 8 filas separadas por '/'.", nameof(fen));

            for (int fila = 0; fila < 8; fila++)
            {
                int columna = 0;
                foreach (char c in filas[fila])
                {
                    if (char.IsDigit(c))
                    {
                        columna += c - '0';
                    }
                    else if ("pnbrqkPNBRQK".IndexOf(c) >= 0)
                    {
                        Piece pieza = new Piece(c);
                        int indice = (7 - fila) * 8 + columna;

                        bitboards[AjedrezUtils.ObtenerIndicePieza(pieza)] |= BitboardUtils.SetBit(indice);
                        columna++;
                    }
                    else
                    {
                        throw new ArgumentException($"Caracter no válido en la disposición de piezas: '{c}' en fila {fila + 1}.", nameof(fen));
                    }
                }

                if (columna != 8)
                    throw new ArgumentException($"Fila {fila + 1} del tablero no tiene exactamente 8 columnas.", nameof(fen));
            }

            // 2. Color del turno
            if (turnoFEN != "w" && turnoFEN != "b")
                throw new ArgumentException($"Valor de turno inválido: '{turnoFEN}'. Debe ser 'w' o 'b'.", nameof(fen));

            estadoActual.Turn = turnoFEN == "w" ? Piece.Color.White : Piece.Color.Black;

            // 3. Enroques disponibles
            if (!System.Text.RegularExpressions.Regex.IsMatch(enroquesFEN, "^(K?Q?k?q?|\\-)$"))
                throw new ArgumentException($"Formato de enroques inválido: '{enroquesFEN}'.", nameof(fen));

            if (enroquesFEN.Contains("K")) estadoActual.InitializeCastlingRights(BoardState.WHITE_KINGSIDE_CASTLING);
            if (enroquesFEN.Contains("Q")) estadoActual.InitializeCastlingRights(BoardState.WHITE_QUEENSIDE_CASTLING);
            if (enroquesFEN.Contains("k")) estadoActual.InitializeCastlingRights(BoardState.BLACK_KINGSIDE_CASTLING);
            if (enroquesFEN.Contains("q")) estadoActual.InitializeCastlingRights(BoardState.BLACK_QUEENSIDE_CASTLING);

            // 4. Peón al paso
            if (peonAlPasoFEN != "-")
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(peonAlPasoFEN, "^[a-h][36]$"))
                    throw new ArgumentException($"Valor de peón al paso inválido: '{peonAlPasoFEN}'.", nameof(fen));

                int col = AjedrezUtils.NombreAColumna(peonAlPasoFEN[0]);
                int fila = AjedrezUtils.NombreAFila(peonAlPasoFEN[1]);
                //estadoActual.VulnerablePawnSquare = AjedrezUtils.CoordenadasAIndice(fila, col);
                if (fila == 2)
                    estadoActual.VulnerablePawnSquare = (24 + col);
                else
                    estadoActual.VulnerablePawnSquare = (32 + col);
            }
            else
            {
                estadoActual.HasVulnerablePawn = false;
            }

            // 5. Número de plys inactivo
            if (!byte.TryParse(plysFEN, out byte plys))
                throw new ArgumentException($"Número de medio movimientos inválido: '{plysFEN}'.", nameof(fen));

            estadoActual.InactivePlyCount = plys;

            // 6. Número de movimientos totales
            if (!uint.TryParse(movimientosFEN, out uint movimientos))
                throw new ArgumentException($"Número de movimientos inválido: '{movimientosFEN}'.", nameof(fen));

            numMovimientosTotales = movimientos;

            // 7. Zobrist hash y repetición
            estadoActual.ZobristHash = ZobristHashing.CrearZobristHash(this);
            historialPosicionesRepetidas.Push(estadoActual.ZobristHash, true);
        }

        public BoardState EstadoActual
        {
            get
            {
                return estadoActual;
            }
        }

        public RepetitionStack HistorialPosicionesRepetidas
        {
            get
            {
                return historialPosicionesRepetidas;
            }
        }

        public Piece.Color Turno
        {
            get
            {
                return estadoActual.Turn;
            }
        }

        private int PeonVulnerable
        {
            get
            {
                return estadoActual.VulnerablePawnSquare;
            }
        }

        public ulong[] Bitboards
        {
            get
            {
                return bitboards;
            }
        }

        public ulong BitboardReyBlanco
        {
            get
            {
                return bitboards[0];
            }
        }

        public ulong BitboardReinasBlancas
        {
            get
            {
                return bitboards[1];
            }
        }

        public ulong BitboardTorresBlancas
        {
            get
            {
                return bitboards[2];
            }
        }

        public ulong BitboardAlfilesBlancos
        {
            get
            {
                return bitboards[3];
            }
        }

        public ulong BitboardCaballosBlancos
        {
            get
            {
                return bitboards[4];
            }
        }

        public ulong BitboardPeonesBlancos
        {
            get
            {
                return bitboards[5];
            }
        }

        public ulong BitboardReyNegro
        {
            get
            {
                return bitboards[6];
            }
        }

        public ulong BitboardReinasNegras
        {
            get
            {
                return bitboards[7];
            }
        }

        public ulong BitboardTorresNegras
        {
            get
            {
                return bitboards[8];
            }
        }

        public ulong BitboardAlfilesNegros
        {
            get
            {
                return bitboards[9];
            }
        }

        public ulong BitboardCaballosNegros
        {
            get
            {
                return bitboards[10];
            }
        }

        public ulong BitboardPeonesNegros
        {
            get
            {
                return bitboards[11];
            }
        }

        public uint NumMovimientosTotales
        {
            get
            {
                return numMovimientosTotales;
            }
        }

        private void CambiarTurno()
        {
            estadoActual.Turn = AjedrezUtils.InversoColor(estadoActual.Turn);
            estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashTurno(estadoActual.ZobristHash);

            // Anular el peón vulnerable si es que lo hay
            if (estadoActual.HasVulnerablePawn)
            {
                if (AjedrezUtils.MismoColor(ObtenerPieza(PeonVulnerable).PieceColor, Turno))
                {
                    estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashColumnasAlPaso(estadoActual.ZobristHash, PeonVulnerable); // Cuidado con mover esta línea hacia abajo, PeonVulnerable ya no devolverá la casilla
                    estadoActual.HasVulnerablePawn = false;
                }
            }
        }

        public void HacerMovimiento(Move movimiento, bool enBusqueda = false)
        {
            // Añadimos el estado anterior a la pila
            historialEstados.Push(estadoActual);

            // Anulamos la captura
            estadoActual.HasCapture = false;

            // Obtenemos el índice de la pieza que se quiere mover
            Piece piezaEnOrigen = ObtenerPieza(movimiento.From);
            Piece piezaCapturada = ObtenerPieza(movimiento.To);

            // Actualizar Zobrist hash movimiento
            estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashCasilla(estadoActual.ZobristHash, piezaEnOrigen, movimiento.From);
            if (!movimiento.IsPromotion())
                estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashCasilla(estadoActual.ZobristHash, piezaEnOrigen, movimiento.To);

            // Cambiamos la casilla de la pieza
            ActualizarBitboardsMovimiento(piezaEnOrigen, movimiento.From, movimiento.To);

            if (piezaCapturada.PieceType != Piece.Type.None)
            {
                if (estadoActual.HasVulnerablePawn && (movimiento.To == PeonVulnerable))
                {
                    // La pieza capturada era el peon vulnerable, se anula
                    estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashColumnasAlPaso(estadoActual.ZobristHash, PeonVulnerable); // Cuidado con mover esta línea hacia abajo, PeonVulnerable ya no devolverá la casilla
                    estadoActual.HasVulnerablePawn = false;
                }
                else if (piezaCapturada.PieceType == Piece.Type.Rook)
                {
                    // Cancelar los enroques correspondientes de cada torre
                    switch (movimiento.To)
                    {
                        case 0:
                            {
                                // Actualizar los derechos disponibles del estado
                                estadoActual.CancelCastling(BoardState.WHITE_QUEENSIDE_CASTLING);
                                break;
                            }

                        case 7:
                            {
                                // Actualizar los derechos disponibles del estado
                                estadoActual.CancelCastling(BoardState.WHITE_KINGSIDE_CASTLING);
                                break;
                            }

                        case 56:
                            {
                                // Actualizar los derechos disponibles del estado
                                estadoActual.CancelCastling(BoardState.BLACK_QUEENSIDE_CASTLING);
                                break;
                            }

                        case 63:
                            {
                                // Actualizar los derechos disponibles del estado
                                estadoActual.CancelCastling(BoardState.BLACK_KINGSIDE_CASTLING);
                                break;
                            }
                    }

                    // Actualizar Zobrist hash enroques disponibles
                    estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashEnroquesDisponibles(estadoActual.ZobristHash, historialEstados.Peek().CastlingRights); // Eliminar los enroques disponibles antiguos
                    estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashEnroquesDisponibles(estadoActual.ZobristHash, estadoActual.CastlingRights); // Añadir los enroques disponibles nuevos
                }

                // Hay captura
                EliminarPieza(piezaCapturada, movimiento.To);
            }

            // Manejamos los flag
            switch (movimiento.Flag)
            {
                case Move.EN_PASSANT_CAPTURE:
                    {
                        EliminarPieza(new Piece(Piece.Type.Pawn, AjedrezUtils.InversoColor(Turno)), PeonVulnerable);
                        estadoActual.HasVulnerablePawn = false; ;
                        break;
                    }

                case Move.CASTLE:
                    {
                        Piece torre = new Piece(Piece.Type.Rook, Turno);

                        switch (movimiento.To)
                        {
                            case AjedrezUtils.ENROQUE_LARGO_BLANCAS:
                                {
                                    // Enroque largo blancas
                                    ActualizarBitboardsMovimiento(torre, 0, 3);
                                    estadoActual.CancelCastling(BoardState.WHITE_KINGSIDE_CASTLING | BoardState.WHITE_QUEENSIDE_CASTLING);

                                    // Actualizar Zobrist hash movimiento
                                    estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashCasilla(estadoActual.ZobristHash, torre, 0);
                                    estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashCasilla(estadoActual.ZobristHash, torre, 3);
                                    break;
                                }

                            case AjedrezUtils.ENROQUE_CORTO_BLANCAS:
                                {
                                    // Enroque corto blancas
                                    ActualizarBitboardsMovimiento(torre, 7, 5);
                                    estadoActual.CancelCastling(BoardState.WHITE_KINGSIDE_CASTLING | BoardState.WHITE_QUEENSIDE_CASTLING);

                                    // Actualizar Zobrist hash movimiento
                                    estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashCasilla(estadoActual.ZobristHash, torre, 7);
                                    estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashCasilla(estadoActual.ZobristHash, torre, 5);
                                    break;
                                }

                            case AjedrezUtils.ENROQUE_LARGO_NEGRAS:
                                {
                                    // Enroque largo negras
                                    ActualizarBitboardsMovimiento(torre, 56, 59);
                                    estadoActual.CancelCastling(BoardState.BLACK_KINGSIDE_CASTLING | BoardState.BLACK_QUEENSIDE_CASTLING);

                                    // Actualizar Zobrist hash movimiento
                                    estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashCasilla(estadoActual.ZobristHash, torre, 56);
                                    estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashCasilla(estadoActual.ZobristHash, torre, 59);
                                    break;
                                }

                            case AjedrezUtils.ENROQUE_CORTO_NEGRAS:
                                {
                                    // Enroque corto negras
                                    ActualizarBitboardsMovimiento(torre, 63, 61);
                                    estadoActual.CancelCastling(BoardState.BLACK_KINGSIDE_CASTLING | BoardState.BLACK_QUEENSIDE_CASTLING);

                                    // Actualizar Zobrist hash movimiento
                                    estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashCasilla(estadoActual.ZobristHash, torre, 63);
                                    estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashCasilla(estadoActual.ZobristHash, torre, 61);
                                    break;
                                }
                        }

                        // Actualizar Zobrist hash enroques disponibles
                        estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashEnroquesDisponibles(estadoActual.ZobristHash, historialEstados.Peek().CastlingRights); // Eliminar los enroques disponibles antiguos
                        estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashEnroquesDisponibles(estadoActual.ZobristHash, estadoActual.CastlingRights); // Añadir los enroques disponibles nuevos

                        break;
                    }

                case Move.PAWN_TWO_UP:
                    {
                        estadoActual.VulnerablePawnSquare = movimiento.To;
                        estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashColumnasAlPaso(estadoActual.ZobristHash, estadoActual.VulnerablePawnSquare);
                        break;
                    }

                case Move.PROMOTE_TO_QUEEN:
                    {
                        ActualizarBitboardsPromocion(Piece.Type.Queen, Turno, movimiento.To);
                        estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashCasilla(estadoActual.ZobristHash, new Piece(Piece.Type.Queen, Turno), movimiento.To);
                        break;
                    }

                case Move.PROMOTE_TO_KNIGHT:
                    {
                        ActualizarBitboardsPromocion(Piece.Type.Knight, Turno, movimiento.To);
                        estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashCasilla(estadoActual.ZobristHash, new Piece(Piece.Type.Knight, Turno), movimiento.To);
                        break;
                    }

                case Move.PROMOTE_TO_ROOK:
                    {
                        ActualizarBitboardsPromocion(Piece.Type.Rook, Turno, movimiento.To);
                        estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashCasilla(estadoActual.ZobristHash, new Piece(Piece.Type.Rook, Turno), movimiento.To);
                        break;
                    }

                case Move.PROMOTE_TO_BISHOP:
                    {
                        ActualizarBitboardsPromocion(Piece.Type.Bishop, Turno, movimiento.To);
                        estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashCasilla(estadoActual.ZobristHash, new Piece(Piece.Type.Bishop, Turno), movimiento.To);
                        break;
                    }
            }

            if (piezaEnOrigen.PieceType == Piece.Type.King /*&& movimiento.Flag != Move.CASTLE*/)
            {
                if (AjedrezUtils.MismoColor(piezaEnOrigen.PieceColor, Piece.Color.White))
                {
                    estadoActual.CancelCastling(BoardState.WHITE_KINGSIDE_CASTLING | BoardState.WHITE_QUEENSIDE_CASTLING);
                }
                else
                {
                    estadoActual.CancelCastling(BoardState.BLACK_KINGSIDE_CASTLING | BoardState.BLACK_QUEENSIDE_CASTLING);
                }

                // Actualizar Zobrist hash enroques disponibles
                estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashEnroquesDisponibles(estadoActual.ZobristHash, historialEstados.Peek().CastlingRights); // Eliminar los enroques disponibles antiguos
                estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashEnroquesDisponibles(estadoActual.ZobristHash, estadoActual.CastlingRights); // Añadir los enroques disponibles nuevos
            }
            else if (piezaEnOrigen.PieceType == Piece.Type.Rook)
            {
                if (AjedrezUtils.MismoColor(piezaEnOrigen.PieceColor, Piece.Color.White))
                {
                    if (movimiento.From == 0)
                        estadoActual.CancelCastling(BoardState.WHITE_QUEENSIDE_CASTLING);
                    else if (movimiento.From == 7)
                        estadoActual.CancelCastling(BoardState.WHITE_KINGSIDE_CASTLING);
                }
                else
                {
                    if (movimiento.From == 56)
                        estadoActual.CancelCastling(BoardState.BLACK_QUEENSIDE_CASTLING);
                    else if (movimiento.From == 63)
                        estadoActual.CancelCastling(BoardState.BLACK_KINGSIDE_CASTLING);
                }

                // Actualizar Zobrist hash enroques disponibles
                estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashEnroquesDisponibles(estadoActual.ZobristHash, historialEstados.Peek().CastlingRights); // Eliminar los enroques disponibles antiguos
                estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashEnroquesDisponibles(estadoActual.ZobristHash, estadoActual.CastlingRights); // Añadir los enroques disponibles nuevos
            }

            // Incrementar movimientos totales
            if (Turno == Piece.Color.Black)
            {
                numMovimientosTotales++;
            }

            // Cambiamos el turno
            CambiarTurno(); // Cuidado con mover esto más abajo, afectará al Zobrist hash

            if (!estadoActual.HasCapture && piezaEnOrigen.PieceType != Piece.Type.Pawn)
            {
                // Movimiento no irreversible
                // Incrementar el contador de la regla de los 50 movimientos
                estadoActual.InactivePlyCount++;
                // Añadir posición a la pila de repeticiones
                historialPosicionesRepetidas.Push(estadoActual.ZobristHash, false);
            }
            else
            {
                // Movimiento irreversible
                estadoActual.InactivePlyCount = 0;

                if (enBusqueda)
                {
                    // Si estamos en búsqueda, no borramos el contenido de la pila, sino que marcamos el último segmento válido
                    historialPosicionesRepetidas.Push(estadoActual.ZobristHash, true);
                }
                else
                {
                    // Si no estamos en búsqueda, borramos el contenido de la pila
                    historialPosicionesRepetidas.Clear();
                }
            }
        }

        public void DeshacerMovimiento(Move movimiento)
        {
            // Obtenemos el índice de la pieza que quiere deshacer su movimiento
            Piece pieza = ObtenerPieza(movimiento.To);

            // Cambiamos la casilla de la pieza
            ActualizarBitboardsMovimiento(pieza, movimiento.To, movimiento.From);

            // Manejamos los flag
            switch (movimiento.Flag)
            {
                case Move.CASTLE:
                    {
                        switch (movimiento.To)
                        {
                            case AjedrezUtils.ENROQUE_LARGO_BLANCAS:
                                {
                                    // Enroque largo blancas
                                    ActualizarBitboardsMovimiento(Piece.Type.Rook, pieza.PieceColor, 3, 0);
                                    break;
                                }

                            case AjedrezUtils.ENROQUE_CORTO_BLANCAS:
                                {
                                    // Enroque corto blancas
                                    ActualizarBitboardsMovimiento(Piece.Type.Rook, pieza.PieceColor, 5, 7);
                                    break;
                                }

                            case AjedrezUtils.ENROQUE_LARGO_NEGRAS:
                                {
                                    // Enroque largo negras
                                    ActualizarBitboardsMovimiento(Piece.Type.Rook, pieza.PieceColor, 59, 56);
                                    break;
                                }

                            case AjedrezUtils.ENROQUE_CORTO_NEGRAS:
                                {
                                    // Enroque corto negras
                                    ActualizarBitboardsMovimiento(Piece.Type.Rook, pieza.PieceColor, 61, 63);
                                    break;
                                }
                        }

                        break;
                    }

                case Move.PROMOTE_TO_QUEEN:
                    {
                        DesactualizarBitboardsPromocion(Piece.Type.Queen, pieza.PieceColor, movimiento.From);
                        break;
                    }

                case Move.PROMOTE_TO_KNIGHT:
                    {
                        DesactualizarBitboardsPromocion(Piece.Type.Knight, pieza.PieceColor, movimiento.From);
                        break;
                    }

                case Move.PROMOTE_TO_ROOK:
                    {
                        DesactualizarBitboardsPromocion(Piece.Type.Rook, pieza.PieceColor, movimiento.From);
                        break;
                    }

                case Move.PROMOTE_TO_BISHOP:
                    {
                        DesactualizarBitboardsPromocion(Piece.Type.Bishop, pieza.PieceColor, movimiento.From);
                        break;
                    }
            }

            if (estadoActual.HasCapture)
            {
                (Piece piezaCapturada, int casilla) = piezasMuertas.Pop();
                bitboards[AjedrezUtils.ObtenerIndicePieza(piezaCapturada.PieceType, piezaCapturada.PieceColor)] |= BitboardUtils.SetBit(casilla);
            }

            // Decrementar movimientos totales
            if (Turno == Piece.Color.White)
            {
                numMovimientosTotales--;
            }

            // Restauramos el último estado
            estadoActual = historialEstados.Pop();

            // Eliminamos la última entrada del historial de posiciones repetidas, si tiene alguna
            historialPosicionesRepetidas.Pop();
        }

        public (List<Move> movimientosLegales, bool jaque) GenerarMovimientosLegales
        (
            bool acortarGeneracion = false,  // Evitar seguir generando movimientos cuando se llega a una situación de tablas
            bool soloGenerarCapturas = false // Generar solo movimientos de captura
        )
        {
            List<Move> movimientosLegales = new List<Move>();
            int casillaRey = ObtenerCasillaRey(Turno);

            if (casillaRey == -1)
            {
                // No hay rey
                return (movimientosLegales, false);
            }

            if (acortarGeneracion)
            {
                if (estadoActual.InactivePlyCount >= MAX_PLYS_INACTIVO)
                {
                    // Se cumple la regla de los 50 movimientos
                    return (movimientosLegales, false);
                }

                if (SituacionPartida.MaterialInsuficiente(this))
                {
                    // El material es insuficiente
                    return (movimientosLegales, false);
                }

                if (historialPosicionesRepetidas.IsThreefoldRepetition(estadoActual.ZobristHash))
                {
                    // El último movimiento realizado supuso una triple repetición
                    return (movimientosLegales, false);
                }
            }

            ulong casillasOcupadas = ObtenerOcupadas();
            ulong atacantesRey = CasillaAtacadaPor(casillasOcupadas, casillaRey, AjedrezUtils.InversoColor(Turno));
            (ulong piezasClavadas, ulong pinners) = ObtenerPiezasClavadas(casillasOcupadas, casillaRey, Turno);
            bool jaque = atacantesRey == 0 ? false : true;
            ulong piezas = AjedrezUtils.MismoColor(Turno, Piece.Color.White) ? ObtenerOcupadasBlancas() : ObtenerOcupadasNegras();

            while (piezas != 0)
            {
                int casilla = BitboardUtils.PrimerBitActivo(piezas);
                piezas &= piezas - 1; // Borra el bit más bajo activo

                switch (ObtenerPieza(casilla).PieceType)
                {
                    case Piece.Type.King:
                        {
                            GenerarMovimientosRey(movimientosLegales, casilla, casillasOcupadas, ObtenerBitboardMovimientosValidosRey(jaque, casillasOcupadas), soloGenerarCapturas);
                            break;
                        }

                    case Piece.Type.Queen:
                        {
                            GenerarMovimientosReina(movimientosLegales, casilla, casillasOcupadas, ObtenerBitboardMovimientosValidos(casilla, jaque, atacantesRey, piezasClavadas, pinners, casillaRey), soloGenerarCapturas);
                            break;
                        }

                    case Piece.Type.Rook:
                        {
                            GenerarMovimientosTorre(movimientosLegales, casilla, casillasOcupadas, ObtenerBitboardMovimientosValidos(casilla, jaque, atacantesRey, piezasClavadas, pinners, casillaRey), soloGenerarCapturas);
                            break;
                        }

                    case Piece.Type.Bishop:
                        {
                            GenerarMovimientosAlfil(movimientosLegales, casilla, casillasOcupadas, ObtenerBitboardMovimientosValidos(casilla, jaque, atacantesRey, piezasClavadas, pinners, casillaRey), soloGenerarCapturas);
                            break;
                        }

                    case Piece.Type.Knight:
                        {
                            GenerarMovimientosCaballo(movimientosLegales, casilla, ObtenerBitboardMovimientosValidos(casilla, jaque, atacantesRey, piezasClavadas, pinners, casillaRey), soloGenerarCapturas);
                            break;
                        }

                    case Piece.Type.Pawn:
                        {
                            GenerarMovimientosPeon(movimientosLegales, casilla, casillaRey, casillasOcupadas, ObtenerBitboardMovimientosValidos(casilla, jaque, atacantesRey, piezasClavadas, pinners, casillaRey), soloGenerarCapturas);
                            break;
                        }
                }
            }

            return (movimientosLegales, jaque);
        }

        private void GenerarMovimientosRey(List<Move> movimientosLegales, int casilla, ulong casillasOcupadas, ulong movimientosValidos, bool soloGenerarCapturas)
        {
            for (int direccion = 0; direccion < 8; direccion++)
            {
                if (AjedrezUtils.NumCasillasHastaBorde[casilla][direccion] > 0)
                {
                    int casillaDestino = casilla + AjedrezUtils.Direcciones[direccion];
                    Piece piezaEnDestino = ObtenerPieza(casillaDestino);

                    // Si hay una pieza del mismo color, no es un movimiento válido
                    if (piezaEnDestino.PieceType != Piece.Type.None && AjedrezUtils.MismoColor(piezaEnDestino.PieceColor, Turno))
                    {
                        continue;
                    }

                    // Si solo se generan capturas y no hay una pieza en el destino, buscar en el siguente
                    if (soloGenerarCapturas && piezaEnDestino.PieceType == Piece.Type.None)
                    {
                        continue;
                    }

                    // Si no es una casilla atacada y el movimiento es válido
                    if (!CasillaAtacada(casillasOcupadas, casillaDestino, AjedrezUtils.InversoColor(Turno)) && BitboardUtils.EstaCasillaActiva(casillaDestino, movimientosValidos))
                    {
                        movimientosLegales.Add(new Move(casilla, casillaDestino, Move.NO_FLAG));
                    }
                }
            }

            if (!soloGenerarCapturas)
            {
                if (Turno == Piece.Color.White)
                {
                    // Enroque largo blancas
                    if (estadoActual.IsCastlingAvailable(BoardState.WHITE_QUEENSIDE_CASTLING) && !CasillaAtacada(casillasOcupadas, casilla, AjedrezUtils.InversoColor(Turno)) && CasillaVacia(casillasOcupadas, 1) && CasillaVacia(casillasOcupadas, 2) && !CasillaAtacada(casillasOcupadas, 2, AjedrezUtils.InversoColor(Turno)) && CasillaVacia(casillasOcupadas, 3) && !CasillaAtacada(casillasOcupadas, 3, AjedrezUtils.InversoColor(Turno)))
                    {
                        movimientosLegales.Add(new Move(casilla, 2, Move.CASTLE));
                    }

                    // Enroque corto blancas
                    if (estadoActual.IsCastlingAvailable(BoardState.WHITE_KINGSIDE_CASTLING) && !CasillaAtacada(casillasOcupadas, casilla, AjedrezUtils.InversoColor(Turno)) && CasillaVacia(casillasOcupadas, 5) && !CasillaAtacada(casillasOcupadas, 5, AjedrezUtils.InversoColor(Turno)) && CasillaVacia(casillasOcupadas, 6) && !CasillaAtacada(casillasOcupadas, 6, AjedrezUtils.InversoColor(Turno)))
                    {
                        movimientosLegales.Add(new Move(casilla, 6, Move.CASTLE));
                    }
                }
                else
                {
                    // Enroque largo negras
                    if (estadoActual.IsCastlingAvailable(BoardState.BLACK_QUEENSIDE_CASTLING) && !CasillaAtacada(casillasOcupadas, casilla, AjedrezUtils.InversoColor(Turno)) && CasillaVacia(casillasOcupadas, 57) && CasillaVacia(casillasOcupadas, 58) && !CasillaAtacada(casillasOcupadas, 58, AjedrezUtils.InversoColor(Turno)) && CasillaVacia(casillasOcupadas, 59) && !CasillaAtacada(casillasOcupadas, 59, AjedrezUtils.InversoColor(Turno)))
                    {
                        movimientosLegales.Add(new Move(casilla, 58, Move.CASTLE));
                    }

                    // Enroque corto negras
                    if (estadoActual.IsCastlingAvailable(BoardState.BLACK_KINGSIDE_CASTLING) && !CasillaAtacada(casillasOcupadas, casilla, AjedrezUtils.InversoColor(Turno)) && CasillaVacia(casillasOcupadas, 61) && !CasillaAtacada(casillasOcupadas, 61, AjedrezUtils.InversoColor(Turno)) && CasillaVacia(casillasOcupadas, 62) && !CasillaAtacada(casillasOcupadas, 62, AjedrezUtils.InversoColor(Turno)))
                    {
                        movimientosLegales.Add(new Move(casilla, 62, Move.CASTLE));
                    }
                }
            }
        }

        private void GenerarMovimientosReina(List<Move> movimientosLegales, int casilla, ulong ocupadas, ulong movimientosValidos, bool soloGenerarCapturas)
        {
            ulong ataques = BitboardUtils.AtaquesTorre(ocupadas, casilla) | BitboardUtils.AtaquesAlfil(ocupadas, casilla);

            // Intersección entre ataques y movimientos válidos dados (por ejemplo, por pin o jaque)
            ulong movimientos = ataques & movimientosValidos;

            // Recorremos los bits activos (movimientos posibles)
            while (movimientos != 0)
            {
                int destino = BitboardUtils.PrimerBitActivo(movimientos);
                movimientos &= movimientos - 1; // Borra el bit más bajo activo

                Piece piezaEnDestino = ObtenerPieza(destino);

                // Si no hay pieza del mismo color y el flag de solo generar capturas está activo, solo agregar si hay una pieza del color contrario
                if (piezaEnDestino.PieceType == Piece.Type.None || !AjedrezUtils.MismoColor(piezaEnDestino.PieceColor, Turno))
                {
                    if (!soloGenerarCapturas || (piezaEnDestino.PieceType != Piece.Type.None))
                    {
                        movimientosLegales.Add(new Move(casilla, destino, Move.NO_FLAG));
                    }
                }
            }
        }

        private void GenerarMovimientosTorre(List<Move> movimientosLegales, int casilla, ulong ocupadas, ulong movimientosValidos, bool soloGenerarCapturas)
        {
            ulong ataques = BitboardUtils.AtaquesTorre(ocupadas, casilla);

            // Intersección entre ataques y movimientos válidos dados (por ejemplo, por pin o jaque)
            ulong movimientos = ataques & movimientosValidos;

            // Recorremos los bits activos (movimientos posibles)
            while (movimientos != 0)
            {
                int destino = BitboardUtils.PrimerBitActivo(movimientos);
                movimientos &= movimientos - 1; // Borra el bit más bajo activo

                Piece piezaEnDestino = ObtenerPieza(destino);

                // Si no hay pieza del mismo color y el flag de solo generar capturas está activo, solo agregar si hay una pieza del color contrario
                if (piezaEnDestino.PieceType == Piece.Type.None || !AjedrezUtils.MismoColor(piezaEnDestino.PieceColor, Turno))
                {
                    if (!soloGenerarCapturas || (piezaEnDestino.PieceType != Piece.Type.None))
                    {
                        movimientosLegales.Add(new Move(casilla, destino, Move.NO_FLAG));
                    }
                }
            }
        }

        private void GenerarMovimientosAlfil(List<Move> movimientosLegales, int casilla, ulong ocupadas, ulong movimientosValidos, bool soloGenerarCapturas)
        {
            ulong ataques = BitboardUtils.AtaquesAlfil(ocupadas, casilla);

            // Intersección entre ataques y movimientos válidos dados (por ejemplo, por pin o jaque)
            ulong movimientos = ataques & movimientosValidos;

            // Recorremos los bits activos (movimientos posibles)
            while (movimientos != 0)
            {
                int destino = BitboardUtils.PrimerBitActivo(movimientos);
                movimientos &= movimientos - 1; // Borra el bit más bajo activo

                Piece piezaEnDestino = ObtenerPieza(destino);

                // Si no hay pieza del mismo color y el flag de solo generar capturas está activo, solo agregar si hay una pieza del color contrario
                if (piezaEnDestino.PieceType == Piece.Type.None || !AjedrezUtils.MismoColor(piezaEnDestino.PieceColor, Turno))
                {
                    if (!soloGenerarCapturas || (piezaEnDestino.PieceType != Piece.Type.None))
                    {
                        movimientosLegales.Add(new Move(casilla, destino, Move.NO_FLAG));
                    }
                }
            }
        }

        private void GenerarMovimientosCaballo(List<Move> movimientosLegales, int casilla, ulong movimientosValidos, bool soloGenerarCapturas)
        {
            foreach (int salto in AjedrezUtils.SaltosCaballo)
            {
                int casillaDestino = casilla + salto;

                // Validar que no se salga del tablero y que el salto sea válido
                if (casillaDestino < 0 || casillaDestino >= 64 || !AjedrezUtils.SaltoValido(casilla, casillaDestino))
                {
                    continue;
                }

                // Verificar si hay una pieza en el destino
                Piece piezaEnDestino = ObtenerPieza(casillaDestino);

                // Si hay una pieza del mismo color, no se puede mover ahí
                if (piezaEnDestino.PieceType != Piece.Type.None && AjedrezUtils.MismoColor(piezaEnDestino.PieceColor, Turno))
                {
                    continue;
                }

                // Si solo se generan capturas y no hay una pieza en el destino, buscar en el siguente
                if (soloGenerarCapturas && piezaEnDestino.PieceType == Piece.Type.None)
                {
                    continue;
                }

                // Es un movimiento válido del caballo
                if (BitboardUtils.EstaCasillaActiva(casillaDestino, movimientosValidos))
                {
                    movimientosLegales.Add(new Move(casilla, casillaDestino, Move.NO_FLAG));
                }
            }
        }

        private void GenerarMovimientosPeon(List<Move> movimientosLegales, int casilla, int casillaRey, ulong casillasOcupadas, ulong movimientosValidos, bool soloGenerarCapturas)
        {
            int avance = AjedrezUtils.MismoColor(Turno, Piece.Color.White) ? 8 : -8;
            int casillaAvance = casilla + avance;
            Piece piezaEnDestino = ObtenerPieza(casillaAvance);

            // Puede promocionar
            if ((AjedrezUtils.MismoColor(Turno, Piece.Color.White) && (AjedrezUtils.ObtenerFila(casilla) == 6)) || (AjedrezUtils.MismoColor(Turno, Piece.Color.Black) && (AjedrezUtils.ObtenerFila(casilla) == 1)))
            {
                // No lo bloquea una pieza
                if (piezaEnDestino.PieceType == Piece.Type.None && BitboardUtils.EstaCasillaActiva(casillaAvance, movimientosValidos))
                {
                    if (!soloGenerarCapturas)
                    {
                        movimientosLegales.Add(new Move(casilla, casillaAvance, Move.PROMOTE_TO_QUEEN));
                        movimientosLegales.Add(new Move(casilla, casillaAvance, Move.PROMOTE_TO_KNIGHT));
                        movimientosLegales.Add(new Move(casilla, casillaAvance, Move.PROMOTE_TO_ROOK));
                        movimientosLegales.Add(new Move(casilla, casillaAvance, Move.PROMOTE_TO_BISHOP));
                    }
                }

                // Puede desplazarse hacia noroeste
                if ((AjedrezUtils.NumCasillasHastaBorde[casilla][AjedrezUtils.NOROESTE] > 0) && BitboardUtils.EstaCasillaActiva(casillaAvance - 1, movimientosValidos))
                {
                    piezaEnDestino = ObtenerPieza(casillaAvance - 1);

                    // Hay captura al oeste
                    if (piezaEnDestino.PieceType != Piece.Type.None && !AjedrezUtils.MismoColor(Turno, piezaEnDestino.PieceColor))
                    {
                        movimientosLegales.Add(new Move(casilla, casillaAvance - 1, Move.PROMOTE_TO_QUEEN));
                        movimientosLegales.Add(new Move(casilla, casillaAvance - 1, Move.PROMOTE_TO_KNIGHT));
                        movimientosLegales.Add(new Move(casilla, casillaAvance - 1, Move.PROMOTE_TO_ROOK));
                        movimientosLegales.Add(new Move(casilla, casillaAvance - 1, Move.PROMOTE_TO_BISHOP));
                    }
                }

                // Puede desplazarse hacia nordeste
                if ((AjedrezUtils.NumCasillasHastaBorde[casilla][AjedrezUtils.NORDESTE] > 0) && BitboardUtils.EstaCasillaActiva(casillaAvance + 1, movimientosValidos))
                {
                    piezaEnDestino = ObtenerPieza(casillaAvance + 1);

                    // Hay captura al este
                    if (piezaEnDestino.PieceType != Piece.Type.None && !AjedrezUtils.MismoColor(Turno, piezaEnDestino.PieceColor))
                    {
                        movimientosLegales.Add(new Move(casilla, casillaAvance + 1, Move.PROMOTE_TO_QUEEN));
                        movimientosLegales.Add(new Move(casilla, casillaAvance + 1, Move.PROMOTE_TO_KNIGHT));
                        movimientosLegales.Add(new Move(casilla, casillaAvance + 1, Move.PROMOTE_TO_ROOK));
                        movimientosLegales.Add(new Move(casilla, casillaAvance + 1, Move.PROMOTE_TO_BISHOP));
                    }
                }
            }
            else
            {
                // Verifica si el movimiento es un avance normal, si no es captura y si el flag de captura está activado
                if (!soloGenerarCapturas && piezaEnDestino.PieceType == Piece.Type.None && BitboardUtils.EstaCasillaActiva(casillaAvance, movimientosValidos))
                {
                    movimientosLegales.Add(new Move(casilla, casillaAvance, Move.NO_FLAG));
                }

                // Puede desplazarse hacia noroeste
                if ((AjedrezUtils.NumCasillasHastaBorde[casilla][AjedrezUtils.NOROESTE] > 0) && BitboardUtils.EstaCasillaActiva(casillaAvance - 1, movimientosValidos))
                {
                    piezaEnDestino = ObtenerPieza(casillaAvance - 1);

                    // Hay captura al oeste
                    if (piezaEnDestino.PieceType != Piece.Type.None && !AjedrezUtils.MismoColor(Turno, piezaEnDestino.PieceColor))
                    {
                        movimientosLegales.Add(new Move(casilla, casillaAvance - 1, Move.NO_FLAG));
                    }
                }

                // Puede desplazarse hacia nordeste
                if ((AjedrezUtils.NumCasillasHastaBorde[casilla][AjedrezUtils.NORDESTE] > 0) && BitboardUtils.EstaCasillaActiva(casillaAvance + 1, movimientosValidos))
                {
                    piezaEnDestino = ObtenerPieza(casillaAvance + 1);

                    // Hay captura al este
                    if (piezaEnDestino.PieceType != Piece.Type.None && !AjedrezUtils.MismoColor(Turno, piezaEnDestino.PieceColor))
                    {
                        movimientosLegales.Add(new Move(casilla, casillaAvance + 1, Move.NO_FLAG));
                    }
                }

                // Hay un peon vulnerable
                if (estadoActual.HasVulnerablePawn)
                {
                    // Hay captura al paso para este peón, el movimiento no salta los límites del tablero y el capturar a esa pieza no pone en jaque al rey
                    if ((AjedrezUtils.NumCasillasHastaBorde[casilla][AjedrezUtils.NOROESTE] > 0) && (PeonVulnerable == casilla - 1) && BitboardUtils.EstaCasillaActiva(casilla - 1, movimientosValidos) && !CapturaAlPasoExponeAlRey(casilla, casillasOcupadas, casillaRey, Turno))
                    {
                        // Al oeste
                        movimientosLegales.Add(new Move(casilla, casillaAvance - 1, Move.EN_PASSANT_CAPTURE));
                    }
                    else if ((AjedrezUtils.NumCasillasHastaBorde[casilla][AjedrezUtils.NORDESTE] > 0) && (PeonVulnerable == casilla + 1) && BitboardUtils.EstaCasillaActiva(casilla + 1, movimientosValidos) && !CapturaAlPasoExponeAlRey(casilla, casillasOcupadas, casillaRey, Turno))
                    {
                        // Al este
                        movimientosLegales.Add(new Move(casilla, casillaAvance + 1, Move.EN_PASSANT_CAPTURE));
                    }
                }

                // Si es el primer movimiento y no está activado el flag de sólo capturas
                if (!soloGenerarCapturas && ((AjedrezUtils.MismoColor(Turno, Piece.Color.White) && AjedrezUtils.ObtenerFila(casilla) == 1) || (AjedrezUtils.MismoColor(Turno, Piece.Color.Black) && AjedrezUtils.ObtenerFila(casilla) == 6)))
                {
                    // Puede mover dos casillas
                    piezaEnDestino = ObtenerPieza(casillaAvance);
                    casillaAvance += avance;
                    Piece piezaEnDestino2 = ObtenerPieza(casillaAvance);

                    // No lo bloquea una pieza
                    if (piezaEnDestino.PieceType == Piece.Type.None && piezaEnDestino2.PieceType == Piece.Type.None && BitboardUtils.EstaCasillaActiva(casillaAvance, movimientosValidos))
                    {
                        movimientosLegales.Add(new Move(casilla, casillaAvance, Move.PAWN_TWO_UP));
                    }
                }
            }
        }

        private ulong ObtenerBitboardMovimientosValidosRey(bool jaque, ulong casillasOcupadas)
        {
            ulong movimientosValidos = ulong.MaxValue;

            if (jaque)
            {
                movimientosValidos &= ~BitboardUtils.AtaquesDeslizantesSinFiltrar(casillasOcupadas, bitboards[AjedrezUtils.ObtenerIndicePieza(Piece.Type.Rook, AjedrezUtils.InversoColor(Turno))], bitboards[AjedrezUtils.ObtenerIndicePieza(Piece.Type.Bishop, AjedrezUtils.InversoColor(Turno))], bitboards[AjedrezUtils.ObtenerIndicePieza(Piece.Type.Queen, AjedrezUtils.InversoColor(Turno))], bitboards[AjedrezUtils.ObtenerIndicePieza(Piece.Type.King, Turno)]);
            }

            return movimientosValidos;
        }

        private ulong ObtenerBitboardMovimientosValidos(int casillaPieza, bool jaque, ulong atacantesRey, ulong piezasClavadas, ulong pinners, int casillaRey)
        {
            ulong movimientosValidos = ulong.MaxValue;

            if (jaque)
            {
                // Hay jaque
                if (BitboardUtils.MasDeUnBitActivo(atacantesRey) || BitboardUtils.EstaCasillaActiva(casillaPieza, piezasClavadas))
                {
                    // Si hay más de un atacante, o la pieza está clavada, no se puede mover
                    movimientosValidos = 0UL;
                }
                else
                {
                    //No hay más de un atacante y la pieza no está clavada
                    int casillaAtacante = BitboardUtils.PrimerBitActivo(atacantesRey);

                    if (ObtenerPieza(casillaAtacante).PieceType.EsDeslizante())
                    {
                        // La pieza sólo puede moverse una casilla entre el atacante y el rey, o a la casilla del atacante
                        movimientosValidos = atacantesRey | BitboardUtils.ObtenerCasillasEntre(casillaAtacante, casillaRey);
                    }
                    else
                    {
                        // El atacante no es una pieza deslizante, sólo puede ser capturado
                        movimientosValidos = atacantesRey;
                    }
                }
            }
            else if (BitboardUtils.EstaCasillaActiva(casillaPieza, piezasClavadas))
            {
                // La pieza está clavada
                while (pinners != 0)
                {
                    // Encontrar al pinner que clava a esta pieza
                    int sq = BitboardUtils.PrimerBitActivo(pinners);
                    ulong casillasEntre = BitboardUtils.ObtenerCasillasEntre(sq, casillaRey);
                    if (BitboardUtils.EstaCasillaActiva(casillaPieza, casillasEntre))
                    {
                        // Pinner encontrado
                        movimientosValidos = casillasEntre | (1UL << sq);
                        break;
                    }
                    pinners &= pinners - 1;
                }
            }

            return movimientosValidos;
        }

        private bool CapturaAlPasoExponeAlRey(int casillaPeon, ulong casillasOcupadas, int casillaRey, Piece.Color colorRey)
        {
            // Si el peon en cuestión es una pieza clavada quitando el peon vulnerable, delvolver true, si no, devolver false
            (ulong clavadasSinPeonVulnerable, _) = ObtenerPiezasClavadas(casillasOcupadas & ~BitboardUtils.SetBit(PeonVulnerable), casillaRey, colorRey);

            if (BitboardUtils.EstaCasillaActiva(casillaPeon, clavadasSinPeonVulnerable))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void EliminarPieza(Piece pieza, int casilla)
        {
            // Añadimos la pieza a la lista de piezas muertas
            piezasMuertas.Push((pieza, casilla));

            // Eliminamos la pieza del bitboard
            EliminarPiezaDeBitboard(pieza.PieceType, pieza.PieceColor, casilla);

            // Marcamos que hay captura
            estadoActual.HasCapture = true;

            // Actualizar Zobrist hash captura
            estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashCasilla(estadoActual.ZobristHash, pieza, casilla);
        }

        private bool CasillaVacia(ulong ocupadas, int casilla)
        {
            return (ocupadas & BitboardUtils.SetBit(casilla)) == 0;
        }

        private bool CasillaAtacada(ulong casillasOcupadas, int casilla, Piece.Color color)
        {
            // 1. Verificar si un peón ataca la casilla
            ulong peones = bitboards[AjedrezUtils.ObtenerIndicePieza(Piece.Type.Pawn, color)]; // Peones del color contrario
            if ((BitboardUtils.AtaquesPeon[AjedrezUtils.MismoColor(color, Piece.Color.White) ? 0 : 1, casilla] & peones) != 0)
                return true;

            // 2. Verificar si un caballo ataca la casilla
            ulong caballos = bitboards[AjedrezUtils.ObtenerIndicePieza(Piece.Type.Knight, color)]; // Caballos del color contrario
            if ((BitboardUtils.AtaquesCaballo[casilla] & caballos) != 0)
                return true;

            // 3. Verificar si un rey ataca la casilla
            ulong rey = bitboards[AjedrezUtils.ObtenerIndicePieza(Piece.Type.King, color)]; // King del color contrario
            if ((BitboardUtils.AtaquesRey[casilla] & rey) != 0)
                return true;

            // 4. Verificar si una reina o un alfil atacan la casilla
            ulong reinasAlfiles = bitboards[AjedrezUtils.ObtenerIndicePieza(Piece.Type.Queen, color)] // Reinas del color contrario
                                | bitboards[AjedrezUtils.ObtenerIndicePieza(Piece.Type.Bishop, color)]; // Alfiles del color contrario

            if ((BitboardUtils.AtaquesAlfil(casillasOcupadas, casilla) & reinasAlfiles) != 0)
                return true;

            // 5. Verificar si una reina o una torre atacan la casilla
            ulong reinasTorres = bitboards[AjedrezUtils.ObtenerIndicePieza(Piece.Type.Queen, color)] // Reinas del color contrario
                                | bitboards[AjedrezUtils.ObtenerIndicePieza(Piece.Type.Rook, color)]; // Torres del color contrario

            if ((BitboardUtils.AtaquesTorre(casillasOcupadas, casilla) & reinasTorres) != 0)
                return true;

            return false;
        }

        private ulong CasillaAtacadaPor(ulong casillasOcupadas, int casilla, Piece.Color color)
        {
            ulong atacantes = 0;

            // 1. Verificar si un peón ataca la casilla
            ulong peones = bitboards[AjedrezUtils.ObtenerIndicePieza(Piece.Type.Pawn, color)]; // Peones del color contrario
            atacantes |= (BitboardUtils.AtaquesPeon[AjedrezUtils.MismoColor(color, Piece.Color.White) ? 0 : 1, casilla] & peones);

            // 2. Verificar si un caballo ataca la casilla
            ulong caballos = bitboards[AjedrezUtils.ObtenerIndicePieza(Piece.Type.Knight, color)]; // Caballos del color contrario
            atacantes |= (BitboardUtils.AtaquesCaballo[casilla] & caballos);

            // 3. Verificar si un rey ataca la casilla
            ulong rey = bitboards[AjedrezUtils.ObtenerIndicePieza(Piece.Type.King, color)]; // King del color contrario
            atacantes |= (BitboardUtils.AtaquesRey[casilla] & rey);

            // 4. Verificar si una reina o un alfil atacan la casilla
            ulong reinasAlfiles = bitboards[AjedrezUtils.ObtenerIndicePieza(Piece.Type.Queen, color)] // Reinas del color contrario
                                | bitboards[AjedrezUtils.ObtenerIndicePieza(Piece.Type.Bishop, color)]; // Alfiles del color contrario

            atacantes |= (BitboardUtils.AtaquesAlfil(casillasOcupadas, casilla) & reinasAlfiles);

            // 5. Verificar si una reina o una torre atacan la casilla
            ulong reinasTorres = bitboards[AjedrezUtils.ObtenerIndicePieza(Piece.Type.Queen, color)] // Reinas del color contrario
                                | bitboards[AjedrezUtils.ObtenerIndicePieza(Piece.Type.Rook, color)]; // Torres del color contrario

            atacantes |= (BitboardUtils.AtaquesTorre(casillasOcupadas, casilla) & reinasTorres);

            return atacantes;
        }

        private void ActualizarBitboardsMovimiento(Piece.Type tipoPieza, Piece.Color color, int origen, int destino)
        {
            bitboards[AjedrezUtils.ObtenerIndicePieza(tipoPieza, color)] &= ~(1UL << origen); // limpiar casilla origen
            bitboards[AjedrezUtils.ObtenerIndicePieza(tipoPieza, color)] |= (1UL << destino); // poner casilla destino
        }

        private void ActualizarBitboardsMovimiento(Piece pieza, int origen, int destino)
        {
            bitboards[AjedrezUtils.ObtenerIndicePieza(pieza)] &= ~(1UL << origen); // limpiar casilla origen
            bitboards[AjedrezUtils.ObtenerIndicePieza(pieza)] |= (1UL << destino); // poner casilla destino
        }

        private void EliminarPiezaDeBitboard(Piece.Type tipoPieza, Piece.Color color, int casilla)
        {
            bitboards[AjedrezUtils.ObtenerIndicePieza(tipoPieza, color)] &= ~(1UL << casilla);
        }

        private void EliminarPiezaDeBitboard(Piece pieza, int casilla)
        {
            bitboards[AjedrezUtils.ObtenerIndicePieza(pieza)] &= ~(1UL << casilla);
        }

        private void ActualizarBitboardsPromocion(Piece.Type tipoPieza, Piece.Color color, int casilla)
        {
            bitboards[AjedrezUtils.ObtenerIndicePieza(Piece.Type.Pawn, color)] &= ~(1UL << casilla);
            bitboards[AjedrezUtils.ObtenerIndicePieza(tipoPieza, color)] |= (1UL << casilla);
        }

        private void ActualizarBitboardsPromocion(Piece pieza, int casilla)
        {
            bitboards[AjedrezUtils.ObtenerIndicePieza(Piece.Type.Pawn, pieza.PieceColor)] &= ~(1UL << casilla);
            bitboards[AjedrezUtils.ObtenerIndicePieza(pieza.PieceType, pieza.PieceColor)] |= (1UL << casilla);
        }

        private void DesactualizarBitboardsPromocion(Piece.Type tipoPieza, Piece.Color color, int casilla)
        {
            bitboards[AjedrezUtils.ObtenerIndicePieza(tipoPieza, color)] &= ~(1UL << casilla);
            bitboards[AjedrezUtils.ObtenerIndicePieza(Piece.Type.Pawn, color)] |= (1UL << casilla);
        }

        private void DesactualizarBitboardsPromocion(Piece pieza, int casilla)
        {
            bitboards[AjedrezUtils.ObtenerIndicePieza(pieza.PieceType, pieza.PieceColor)] &= ~(1UL << casilla);
            bitboards[AjedrezUtils.ObtenerIndicePieza(Piece.Type.Pawn, pieza.PieceColor)] |= (1UL << casilla);
        }

        private ulong ObtenerOcupadasBlancas()
        {
            return bitboards[0] | bitboards[1] | bitboards[2] | bitboards[3] | bitboards[4] | bitboards[5];
        }

        private ulong ObtenerOcupadasNegras()
        {
            return bitboards[6] | bitboards[7] | bitboards[8] | bitboards[9] | bitboards[10] | bitboards[11];
        }

        private ulong ObtenerOcupadas()
        {
            return ObtenerOcupadasBlancas() | ObtenerOcupadasNegras();
        }

        public int ObtenerCasillaRey(Piece.Color color)
        {
            ulong rey = bitboards[AjedrezUtils.ObtenerIndicePieza(Piece.Type.King, color)];
            if (rey == 0)
            {
                return -1;
            }

            return BitboardUtils.PrimerBitActivo(rey);
        }

        private (ulong pinned, ulong pinners) ObtenerPiezasClavadas(ulong casillasOcupadas, int casillaRey, Piece.Color colorRey)
        {
            ulong piezasAliadas = AjedrezUtils.MismoColor(colorRey, Piece.Color.White) ? ObtenerOcupadasBlancas() : ObtenerOcupadasNegras();
            ulong pinned = 0;

            ulong pinners = BitboardUtils.AtaquesRayosXTorre(casillasOcupadas, piezasAliadas, casillaRey) & (bitboards[AjedrezUtils.ObtenerIndicePieza(Piece.Type.Rook, AjedrezUtils.InversoColor(colorRey))] | bitboards[AjedrezUtils.ObtenerIndicePieza(Piece.Type.Queen, AjedrezUtils.InversoColor(colorRey))]);
            ulong pinnersRetorno = pinners;
            while (pinners != 0)
            {
                int sq = BitboardUtils.PrimerBitActivo(pinners);
                pinned |= BitboardUtils.ObtenerCasillasEntre(sq, casillaRey) & piezasAliadas;
                pinners &= pinners - 1;
            }

            pinners = BitboardUtils.AtaquesRayosXAlfil(casillasOcupadas, piezasAliadas, casillaRey) & (bitboards[AjedrezUtils.ObtenerIndicePieza(Piece.Type.Bishop, AjedrezUtils.InversoColor(colorRey))] | bitboards[AjedrezUtils.ObtenerIndicePieza(Piece.Type.Queen, AjedrezUtils.InversoColor(colorRey))]);
            pinnersRetorno |= pinners;
            while (pinners != 0)
            {
                int sq = BitboardUtils.PrimerBitActivo(pinners);
                pinned |= BitboardUtils.ObtenerCasillasEntre(sq, casillaRey) & piezasAliadas;
                pinners &= pinners - 1;
            }

            return (pinned, pinnersRetorno);
        }

        public Piece ObtenerPieza(int casilla)
        {
            for (int i = 0; i < bitboards.Length; i++)
            {
                if ((bitboards[i] & BitboardUtils.SetBit(casilla)) != 0)
                {
                    Piece.Color color = (i < 6) ? Piece.Color.White : Piece.Color.Black;
                    int tipoIndex = i % 6;

                    Piece.Type tipo = tipoIndex switch
                    {
                        0 => Piece.Type.King,
                        1 => Piece.Type.Queen,
                        2 => Piece.Type.Rook,
                        3 => Piece.Type.Bishop,
                        4 => Piece.Type.Knight,
                        5 => Piece.Type.Pawn,
                        _ => Piece.Type.None
                    };

                    return new Piece(tipo, color);
                }
            }

            // Si no hay pieza en la casilla, devolvemos una pieza vacía (Nada, Blancas)
            return new Piece(Piece.Type.None, Piece.Color.White);
        }

        public bool CasillaAtacadaPorPeonRival(int casilla)
        {
            if (AjedrezUtils.MismoColor(Turno, Piece.Color.White))
            {
                ulong peonesRivales = bitboards[11];
                // Ataques posibles a 'casilla' desde peones rivales
                ulong ataques = BitboardUtils.AtaquesPeon[1, casilla];
                return (ataques & peonesRivales) != 0;
            }
            else
            {
                ulong peonesRivales = bitboards[5];
                // Ataques posibles a 'casilla' desde peones rivales
                ulong ataques = BitboardUtils.AtaquesPeon[0, casilla];
                return (ataques & peonesRivales) != 0;
            }
        }

        public ulong ObtenerPiezasClavadasDebug()
        {
            (ulong piezasClavadas, _) = ObtenerPiezasClavadas(ObtenerOcupadas(), ObtenerCasillaRey(Turno), Turno);
            return piezasClavadas;
        }

        public ulong ObtenerPiezasAtacandoReyDebug()
        {
            return CasillaAtacadaPor(ObtenerOcupadas(), ObtenerCasillaRey(Turno), AjedrezUtils.InversoColor(Turno));
        }

        public ulong ObtenerPinnersDebug()
        {
            (_, ulong pinners) = ObtenerPiezasClavadas(ObtenerOcupadas(), ObtenerCasillaRey(Turno), Turno);
            return pinners;
        }
    }
}
