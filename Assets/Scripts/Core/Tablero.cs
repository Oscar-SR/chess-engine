using System;
using System.Collections.Generic;
using Ajedrez.Utilities;

namespace Ajedrez.Core
{
    public class Tablero
    {
        public enum TipoSituacionTablero : byte
        {
            Normal,
            Jaque,
            JaqueMate,
            ReyAhogado,
            TriplePosicionRepetida,
            Regla50Movimientos,
            MaterialInsuficiente,
            SinTiempo
        }

        public const int MAX_PLYS_INACTIVO = 50;

        private ulong[] bitboards;
        private Stack<(Pieza, int)> piezasMuertas;
        private EstadoTablero estadoActual;
        private Stack<EstadoTablero> historialEstados;
        private PilaRepeticiones historialPosicionesRepetidas; // Mantiene un historial de las últimas posiciones hasheadas, hasta el último movimiento irreversible
        private uint numMovimientosTotales;

        public Tablero()
        {
            CargarFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
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
                    Pieza pieza = ObtenerPieza(indice);

                    if (pieza.TipoPieza == Pieza.Tipo.Nada)
                    {
                        casillasVacias++;
                    }
                    else
                    {
                        char piezaCaracter;

                        switch (pieza.TipoPieza)
                        {
                            case Pieza.Tipo.Peon:
                                piezaCaracter = 'p';
                                break;

                            case Pieza.Tipo.Caballo:
                                piezaCaracter = 'n';
                                break;

                            case Pieza.Tipo.Alfil:
                                piezaCaracter = 'b';
                                break;

                            case Pieza.Tipo.Torre:
                                piezaCaracter = 'r';
                                break;

                            case Pieza.Tipo.Reina:
                                piezaCaracter = 'q';
                                break;

                            case Pieza.Tipo.Rey:
                                piezaCaracter = 'k';
                                break;

                            default:
                                piezaCaracter = 'x';
                                break;
                        }

                        if (pieza.ColorPieza == Pieza.Color.Blancas)
                        {
                            piezaCaracter = char.ToUpper(piezaCaracter);
                        }

                        if (casillasVacias > 0)
                        {
                            fen += casillasVacias.ToString();
                            casillasVacias = 0;
                        }
                        fen += piezaCaracter;
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
            fen += estadoActual.Turno == Pieza.Color.Blancas ? " w " : " b ";

            // 3 Enroques
            string enroques = "";
            if (estadoActual.EnroqueDisponible(EstadoTablero.ENROQUE_CORTO_BLANCAS)) enroques += "K";
            if (estadoActual.EnroqueDisponible(EstadoTablero.ENROQUE_LARGO_BLANCAS)) enroques += "Q";
            if (estadoActual.EnroqueDisponible(EstadoTablero.ENROQUE_CORTO_NEGRAS)) enroques += "k";
            if (estadoActual.EnroqueDisponible(EstadoTablero.ENROQUE_LARGO_NEGRAS)) enroques += "q";
            fen += string.IsNullOrEmpty(enroques) ? "-" : enroques;
            fen += " ";

            // 4 Peón al paso
            if (incluirPeonAlPaso && estadoActual.HayPeonVulnerable)
            {
                (int fila, int columna) = AjedrezUtils.IndiceACoordenadas(estadoActual.CasillaPeonVulnerable);
                char colCaracter = (char)('a' + columna);
                if (fila == 3)
                {
                    fila--;
                }
                else
                {
                    fila++;
                }
                char filaCaracter = (char)('1' + fila);
                fen += $"{colCaracter}{filaCaracter}";
            }
            else
            {
                fen += "-";
            }

            // 5 Número de plys inactivo
            fen += $" {estadoActual.NumPlysInactivo}";

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
            piezasMuertas = new Stack<(Pieza, int)>();
            historialEstados = new Stack<EstadoTablero>(capacity: 64);
            historialPosicionesRepetidas = new PilaRepeticiones();

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
                        Pieza.Color color = char.IsUpper(c) ? Pieza.Color.Blancas : Pieza.Color.Negras;
                        int indice = (7 - fila) * 8 + columna;

                        Pieza.Tipo tipoPieza = char.ToLower(c) switch
                        {
                            'p' => Pieza.Tipo.Peon,
                            'n' => Pieza.Tipo.Caballo,
                            'b' => Pieza.Tipo.Alfil,
                            'r' => Pieza.Tipo.Torre,
                            'q' => Pieza.Tipo.Reina,
                            'k' => Pieza.Tipo.Rey,
                            _ => throw new ArgumentException($"Caracter de pieza desconocido: '{c}' en fila {fila + 1}.", nameof(fen))
                        };

                        bitboards[AjedrezUtils.ObtenerIndicePieza(tipoPieza, color)] |= BitboardUtils.SetBit(indice);
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

            estadoActual.Turno = turnoFEN == "w" ? Pieza.Color.Blancas : Pieza.Color.Negras;

            // 3. Enroques disponibles
            if (!System.Text.RegularExpressions.Regex.IsMatch(enroquesFEN, "^(K?Q?k?q?|\\-)$"))
                throw new ArgumentException($"Formato de enroques inválido: '{enroquesFEN}'.", nameof(fen));

            if (enroquesFEN.Contains("K")) estadoActual.InicializarEnroquesDisponibles(EstadoTablero.ENROQUE_CORTO_BLANCAS);
            if (enroquesFEN.Contains("Q")) estadoActual.InicializarEnroquesDisponibles(EstadoTablero.ENROQUE_LARGO_BLANCAS);
            if (enroquesFEN.Contains("k")) estadoActual.InicializarEnroquesDisponibles(EstadoTablero.ENROQUE_CORTO_NEGRAS);
            if (enroquesFEN.Contains("q")) estadoActual.InicializarEnroquesDisponibles(EstadoTablero.ENROQUE_LARGO_NEGRAS);

            // 4. Peón al paso
            if (peonAlPasoFEN != "-")
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(peonAlPasoFEN, "^[a-h][36]$"))
                    throw new ArgumentException($"Valor de peón al paso inválido: '{peonAlPasoFEN}'.", nameof(fen));

                int col = peonAlPasoFEN[0] - 'a';
                int fila = int.Parse(peonAlPasoFEN[1].ToString()) - 1;
                //estadoActual.CasillaPeonVulnerable = (fila * 8) + col;
                if (fila == 2)
                    estadoActual.CasillaPeonVulnerable = (24 + col);
                else
                    estadoActual.CasillaPeonVulnerable = (32 + col);
            }
            else
            {
                estadoActual.HayPeonVulnerable = false;
            }

            // 5. Número de plys inactivo
            if (!byte.TryParse(plysFEN, out byte plys))
                throw new ArgumentException($"Número de medio movimientos inválido: '{plysFEN}'.", nameof(fen));

            estadoActual.NumPlysInactivo = plys;

            // 6. Número de movimientos totales
            if (!uint.TryParse(movimientosFEN, out uint movimientos))
                throw new ArgumentException($"Número de movimientos inválido: '{movimientosFEN}'.", nameof(fen));

            numMovimientosTotales = movimientos;

            // 7. Zobrist hash y repetición
            estadoActual.ZobristHash = ZobristHashing.CrearZobristHash(this);
            historialPosicionesRepetidas.Push(estadoActual.ZobristHash, true);
        }

        public EstadoTablero EstadoActual
        {
            get
            {
                return estadoActual;
            }
        }

        public Pieza.Color Turno
        {
            get
            {
                return estadoActual.Turno;
            }
        }

        private int PeonVulnerable
        {
            get
            {
                return estadoActual.CasillaPeonVulnerable;
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

        /*
        public void CargarFEN(string fen)
        {
            if (!EsFENValido(fen))
                throw new ArgumentException("La cadena FEN es inválida: " + fen);

            bitboards = new ulong[12];
            piezasMuertas = new Stack<(Pieza, int)>();
            historialEstados = new Stack<EstadoTablero>(capacity: 64);
            historialPosicionesRepetidas = new PilaRepeticiones();

            string[] partes = fen.Split(' ');

            // 1. Piezas en el tablero
            string[] filas = partes[0].Split('/');
            for (int fila = 0; fila < 8; fila++)
            {
                int columna = 0;
                foreach (char c in filas[fila])
                {
                    if (char.IsDigit(c))
                    {
                        columna += c - '0';
                    }
                    else
                    {
                        Pieza.Color color = char.IsUpper(c) ? Pieza.Color.Blancas : Pieza.Color.Negras;
                        int indice = (7 - fila) * 8 + columna;

                        Pieza.Tipo tipoPieza = char.ToLower(c) switch
                        {
                            'p' => Pieza.Tipo.Peon,
                            'n' => Pieza.Tipo.Caballo,
                            'b' => Pieza.Tipo.Alfil,
                            'r' => Pieza.Tipo.Torre,
                            'q' => Pieza.Tipo.Reina,
                            'k' => Pieza.Tipo.Rey,
                            _ => throw new Exception($"Tipo de pieza desconocido: {c}")
                        };

                        bitboards[AjedrezUtils.ObtenerIndicePieza(tipoPieza, color)] |= BitboardUtils.SetBit(indice);

                        columna++;
                    }
                }
            }

            // 2. Color del turno
            estadoActual.Turno = partes[1] == "w" ? Pieza.Color.Blancas : Pieza.Color.Negras;

            // 3. Enroques disponibles
            if (partes[2].Contains("K")) estadoActual.InicializarEnroquesDisponibles(EstadoTablero.ENROQUE_CORTO_BLANCAS);
            if (partes[2].Contains("Q")) estadoActual.InicializarEnroquesDisponibles(EstadoTablero.ENROQUE_LARGO_BLANCAS);
            if (partes[2].Contains("k")) estadoActual.InicializarEnroquesDisponibles(EstadoTablero.ENROQUE_CORTO_NEGRAS);
            if (partes[2].Contains("q")) estadoActual.InicializarEnroquesDisponibles(EstadoTablero.ENROQUE_LARGO_NEGRAS);

            // 4. Peón al paso
            if (partes[3] != "-")
            {
                int col = partes[3][0] - 'a';
                int fila = int.Parse(partes[3][1].ToString()) - 1;
                if (fila == 2)
                    estadoActual.CasillaPeonVulnerable = (24 + col);
                else
                    estadoActual.CasillaPeonVulnerable = (32 + col);
            }
            else
            {
                estadoActual.HayPeonVulnerable = false;
            }

            // 5. Número de plys inactivo
            estadoActual.NumPlysInactivo = partes.Length > 4
                ? (byte)byte.Parse(partes[4])
                : (byte)0;

            // 6. Número de movimientos totales
            numMovimientosTotales = partes.Length > 5 ? uint.Parse(partes[5]) : 1;

            // 7. Generar el Zobrist hash
            estadoActual.ZobristHash = ZobristHashing.CrearZobristHash(this);

            // 8. Añadir el Zobrist hash al historial de posiciones (como movimiento irreversible, puesto que es la primera posición)
            historialPosicionesRepetidas.Push(estadoActual.ZobristHash, true);
        }

        private bool EsFENValido(string fen)
        {
            if (string.IsNullOrWhiteSpace(fen)) return false;

            string[] partes = fen.Trim().Split(' ');
            if (partes.Length != 6) return false;

            string piezas = partes[0];
            string turno = partes[1];
            string enroques = partes[2];
            string peonAlPaso = partes[3];
            string plys = partes[4];
            string movimientos = partes[5];

            // Validar layout de piezas
            string[] filas = piezas.Split('/');
            if (filas.Length != 8) return false;
            foreach (var fila in filas)
            {
                int columnas = 0;
                foreach (char c in fila)
                {
                    if (char.IsDigit(c))
                    {
                        columnas += c - '0';
                    }
                    else if ("pnbrqkPNBRQK".IndexOf(c) >= 0)
                    {
                        columnas += 1;
                    }
                    else
                    {
                        return false;
                    }
                }

                if (columnas != 8) return false;
            }

            // Validar turno
            if (turno != "w" && turno != "b") return false;

            // Validar enroques
            if (!System.Text.RegularExpressions.Regex.IsMatch(enroques, "^(K?Q?k?q?|\\-)$"))
                return false;

            // Validar peón al paso
            if (peonAlPaso != "-" && !System.Text.RegularExpressions.Regex.IsMatch(peonAlPaso, "^[a-h][36]$"))
                return false;

            // Validar plys y movimientos
            if (!int.TryParse(plys, out int n1) || n1 < 0) return false;
            if (!int.TryParse(movimientos, out int n2) || n2 < 1) return false;

            return true;
        }
        */

        private void CambiarTurno()
        {
            estadoActual.Turno = AjedrezUtils.InversoColor(estadoActual.Turno);
            estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashTurno(estadoActual.ZobristHash);

            // Anular el peón vulnerable si es que lo hay
            if (estadoActual.HayPeonVulnerable)
            {
                if (AjedrezUtils.MismoColor(ObtenerPieza(PeonVulnerable).ColorPieza, Turno))
                {
                    estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashColumnasAlPaso(estadoActual.ZobristHash, PeonVulnerable); // Cuidado con mover esta línea hacia abajo, PeonVulnerable ya no devolverá la casilla
                    estadoActual.HayPeonVulnerable = false;
                }
            }
        }

        public void HacerMovimiento(Movimiento movimiento, bool enBusqueda = false)
        {
            // Añadimos el estado anterior a la pila
            historialEstados.Push(estadoActual);

            // Anulamos la captura
            estadoActual.HayCaptura = false;

            // Obtenemos el índice de la pieza que se quiere mover
            Pieza piezaEnOrigen = ObtenerPieza(movimiento.Origen);
            Pieza piezaCapturada = ObtenerPieza(movimiento.Destino);

            // Actualizar Zobrist hash movimiento
            estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashCasilla(estadoActual.ZobristHash, piezaEnOrigen, movimiento.Origen);
            if (!movimiento.EsPromocion())
                estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashCasilla(estadoActual.ZobristHash, piezaEnOrigen, movimiento.Destino);

            // Cambiamos la casilla de la pieza
            ActualizarBitboardsMovimiento(piezaEnOrigen, movimiento.Origen, movimiento.Destino);

            if (piezaCapturada.TipoPieza != Pieza.Tipo.Nada)
            {
                if (estadoActual.HayPeonVulnerable && (movimiento.Destino == PeonVulnerable))
                {
                    // La pieza capturada era el peon vulnerable, se anula
                    estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashColumnasAlPaso(estadoActual.ZobristHash, PeonVulnerable); // Cuidado con mover esta línea hacia abajo, PeonVulnerable ya no devolverá la casilla
                    estadoActual.HayPeonVulnerable = false;
                }
                else if (piezaCapturada.TipoPieza == Pieza.Tipo.Torre)
                {
                    // Cancelar los enroques correspondientes de cada torre
                    switch (movimiento.Destino)
                    {
                        case 0:
                            {
                                // Actualizar los derechos disponibles del estado
                                estadoActual.CancelarEnroque(EstadoTablero.ENROQUE_LARGO_BLANCAS);
                                break;
                            }

                        case 7:
                            {
                                // Actualizar los derechos disponibles del estado
                                estadoActual.CancelarEnroque(EstadoTablero.ENROQUE_CORTO_BLANCAS);
                                break;
                            }

                        case 56:
                            {
                                // Actualizar los derechos disponibles del estado
                                estadoActual.CancelarEnroque(EstadoTablero.ENROQUE_LARGO_NEGRAS);
                                break;
                            }

                        case 63:
                            {
                                // Actualizar los derechos disponibles del estado
                                estadoActual.CancelarEnroque(EstadoTablero.ENROQUE_CORTO_NEGRAS);
                                break;
                            }
                    }

                    // Actualizar Zobrist hash enroques disponibles
                    estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashEnroquesDisponibles(estadoActual.ZobristHash, historialEstados.Peek().EnroquesDisponibles); // Eliminar los enroques disponibles antiguos
                    estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashEnroquesDisponibles(estadoActual.ZobristHash, estadoActual.EnroquesDisponibles); // Añadir los enroques disponibles nuevos
                }

                // Hay captura
                EliminarPieza(piezaCapturada, movimiento.Destino);
            }

            // Manejamos los flag
            switch (movimiento.Flag)
            {
                case Movimiento.CAPTURA_AL_PASO:
                    {
                        EliminarPieza(new Pieza(Pieza.Tipo.Peon, AjedrezUtils.InversoColor(Turno)), PeonVulnerable);
                        estadoActual.HayPeonVulnerable = false; ;
                        break;
                    }

                case Movimiento.ENROQUE:
                    {
                        Pieza torre = new Pieza(Pieza.Tipo.Torre, Turno);

                        switch (movimiento.Destino)
                        {
                            case AjedrezUtils.ENROQUE_LARGO_BLANCAS:
                                {
                                    // Enroque largo blancas
                                    ActualizarBitboardsMovimiento(torre, 0, 3);
                                    estadoActual.CancelarEnroque(EstadoTablero.ENROQUE_CORTO_BLANCAS | EstadoTablero.ENROQUE_LARGO_BLANCAS);

                                    // Actualizar Zobrist hash movimiento
                                    estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashCasilla(estadoActual.ZobristHash, torre, 0);
                                    estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashCasilla(estadoActual.ZobristHash, torre, 3);
                                    break;
                                }

                            case AjedrezUtils.ENROQUE_CORTO_BLANCAS:
                                {
                                    // Enroque corto blancas
                                    ActualizarBitboardsMovimiento(torre, 7, 5);
                                    estadoActual.CancelarEnroque(EstadoTablero.ENROQUE_CORTO_BLANCAS | EstadoTablero.ENROQUE_LARGO_BLANCAS);

                                    // Actualizar Zobrist hash movimiento
                                    estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashCasilla(estadoActual.ZobristHash, torre, 7);
                                    estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashCasilla(estadoActual.ZobristHash, torre, 5);
                                    break;
                                }

                            case AjedrezUtils.ENROQUE_LARGO_NEGRAS:
                                {
                                    // Enroque largo negras
                                    ActualizarBitboardsMovimiento(torre, 56, 59);
                                    estadoActual.CancelarEnroque(EstadoTablero.ENROQUE_CORTO_NEGRAS | EstadoTablero.ENROQUE_LARGO_NEGRAS);

                                    // Actualizar Zobrist hash movimiento
                                    estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashCasilla(estadoActual.ZobristHash, torre, 56);
                                    estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashCasilla(estadoActual.ZobristHash, torre, 59);
                                    break;
                                }

                            case AjedrezUtils.ENROQUE_CORTO_NEGRAS:
                                {
                                    // Enroque corto negras
                                    ActualizarBitboardsMovimiento(torre, 63, 61);
                                    estadoActual.CancelarEnroque(EstadoTablero.ENROQUE_CORTO_NEGRAS | EstadoTablero.ENROQUE_LARGO_NEGRAS);

                                    // Actualizar Zobrist hash movimiento
                                    estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashCasilla(estadoActual.ZobristHash, torre, 63);
                                    estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashCasilla(estadoActual.ZobristHash, torre, 61);
                                    break;
                                }
                        }

                        // Actualizar Zobrist hash enroques disponibles
                        estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashEnroquesDisponibles(estadoActual.ZobristHash, historialEstados.Peek().EnroquesDisponibles); // Eliminar los enroques disponibles antiguos
                        estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashEnroquesDisponibles(estadoActual.ZobristHash, estadoActual.EnroquesDisponibles); // Añadir los enroques disponibles nuevos

                        break;
                    }

                case Movimiento.PEON_MUEVE_DOS:
                    {
                        estadoActual.CasillaPeonVulnerable = movimiento.Destino;
                        estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashColumnasAlPaso(estadoActual.ZobristHash, estadoActual.CasillaPeonVulnerable);
                        break;
                    }

                case Movimiento.PROMOVER_A_REINA:
                    {
                        ActualizarBitboardsPromocion(Pieza.Tipo.Reina, Turno, movimiento.Destino);
                        estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashCasilla(estadoActual.ZobristHash, new Pieza(Pieza.Tipo.Reina, Turno), movimiento.Destino);
                        break;
                    }

                case Movimiento.PROMOVER_A_CABALLO:
                    {
                        ActualizarBitboardsPromocion(Pieza.Tipo.Caballo, Turno, movimiento.Destino);
                        estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashCasilla(estadoActual.ZobristHash, new Pieza(Pieza.Tipo.Caballo, Turno), movimiento.Destino);
                        break;
                    }

                case Movimiento.PROMOVER_A_TORRE:
                    {
                        ActualizarBitboardsPromocion(Pieza.Tipo.Torre, Turno, movimiento.Destino);
                        estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashCasilla(estadoActual.ZobristHash, new Pieza(Pieza.Tipo.Torre, Turno), movimiento.Destino);
                        break;
                    }

                case Movimiento.PROMOVER_A_ALFIL:
                    {
                        ActualizarBitboardsPromocion(Pieza.Tipo.Alfil, Turno, movimiento.Destino);
                        estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashCasilla(estadoActual.ZobristHash, new Pieza(Pieza.Tipo.Alfil, Turno), movimiento.Destino);
                        break;
                    }
            }

            if (piezaEnOrigen.TipoPieza == Pieza.Tipo.Rey /*&& movimiento.Flag != Movimiento.ENROQUE*/)
            {
                if (AjedrezUtils.MismoColor(piezaEnOrigen.ColorPieza, Pieza.Color.Blancas))
                {
                    estadoActual.CancelarEnroque(EstadoTablero.ENROQUE_CORTO_BLANCAS | EstadoTablero.ENROQUE_LARGO_BLANCAS);
                }
                else
                {
                    estadoActual.CancelarEnroque(EstadoTablero.ENROQUE_CORTO_NEGRAS | EstadoTablero.ENROQUE_LARGO_NEGRAS);
                }

                // Actualizar Zobrist hash enroques disponibles
                estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashEnroquesDisponibles(estadoActual.ZobristHash, historialEstados.Peek().EnroquesDisponibles); // Eliminar los enroques disponibles antiguos
                estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashEnroquesDisponibles(estadoActual.ZobristHash, estadoActual.EnroquesDisponibles); // Añadir los enroques disponibles nuevos
            }
            else if (piezaEnOrigen.TipoPieza == Pieza.Tipo.Torre)
            {
                if (AjedrezUtils.MismoColor(piezaEnOrigen.ColorPieza, Pieza.Color.Blancas))
                {
                    if (movimiento.Origen == 0)
                        estadoActual.CancelarEnroque(EstadoTablero.ENROQUE_LARGO_BLANCAS);
                    else if (movimiento.Origen == 7)
                        estadoActual.CancelarEnroque(EstadoTablero.ENROQUE_CORTO_BLANCAS);
                }
                else
                {
                    if (movimiento.Origen == 56)
                        estadoActual.CancelarEnroque(EstadoTablero.ENROQUE_LARGO_NEGRAS);
                    else if (movimiento.Origen == 63)
                        estadoActual.CancelarEnroque(EstadoTablero.ENROQUE_CORTO_NEGRAS);
                }

                // Actualizar Zobrist hash enroques disponibles
                estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashEnroquesDisponibles(estadoActual.ZobristHash, historialEstados.Peek().EnroquesDisponibles); // Eliminar los enroques disponibles antiguos
                estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashEnroquesDisponibles(estadoActual.ZobristHash, estadoActual.EnroquesDisponibles); // Añadir los enroques disponibles nuevos
            }

            // Incrementar movimientos totales
            if (Turno == Pieza.Color.Negras)
            {
                numMovimientosTotales++;
            }

            // Cambiamos el turno
            CambiarTurno(); // Cuidado con mover esto más abajo, afectará al Zobrist hash

            // Aplicar la regla de los 50 movimientos
            if (!estadoActual.HayCaptura && piezaEnOrigen.TipoPieza != Pieza.Tipo.Peon)
            {
                estadoActual.NumPlysInactivo++;
                historialPosicionesRepetidas.Push(estadoActual.ZobristHash, false);
            }
            else
            {
                estadoActual.NumPlysInactivo = 0;
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

        public void DeshacerMovimiento(Movimiento movimiento)
        {
            // Obtenemos el índice de la pieza que quiere deshacer su movimiento
            Pieza pieza = ObtenerPieza(movimiento.Destino);

            // Cambiamos la casilla de la pieza
            ActualizarBitboardsMovimiento(pieza, movimiento.Destino, movimiento.Origen);

            // Manejamos los flag
            switch (movimiento.Flag)
            {
                case Movimiento.ENROQUE:
                    {
                        switch (movimiento.Destino)
                        {
                            case AjedrezUtils.ENROQUE_LARGO_BLANCAS:
                                {
                                    // Enroque largo blancas
                                    ActualizarBitboardsMovimiento(Pieza.Tipo.Torre, pieza.ColorPieza, 3, 0);
                                    break;
                                }

                            case AjedrezUtils.ENROQUE_CORTO_BLANCAS:
                                {
                                    // Enroque corto blancas
                                    ActualizarBitboardsMovimiento(Pieza.Tipo.Torre, pieza.ColorPieza, 5, 7);
                                    break;
                                }

                            case AjedrezUtils.ENROQUE_LARGO_NEGRAS:
                                {
                                    // Enroque largo negras
                                    ActualizarBitboardsMovimiento(Pieza.Tipo.Torre, pieza.ColorPieza, 59, 56);
                                    break;
                                }

                            case AjedrezUtils.ENROQUE_CORTO_NEGRAS:
                                {
                                    // Enroque corto negras
                                    ActualizarBitboardsMovimiento(Pieza.Tipo.Torre, pieza.ColorPieza, 61, 63);
                                    break;
                                }
                        }

                        break;
                    }

                case Movimiento.PROMOVER_A_REINA:
                    {
                        DesactualizarBitboardsPromocion(Pieza.Tipo.Reina, pieza.ColorPieza, movimiento.Origen);
                        break;
                    }

                case Movimiento.PROMOVER_A_CABALLO:
                    {
                        DesactualizarBitboardsPromocion(Pieza.Tipo.Caballo, pieza.ColorPieza, movimiento.Origen);
                        break;
                    }

                case Movimiento.PROMOVER_A_TORRE:
                    {
                        DesactualizarBitboardsPromocion(Pieza.Tipo.Torre, pieza.ColorPieza, movimiento.Origen);
                        break;
                    }

                case Movimiento.PROMOVER_A_ALFIL:
                    {
                        DesactualizarBitboardsPromocion(Pieza.Tipo.Alfil, pieza.ColorPieza, movimiento.Origen);
                        break;
                    }
            }

            if (estadoActual.HayCaptura)
            {
                (Pieza piezaCapturada, int casilla) = piezasMuertas.Pop();
                bitboards[AjedrezUtils.ObtenerIndicePieza(piezaCapturada.TipoPieza, piezaCapturada.ColorPieza)] |= BitboardUtils.SetBit(casilla);
            }

            // Decrementar movimientos totales
            if (Turno == Pieza.Color.Blancas)
            {
                numMovimientosTotales--;
            }

            // Restauramos el último estado
            estadoActual = historialEstados.Pop();

            // Eliminamos la última entrada del historial de posiciones repetidas, si tiene alguna
            historialPosicionesRepetidas.Pop();
        }

        public (List<Movimiento> movimientosLegales, bool jaque) GenerarMovimientosLegales
        (
            bool acortarGeneracion = false,  // Evitar seguir generando movimientos cuando se llega a una situación de tablas
            bool soloGenerarCapturas = false // Generar solo movimientos de captura
        )
        {
            List<Movimiento> movimientosLegales = new List<Movimiento>();
            int casillaRey = ObtenerCasillaRey(Turno);

            if (casillaRey == -1)
            {
                // No hay rey
                return (movimientosLegales, false);
            }

            if (acortarGeneracion)
            {
                if (estadoActual.NumPlysInactivo >= MAX_PLYS_INACTIVO)
                {
                    // Se cumple la regla de los 50 movimientos
                    return (movimientosLegales, false);
                }

                if (MaterialInsuficiente())
                {
                    // El material es insuficiente
                    return (movimientosLegales, false);
                }

                if (historialPosicionesRepetidas.TripleRepeticion(estadoActual.ZobristHash))
                {
                    // El último movimiento realizado supuso una triple repetición
                    return (movimientosLegales, false);
                }
            }

            ulong casillasOcupadas = ObtenerOcupadas();
            ulong atacantesRey = CasillaAtacadaPor(casillasOcupadas, casillaRey, AjedrezUtils.InversoColor(Turno));
            (ulong piezasClavadas, ulong pinners) = ObtenerPiezasClavadas(casillasOcupadas, casillaRey, Turno);
            bool jaque = atacantesRey == 0 ? false : true;
            ulong piezas = AjedrezUtils.MismoColor(Turno, Pieza.Color.Blancas) ? ObtenerOcupadasBlancas() : ObtenerOcupadasNegras();

            while (piezas != 0)
            {
                int casilla = BitboardUtils.PrimerBitActivo(piezas);
                piezas &= piezas - 1; // Borra el bit más bajo activo

                switch (ObtenerPieza(casilla).TipoPieza)
                {
                    case Pieza.Tipo.Rey:
                        {
                            GenerarMovimientosRey(movimientosLegales, casilla, casillasOcupadas, ObtenerBitboardMovimientosValidosRey(jaque, casillasOcupadas), soloGenerarCapturas);
                            break;
                        }

                    case Pieza.Tipo.Reina:
                        {
                            GenerarMovimientosReina(movimientosLegales, casilla, casillasOcupadas, ObtenerBitboardMovimientosValidos(casilla, jaque, atacantesRey, piezasClavadas, pinners, casillaRey), soloGenerarCapturas);
                            break;
                        }

                    case Pieza.Tipo.Torre:
                        {
                            GenerarMovimientosTorre(movimientosLegales, casilla, casillasOcupadas, ObtenerBitboardMovimientosValidos(casilla, jaque, atacantesRey, piezasClavadas, pinners, casillaRey), soloGenerarCapturas);
                            break;
                        }

                    case Pieza.Tipo.Alfil:
                        {
                            GenerarMovimientosAlfil(movimientosLegales, casilla, casillasOcupadas, ObtenerBitboardMovimientosValidos(casilla, jaque, atacantesRey, piezasClavadas, pinners, casillaRey), soloGenerarCapturas);
                            break;
                        }

                    case Pieza.Tipo.Caballo:
                        {
                            GenerarMovimientosCaballo(movimientosLegales, casilla, ObtenerBitboardMovimientosValidos(casilla, jaque, atacantesRey, piezasClavadas, pinners, casillaRey), soloGenerarCapturas);
                            break;
                        }

                    case Pieza.Tipo.Peon:
                        {
                            GenerarMovimientosPeon(movimientosLegales, casilla, casillaRey, casillasOcupadas, ObtenerBitboardMovimientosValidos(casilla, jaque, atacantesRey, piezasClavadas, pinners, casillaRey), soloGenerarCapturas);
                            break;
                        }
                }
            }

            return (movimientosLegales, jaque);
        }

        private void GenerarMovimientosRey(List<Movimiento> movimientosLegales, int casilla, ulong casillasOcupadas, ulong movimientosValidos, bool soloGenerarCapturas)
        {
            for (int direccion = 0; direccion < 8; direccion++)
            {
                if (AjedrezUtils.NumCasillasHastaBorde[casilla][direccion] > 0)
                {
                    int casillaDestino = casilla + AjedrezUtils.Direcciones[direccion];
                    Pieza piezaEnDestino = ObtenerPieza(casillaDestino);

                    // Si hay una pieza del mismo color, no es un movimiento válido
                    if (piezaEnDestino.TipoPieza != Pieza.Tipo.Nada && AjedrezUtils.MismoColor(piezaEnDestino.ColorPieza, Turno))
                    {
                        continue;
                    }

                    // Si solo se generan capturas y no hay una pieza en el destino, buscar en el siguente
                    if (soloGenerarCapturas && piezaEnDestino.TipoPieza == Pieza.Tipo.Nada)
                    {
                        continue;
                    }

                    // Si no es una casilla atacada y el movimiento es válido
                    if (!CasillaAtacada(casillasOcupadas, casillaDestino, AjedrezUtils.InversoColor(Turno)) && BitboardUtils.EstaCasillaActiva(casillaDestino, movimientosValidos))
                    {
                        movimientosLegales.Add(new Movimiento(casilla, casillaDestino, Movimiento.SIN_FLAG));
                    }
                }
            }

            if (!soloGenerarCapturas)
            {
                if (Turno == Pieza.Color.Blancas)
                {
                    // Enroque largo blancas
                    if (estadoActual.EnroqueDisponible(EstadoTablero.ENROQUE_LARGO_BLANCAS) && !CasillaAtacada(casillasOcupadas, casilla, AjedrezUtils.InversoColor(Turno)) && CasillaVacia(casillasOcupadas, 1) && CasillaVacia(casillasOcupadas, 2) && !CasillaAtacada(casillasOcupadas, 2, AjedrezUtils.InversoColor(Turno)) && CasillaVacia(casillasOcupadas, 3) && !CasillaAtacada(casillasOcupadas, 3, AjedrezUtils.InversoColor(Turno)))
                    {
                        movimientosLegales.Add(new Movimiento(casilla, 2, Movimiento.ENROQUE));
                    }

                    // Enroque corto blancas
                    if (estadoActual.EnroqueDisponible(EstadoTablero.ENROQUE_CORTO_BLANCAS) && !CasillaAtacada(casillasOcupadas, casilla, AjedrezUtils.InversoColor(Turno)) && CasillaVacia(casillasOcupadas, 5) && !CasillaAtacada(casillasOcupadas, 5, AjedrezUtils.InversoColor(Turno)) && CasillaVacia(casillasOcupadas, 6) && !CasillaAtacada(casillasOcupadas, 6, AjedrezUtils.InversoColor(Turno)))
                    {
                        movimientosLegales.Add(new Movimiento(casilla, 6, Movimiento.ENROQUE));
                    }
                }
                else
                {
                    // Enroque largo negras
                    if (estadoActual.EnroqueDisponible(EstadoTablero.ENROQUE_LARGO_NEGRAS) && !CasillaAtacada(casillasOcupadas, casilla, AjedrezUtils.InversoColor(Turno)) && CasillaVacia(casillasOcupadas, 57) && CasillaVacia(casillasOcupadas, 58) && !CasillaAtacada(casillasOcupadas, 58, AjedrezUtils.InversoColor(Turno)) && CasillaVacia(casillasOcupadas, 59) && !CasillaAtacada(casillasOcupadas, 59, AjedrezUtils.InversoColor(Turno)))
                    {
                        movimientosLegales.Add(new Movimiento(casilla, 58, Movimiento.ENROQUE));
                    }

                    // Enroque corto negras
                    if (estadoActual.EnroqueDisponible(EstadoTablero.ENROQUE_CORTO_NEGRAS) && !CasillaAtacada(casillasOcupadas, casilla, AjedrezUtils.InversoColor(Turno)) && CasillaVacia(casillasOcupadas, 61) && !CasillaAtacada(casillasOcupadas, 61, AjedrezUtils.InversoColor(Turno)) && CasillaVacia(casillasOcupadas, 62) && !CasillaAtacada(casillasOcupadas, 62, AjedrezUtils.InversoColor(Turno)))
                    {
                        movimientosLegales.Add(new Movimiento(casilla, 62, Movimiento.ENROQUE));
                    }
                }
            }
        }

        private void GenerarMovimientosReina(List<Movimiento> movimientosLegales, int casilla, ulong ocupadas, ulong movimientosValidos, bool soloGenerarCapturas)
        {
            ulong ataques = BitboardUtils.AtaquesTorre(ocupadas, casilla) | BitboardUtils.AtaquesAlfil(ocupadas, casilla);

            // Intersección entre ataques y movimientos válidos dados (por ejemplo, por pin o jaque)
            ulong movimientos = ataques & movimientosValidos;

            // Recorremos los bits activos (movimientos posibles)
            while (movimientos != 0)
            {
                int destino = BitboardUtils.PrimerBitActivo(movimientos);
                movimientos &= movimientos - 1; // Borra el bit más bajo activo

                Pieza piezaEnDestino = ObtenerPieza(destino);

                // Si no hay pieza del mismo color y el flag de solo generar capturas está activo, solo agregar si hay una pieza del color contrario
                if (piezaEnDestino.TipoPieza == Pieza.Tipo.Nada || !AjedrezUtils.MismoColor(piezaEnDestino.ColorPieza, Turno))
                {
                    if (!soloGenerarCapturas || (piezaEnDestino.TipoPieza != Pieza.Tipo.Nada))
                    {
                        movimientosLegales.Add(new Movimiento(casilla, destino, Movimiento.SIN_FLAG));
                    }
                }
            }
        }

        private void GenerarMovimientosTorre(List<Movimiento> movimientosLegales, int casilla, ulong ocupadas, ulong movimientosValidos, bool soloGenerarCapturas)
        {
            ulong ataques = BitboardUtils.AtaquesTorre(ocupadas, casilla);

            // Intersección entre ataques y movimientos válidos dados (por ejemplo, por pin o jaque)
            ulong movimientos = ataques & movimientosValidos;

            // Recorremos los bits activos (movimientos posibles)
            while (movimientos != 0)
            {
                int destino = BitboardUtils.PrimerBitActivo(movimientos);
                movimientos &= movimientos - 1; // Borra el bit más bajo activo

                Pieza piezaEnDestino = ObtenerPieza(destino);

                // Si no hay pieza del mismo color y el flag de solo generar capturas está activo, solo agregar si hay una pieza del color contrario
                if (piezaEnDestino.TipoPieza == Pieza.Tipo.Nada || !AjedrezUtils.MismoColor(piezaEnDestino.ColorPieza, Turno))
                {
                    if (!soloGenerarCapturas || (piezaEnDestino.TipoPieza != Pieza.Tipo.Nada))
                    {
                        movimientosLegales.Add(new Movimiento(casilla, destino, Movimiento.SIN_FLAG));
                    }
                }
            }
        }

        private void GenerarMovimientosAlfil(List<Movimiento> movimientosLegales, int casilla, ulong ocupadas, ulong movimientosValidos, bool soloGenerarCapturas)
        {
            ulong ataques = BitboardUtils.AtaquesAlfil(ocupadas, casilla);

            // Intersección entre ataques y movimientos válidos dados (por ejemplo, por pin o jaque)
            ulong movimientos = ataques & movimientosValidos;

            // Recorremos los bits activos (movimientos posibles)
            while (movimientos != 0)
            {
                int destino = BitboardUtils.PrimerBitActivo(movimientos);
                movimientos &= movimientos - 1; // Borra el bit más bajo activo

                Pieza piezaEnDestino = ObtenerPieza(destino);

                // Si no hay pieza del mismo color y el flag de solo generar capturas está activo, solo agregar si hay una pieza del color contrario
                if (piezaEnDestino.TipoPieza == Pieza.Tipo.Nada || !AjedrezUtils.MismoColor(piezaEnDestino.ColorPieza, Turno))
                {
                    if (!soloGenerarCapturas || (piezaEnDestino.TipoPieza != Pieza.Tipo.Nada))
                    {
                        movimientosLegales.Add(new Movimiento(casilla, destino, Movimiento.SIN_FLAG));
                    }
                }
            }
        }

        private void GenerarMovimientosCaballo(List<Movimiento> movimientosLegales, int casilla, ulong movimientosValidos, bool soloGenerarCapturas)
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
                Pieza piezaEnDestino = ObtenerPieza(casillaDestino);

                // Si hay una pieza del mismo color, no se puede mover ahí
                if (piezaEnDestino.TipoPieza != Pieza.Tipo.Nada && AjedrezUtils.MismoColor(piezaEnDestino.ColorPieza, Turno))
                {
                    continue;
                }

                // Si solo se generan capturas y no hay una pieza en el destino, buscar en el siguente
                if (soloGenerarCapturas && piezaEnDestino.TipoPieza == Pieza.Tipo.Nada)
                {
                    continue;
                }

                // Es un movimiento válido del caballo
                if (BitboardUtils.EstaCasillaActiva(casillaDestino, movimientosValidos))
                {
                    movimientosLegales.Add(new Movimiento(casilla, casillaDestino, Movimiento.SIN_FLAG));
                }
            }
        }

        private void GenerarMovimientosPeon(List<Movimiento> movimientosLegales, int casilla, int casillaRey, ulong casillasOcupadas, ulong movimientosValidos, bool soloGenerarCapturas)
        {
            int avance = AjedrezUtils.MismoColor(Turno, Pieza.Color.Blancas) ? 8 : -8;
            int casillaAvance = casilla + avance;
            Pieza piezaEnDestino = ObtenerPieza(casillaAvance);

            // Puede promocionar
            if ((AjedrezUtils.MismoColor(Turno, Pieza.Color.Blancas) && (AjedrezUtils.ObtenerFila(casilla) == 6)) || (AjedrezUtils.MismoColor(Turno, Pieza.Color.Negras) && (AjedrezUtils.ObtenerFila(casilla) == 1)))
            {
                // No lo bloquea una pieza
                if (piezaEnDestino.TipoPieza == Pieza.Tipo.Nada && BitboardUtils.EstaCasillaActiva(casillaAvance, movimientosValidos))
                {
                    if (!soloGenerarCapturas)
                    {
                        movimientosLegales.Add(new Movimiento(casilla, casillaAvance, Movimiento.PROMOVER_A_REINA));
                        movimientosLegales.Add(new Movimiento(casilla, casillaAvance, Movimiento.PROMOVER_A_CABALLO));
                        movimientosLegales.Add(new Movimiento(casilla, casillaAvance, Movimiento.PROMOVER_A_TORRE));
                        movimientosLegales.Add(new Movimiento(casilla, casillaAvance, Movimiento.PROMOVER_A_ALFIL));
                    }
                }

                // Puede desplazarse hacia noroeste
                if ((AjedrezUtils.NumCasillasHastaBorde[casilla][AjedrezUtils.NOROESTE] > 0) && BitboardUtils.EstaCasillaActiva(casillaAvance - 1, movimientosValidos))
                {
                    piezaEnDestino = ObtenerPieza(casillaAvance - 1);

                    // Hay captura al oeste
                    if (piezaEnDestino.TipoPieza != Pieza.Tipo.Nada && !AjedrezUtils.MismoColor(Turno, piezaEnDestino.ColorPieza))
                    {
                        movimientosLegales.Add(new Movimiento(casilla, casillaAvance - 1, Movimiento.PROMOVER_A_REINA));
                        movimientosLegales.Add(new Movimiento(casilla, casillaAvance - 1, Movimiento.PROMOVER_A_CABALLO));
                        movimientosLegales.Add(new Movimiento(casilla, casillaAvance - 1, Movimiento.PROMOVER_A_TORRE));
                        movimientosLegales.Add(new Movimiento(casilla, casillaAvance - 1, Movimiento.PROMOVER_A_ALFIL));
                    }
                }

                // Puede desplazarse hacia nordeste
                if ((AjedrezUtils.NumCasillasHastaBorde[casilla][AjedrezUtils.NORDESTE] > 0) && BitboardUtils.EstaCasillaActiva(casillaAvance + 1, movimientosValidos))
                {
                    piezaEnDestino = ObtenerPieza(casillaAvance + 1);

                    // Hay captura al este
                    if (piezaEnDestino.TipoPieza != Pieza.Tipo.Nada && !AjedrezUtils.MismoColor(Turno, piezaEnDestino.ColorPieza))
                    {
                        movimientosLegales.Add(new Movimiento(casilla, casillaAvance + 1, Movimiento.PROMOVER_A_REINA));
                        movimientosLegales.Add(new Movimiento(casilla, casillaAvance + 1, Movimiento.PROMOVER_A_CABALLO));
                        movimientosLegales.Add(new Movimiento(casilla, casillaAvance + 1, Movimiento.PROMOVER_A_TORRE));
                        movimientosLegales.Add(new Movimiento(casilla, casillaAvance + 1, Movimiento.PROMOVER_A_ALFIL));
                    }
                }
            }
            else
            {
                // Verifica si el movimiento es un avance normal, si no es captura y si el flag de captura está activado
                if (!soloGenerarCapturas && piezaEnDestino.TipoPieza == Pieza.Tipo.Nada && BitboardUtils.EstaCasillaActiva(casillaAvance, movimientosValidos))
                {
                    movimientosLegales.Add(new Movimiento(casilla, casillaAvance, Movimiento.SIN_FLAG));
                }

                // Puede desplazarse hacia noroeste
                if ((AjedrezUtils.NumCasillasHastaBorde[casilla][AjedrezUtils.NOROESTE] > 0) && BitboardUtils.EstaCasillaActiva(casillaAvance - 1, movimientosValidos))
                {
                    piezaEnDestino = ObtenerPieza(casillaAvance - 1);

                    // Hay captura al oeste
                    if (piezaEnDestino.TipoPieza != Pieza.Tipo.Nada && !AjedrezUtils.MismoColor(Turno, piezaEnDestino.ColorPieza))
                    {
                        movimientosLegales.Add(new Movimiento(casilla, casillaAvance - 1, Movimiento.SIN_FLAG));
                    }
                }

                // Puede desplazarse hacia nordeste
                if ((AjedrezUtils.NumCasillasHastaBorde[casilla][AjedrezUtils.NORDESTE] > 0) && BitboardUtils.EstaCasillaActiva(casillaAvance + 1, movimientosValidos))
                {
                    piezaEnDestino = ObtenerPieza(casillaAvance + 1);

                    // Hay captura al este
                    if (piezaEnDestino.TipoPieza != Pieza.Tipo.Nada && !AjedrezUtils.MismoColor(Turno, piezaEnDestino.ColorPieza))
                    {
                        movimientosLegales.Add(new Movimiento(casilla, casillaAvance + 1, Movimiento.SIN_FLAG));
                    }
                }

                // Hay un peon vulnerable
                if (estadoActual.HayPeonVulnerable)
                {
                    // Hay captura al paso para este peón, el movimiento no salta los límites del tablero y el capturar a esa pieza no pone en jaque al rey
                    if ((AjedrezUtils.NumCasillasHastaBorde[casilla][AjedrezUtils.NOROESTE] > 0) && (PeonVulnerable == casilla - 1) && BitboardUtils.EstaCasillaActiva(casilla - 1, movimientosValidos) && !CapturaAlPasoExponeAlRey(casilla, casillasOcupadas, casillaRey, Turno))
                    {
                        // Al oeste
                        movimientosLegales.Add(new Movimiento(casilla, casillaAvance - 1, Movimiento.CAPTURA_AL_PASO));
                    }
                    else if ((AjedrezUtils.NumCasillasHastaBorde[casilla][AjedrezUtils.NORDESTE] > 0) && (PeonVulnerable == casilla + 1) && BitboardUtils.EstaCasillaActiva(casilla + 1, movimientosValidos) && !CapturaAlPasoExponeAlRey(casilla, casillasOcupadas, casillaRey, Turno))
                    {
                        // Al este
                        movimientosLegales.Add(new Movimiento(casilla, casillaAvance + 1, Movimiento.CAPTURA_AL_PASO));
                    }
                }

                // Si es el primer movimiento y no está activado el flag de sólo capturas
                if (!soloGenerarCapturas && ((AjedrezUtils.MismoColor(Turno, Pieza.Color.Blancas) && AjedrezUtils.ObtenerFila(casilla) == 1) || (AjedrezUtils.MismoColor(Turno, Pieza.Color.Negras) && AjedrezUtils.ObtenerFila(casilla) == 6)))
                {
                    // Puede mover dos casillas
                    piezaEnDestino = ObtenerPieza(casillaAvance);
                    casillaAvance += avance;
                    Pieza piezaEnDestino2 = ObtenerPieza(casillaAvance);

                    // No lo bloquea una pieza
                    if (piezaEnDestino.TipoPieza == Pieza.Tipo.Nada && piezaEnDestino2.TipoPieza == Pieza.Tipo.Nada && BitboardUtils.EstaCasillaActiva(casillaAvance, movimientosValidos))
                    {
                        movimientosLegales.Add(new Movimiento(casilla, casillaAvance, Movimiento.PEON_MUEVE_DOS));
                    }
                }
            }
        }

        private ulong ObtenerBitboardMovimientosValidosRey(bool jaque, ulong casillasOcupadas)
        {
            ulong movimientosValidos = ulong.MaxValue;

            if (jaque)
            {
                movimientosValidos &= ~BitboardUtils.AtaquesDeslizantesSinFiltrar(casillasOcupadas, bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Torre, AjedrezUtils.InversoColor(Turno))], bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Alfil, AjedrezUtils.InversoColor(Turno))], bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Reina, AjedrezUtils.InversoColor(Turno))], bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Rey, Turno)]);
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

                    if (ObtenerPieza(casillaAtacante).TipoPieza.EsDeslizante())
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

        private bool CapturaAlPasoExponeAlRey(int casillaPeon, ulong casillasOcupadas, int casillaRey, Pieza.Color colorRey)
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

        private void EliminarPieza(Pieza pieza, int casilla)
        {
            // Añadimos la pieza a la lista de piezas muertas
            piezasMuertas.Push((pieza, casilla));

            // Eliminamos la pieza del bitboard
            EliminarPiezaDeBitboard(pieza.TipoPieza, pieza.ColorPieza, casilla);

            // Marcamos que hay captura
            estadoActual.HayCaptura = true;

            // Actualizar Zobrist hash captura
            estadoActual.ZobristHash = ZobristHashing.ActualizarZobristHashCasilla(estadoActual.ZobristHash, pieza, casilla);
        }

        private bool CasillaVacia(ulong ocupadas, int casilla)
        {
            return (ocupadas & BitboardUtils.SetBit(casilla)) == 0;
        }

        private bool CasillaAtacada(ulong casillasOcupadas, int casilla, Pieza.Color color)
        {
            // 1. Verificar si un peón ataca la casilla
            ulong peones = bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Peon, color)]; // Peones del color contrario
            if ((BitboardUtils.AtaquesPeon[AjedrezUtils.MismoColor(color, Pieza.Color.Blancas) ? 0 : 1, casilla] & peones) != 0)
                return true;

            // 2. Verificar si un caballo ataca la casilla
            ulong caballos = bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Caballo, color)]; // Caballos del color contrario
            if ((BitboardUtils.AtaquesCaballo[casilla] & caballos) != 0)
                return true;

            // 3. Verificar si un rey ataca la casilla
            ulong rey = bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Rey, color)]; // Rey del color contrario
            if ((BitboardUtils.AtaquesRey[casilla] & rey) != 0)
                return true;

            // 4. Verificar si una reina o un alfil atacan la casilla
            ulong reinasAlfiles = bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Reina, color)] // Reinas del color contrario
                                | bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Alfil, color)]; // Alfiles del color contrario

            if ((BitboardUtils.AtaquesAlfil(casillasOcupadas, casilla) & reinasAlfiles) != 0)
                return true;

            // 5. Verificar si una reina o una torre atacan la casilla
            ulong reinasTorres = bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Reina, color)] // Reinas del color contrario
                                | bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Torre, color)]; // Torres del color contrario

            if ((BitboardUtils.AtaquesTorre(casillasOcupadas, casilla) & reinasTorres) != 0)
                return true;

            return false;
        }

        private ulong CasillaAtacadaPor(ulong casillasOcupadas, int casilla, Pieza.Color color)
        {
            ulong atacantes = 0;

            // 1. Verificar si un peón ataca la casilla
            ulong peones = bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Peon, color)]; // Peones del color contrario
            atacantes |= (BitboardUtils.AtaquesPeon[AjedrezUtils.MismoColor(color, Pieza.Color.Blancas) ? 0 : 1, casilla] & peones);

            // 2. Verificar si un caballo ataca la casilla
            ulong caballos = bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Caballo, color)]; // Caballos del color contrario
            atacantes |= (BitboardUtils.AtaquesCaballo[casilla] & caballos);

            // 3. Verificar si un rey ataca la casilla
            ulong rey = bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Rey, color)]; // Rey del color contrario
            atacantes |= (BitboardUtils.AtaquesRey[casilla] & rey);

            // 4. Verificar si una reina o un alfil atacan la casilla
            ulong reinasAlfiles = bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Reina, color)] // Reinas del color contrario
                                | bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Alfil, color)]; // Alfiles del color contrario

            atacantes |= (BitboardUtils.AtaquesAlfil(casillasOcupadas, casilla) & reinasAlfiles);

            // 5. Verificar si una reina o una torre atacan la casilla
            ulong reinasTorres = bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Reina, color)] // Reinas del color contrario
                                | bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Torre, color)]; // Torres del color contrario

            atacantes |= (BitboardUtils.AtaquesTorre(casillasOcupadas, casilla) & reinasTorres);

            return atacantes;
        }

        private void ActualizarBitboardsMovimiento(Pieza.Tipo tipoPieza, Pieza.Color color, int origen, int destino)
        {
            bitboards[AjedrezUtils.ObtenerIndicePieza(tipoPieza, color)] &= ~(1UL << origen); // limpiar casilla origen
            bitboards[AjedrezUtils.ObtenerIndicePieza(tipoPieza, color)] |= (1UL << destino); // poner casilla destino
        }

        private void ActualizarBitboardsMovimiento(Pieza pieza, int origen, int destino)
        {
            bitboards[AjedrezUtils.ObtenerIndicePieza(pieza)] &= ~(1UL << origen); // limpiar casilla origen
            bitboards[AjedrezUtils.ObtenerIndicePieza(pieza)] |= (1UL << destino); // poner casilla destino
        }

        private void EliminarPiezaDeBitboard(Pieza.Tipo tipoPieza, Pieza.Color color, int casilla)
        {
            bitboards[AjedrezUtils.ObtenerIndicePieza(tipoPieza, color)] &= ~(1UL << casilla);
        }

        private void EliminarPiezaDeBitboard(Pieza pieza, int casilla)
        {
            bitboards[AjedrezUtils.ObtenerIndicePieza(pieza)] &= ~(1UL << casilla);
        }

        private void ActualizarBitboardsPromocion(Pieza.Tipo tipoPieza, Pieza.Color color, int casilla)
        {
            bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Peon, color)] &= ~(1UL << casilla);
            bitboards[AjedrezUtils.ObtenerIndicePieza(tipoPieza, color)] |= (1UL << casilla);
        }

        private void ActualizarBitboardsPromocion(Pieza pieza, int casilla)
        {
            bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Peon, pieza.ColorPieza)] &= ~(1UL << casilla);
            bitboards[AjedrezUtils.ObtenerIndicePieza(pieza.TipoPieza, pieza.ColorPieza)] |= (1UL << casilla);
        }

        private void DesactualizarBitboardsPromocion(Pieza.Tipo tipoPieza, Pieza.Color color, int casilla)
        {
            bitboards[AjedrezUtils.ObtenerIndicePieza(tipoPieza, color)] &= ~(1UL << casilla);
            bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Peon, color)] |= (1UL << casilla);
        }

        private void DesactualizarBitboardsPromocion(Pieza pieza, int casilla)
        {
            bitboards[AjedrezUtils.ObtenerIndicePieza(pieza.TipoPieza, pieza.ColorPieza)] &= ~(1UL << casilla);
            bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Peon, pieza.ColorPieza)] |= (1UL << casilla);
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

        public int ObtenerCasillaRey(Pieza.Color color)
        {
            ulong rey = bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Rey, color)];
            if (rey == 0)
            {
                return -1;
            }

            return BitboardUtils.PrimerBitActivo(rey);
        }

        private (ulong pinned, ulong pinners) ObtenerPiezasClavadas(ulong casillasOcupadas, int casillaRey, Pieza.Color colorRey)
        {
            ulong piezasAliadas = AjedrezUtils.MismoColor(colorRey, Pieza.Color.Blancas) ? ObtenerOcupadasBlancas() : ObtenerOcupadasNegras();
            ulong pinned = 0;

            ulong pinners = BitboardUtils.AtaquesRayosXTorre(casillasOcupadas, piezasAliadas, casillaRey) & (bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Torre, AjedrezUtils.InversoColor(colorRey))] | bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Reina, AjedrezUtils.InversoColor(colorRey))]);
            ulong pinnersRetorno = pinners;
            while (pinners != 0)
            {
                int sq = BitboardUtils.PrimerBitActivo(pinners);
                pinned |= BitboardUtils.ObtenerCasillasEntre(sq, casillaRey) & piezasAliadas;
                pinners &= pinners - 1;
            }

            pinners = BitboardUtils.AtaquesRayosXAlfil(casillasOcupadas, piezasAliadas, casillaRey) & (bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Alfil, AjedrezUtils.InversoColor(colorRey))] | bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Reina, AjedrezUtils.InversoColor(colorRey))]);
            pinnersRetorno |= pinners;
            while (pinners != 0)
            {
                int sq = BitboardUtils.PrimerBitActivo(pinners);
                pinned |= BitboardUtils.ObtenerCasillasEntre(sq, casillaRey) & piezasAliadas;
                pinners &= pinners - 1;
            }

            return (pinned, pinnersRetorno);
        }

        public TipoSituacionTablero ObtenerSituacionTablero(int numMovimientosLegales, bool jaque)
        {
            TipoSituacionTablero situacionTablero = TipoSituacionTablero.Normal;

            if (estadoActual.NumPlysInactivo >= MAX_PLYS_INACTIVO)
            {
                // Regla de los 50 movimientos
                situacionTablero = TipoSituacionTablero.Regla50Movimientos;
            }
            else if (MaterialInsuficiente())
            {
                // Material insuficiente
                situacionTablero = TipoSituacionTablero.MaterialInsuficiente;
            }
            else if (historialPosicionesRepetidas.TripleRepeticion(estadoActual.ZobristHash))
            {
                // El último movimiento realizado supuso una triple repetición
                situacionTablero = TipoSituacionTablero.TriplePosicionRepetida;
            }
            else if (numMovimientosLegales == 0)
            {
                if (jaque)
                {
                    // Hay jaque mate
                    situacionTablero = TipoSituacionTablero.JaqueMate;

                }
                else
                {
                    // Hay rey ahogado
                    situacionTablero = TipoSituacionTablero.ReyAhogado;
                }
            }
            else if (jaque)
            {
                // Hay jaque
                situacionTablero = TipoSituacionTablero.Jaque;
            }

            return situacionTablero;
        }

        private bool MaterialInsuficiente()
        {
            ulong piezasSinReyes = bitboards[1] | bitboards[2] | bitboards[3] | bitboards[4] | bitboards[5] | bitboards[7] | bitboards[8] | bitboards[9] | bitboards[10] | bitboards[11];

            // Solo hay reyes
            if (piezasSinReyes == 0)
                return true;
            else if (BitboardUtils.EsPotenciaDe2(piezasSinReyes))
            {
                // Solo hay rey y caballo contra rey
                if (BitboardUtils.EsPotenciaDe2(bitboards[4] | bitboards[10]))
                    return true;

                // Solo hay rey y alfil contra rey
                if (BitboardUtils.EsPotenciaDe2(bitboards[3] | bitboards[9]))
                    return true;
            }
            else if ((bitboards[1] | bitboards[2] | bitboards[4] | bitboards[5] | bitboards[7] | bitboards[8] | bitboards[10] | bitboards[11]) == 0)
            {
                // Solo hay reyes y alfiles (si los alfiles controlan casillas del mismo color)
                if ((bitboards[3] | bitboards[9]) != 0 && BitboardUtils.AlfilesEnCasillasMismoColor(bitboards[3] | bitboards[9]))
                    return true;
            }

            return false;
        }

        /*
        private bool MaterialInsuficiente()
        {
            int numPiezasSinReyes = BitboardUtils.ContarBitsActivos(bitboards[1] | bitboards[2] | bitboards[3] | bitboards[4] | bitboards[5] | bitboards[7] | bitboards[8] | bitboards[9] | bitboards[10] | bitboards[11]);

            // Solo hay reyes
            if (numPiezasSinReyes == 0)
                return true;

            // Solo hay rey y caballo contra rey
            if (numPiezasSinReyes == 1 && (BitboardUtils.ContarBitsActivos(bitboards[4] | bitboards[10]) == 1))
                return true;

            // Solo hay rey y alfil contra rey
            if (numPiezasSinReyes == 1 && (BitboardUtils.ContarBitsActivos(bitboards[3] | bitboards[9]) == 1))
                return true;

            // Solo hay reyes y alfiles (si los alfiles controlan casillas del mismo color)
            if ((bitboards[1] | bitboards[2] | bitboards[4] | bitboards[5] | bitboards[7] | bitboards[8] | bitboards[10] | bitboards[11]) == 0)
            {
                if ((bitboards[3] | bitboards[9]) != 0 && BitboardUtils.AlfilesEnCasillasMismoColor(bitboards[3] | bitboards[9]))
                    return true;
            }

            return false;
        }
        */

        public Pieza ObtenerPieza(int casilla)
        {
            for (int i = 0; i < bitboards.Length; i++)
            {
                if ((bitboards[i] & BitboardUtils.SetBit(casilla)) != 0)
                {
                    Pieza.Color color = (i < 6) ? Pieza.Color.Blancas : Pieza.Color.Negras;
                    int tipoIndex = i % 6;

                    Pieza.Tipo tipo = tipoIndex switch
                    {
                        0 => Pieza.Tipo.Rey,
                        1 => Pieza.Tipo.Reina,
                        2 => Pieza.Tipo.Torre,
                        3 => Pieza.Tipo.Alfil,
                        4 => Pieza.Tipo.Caballo,
                        5 => Pieza.Tipo.Peon,
                        _ => Pieza.Tipo.Nada
                    };

                    return new Pieza(tipo, color);
                }
            }

            // Si no hay pieza en la casilla, devolvemos una pieza vacía (Nada, Blancas)
            return new Pieza(Pieza.Tipo.Nada, Pieza.Color.Blancas);
        }

        public bool CasillaAtacadaPorPeonRival(int casilla)
        {
            if (AjedrezUtils.MismoColor(Turno, Pieza.Color.Blancas))
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
