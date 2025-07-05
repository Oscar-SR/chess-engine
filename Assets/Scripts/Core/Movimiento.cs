using System;
using Ajedrez.Utilities;

namespace Ajedrez.Core
{
    public readonly struct Movimiento
    {
        // Flags
        public const int SIN_FLAG = 0b0000;
        public const int CAPTURA_AL_PASO = 0b0001;
        public const int ENROQUE = 0b0010;
        public const int PEON_MUEVE_DOS = 0b0011;
        public const int PROMOVER_A_REINA = 0b0100;
        public const int PROMOVER_A_CABALLO = 0b0101;
        public const int PROMOVER_A_TORRE = 0b0110;
        public const int PROMOVER_A_ALFIL = 0b0111;

        private readonly ushort valor; // FFFFDDDDDDOOOOOO

        public Movimiento(int origen, int destino, int flag)
        {
            this.valor = (ushort)(origen | destino << 6 | flag << 12);
        }

        public Movimiento(ushort valor)
        {
            this.valor = valor;
        }

        public Movimiento(string LAN, Tablero tablero)
        {
            if (string.IsNullOrEmpty(LAN) || (LAN.Length != 4 && LAN.Length != 5))
                throw new ArgumentException("LAN inválida");

            int origen = NotacionAlgebraicaACasilla(LAN.Substring(0, 2));
            int destino = NotacionAlgebraicaACasilla(LAN.Substring(2, 2));
            Pieza.Tipo tipoPieza = tablero.ObtenerPieza(origen).TipoPieza;
            int flag = SIN_FLAG;

            if (tipoPieza == Pieza.Tipo.Peon)
            {
                if (LAN.Length == 5)
                {
                    // Promoción
                    flag = LAN[4] switch
                    {
                        'q' => PROMOVER_A_REINA,
                        'n' => PROMOVER_A_CABALLO,
                        'r' => PROMOVER_A_TORRE,
                        'b' => PROMOVER_A_ALFIL,
                        _ => throw new ArgumentException("Promoción inválida en LAN")
                    };
                }
                else if (Math.Abs(AjedrezUtils.ObtenerFila(origen) - AjedrezUtils.ObtenerFila(destino)) == 2)
                {
                    flag = PEON_MUEVE_DOS;
                }
                else if ((AjedrezUtils.ObtenerColumna(origen) != AjedrezUtils.ObtenerColumna(destino)) && (tablero.ObtenerPieza(destino).TipoPieza == Pieza.Tipo.Nada))
                {
                    flag = CAPTURA_AL_PASO;
                }
            }
            else if ((tipoPieza == Pieza.Tipo.Rey) && (Math.Abs(AjedrezUtils.ObtenerColumna(origen) - AjedrezUtils.ObtenerColumna(destino)) > 1))
            {
                // Enroque
                flag = ENROQUE;
            }

            this.valor = (ushort)(origen | (destino << 6) | (flag << 12));
        }
        public static Movimiento Nulo => new Movimiento(0);

        public int Origen
        {
            get
            {
                return this.valor & 0b0000000000111111;
            }
        }

        public int Destino
        {
            get
            {
                return (this.valor & 0b0000111111000000) >> 6;
            }
        }

        public int Flag
        {
            get
            {
                return this.valor >> 12;
            }
        }

        public bool EsPromocion()
        {
            return Flag == PROMOVER_A_REINA || Flag == PROMOVER_A_CABALLO
                || Flag == PROMOVER_A_TORRE || Flag == PROMOVER_A_ALFIL;
        }

        public string ToLAN()
        {
            string origenLAN = CasillaANotacionAlgebraica(Origen);
            string destinoLAN = CasillaANotacionAlgebraica(Destino);
            string sufijo = "";

            if (EsPromocion())
            {
                sufijo = Flag switch
                {
                    PROMOVER_A_REINA => "q",
                    PROMOVER_A_CABALLO => "n",
                    PROMOVER_A_TORRE => "r",
                    PROMOVER_A_ALFIL => "b",
                    _ => ""
                };
            }

            return origenLAN + destinoLAN + sufijo;
        }

        private static int NotacionAlgebraicaACasilla(string casilla)
        {
            char columna = casilla[0];
            char fila = casilla[1];
            int x = columna - 'a';     // 0 a 7
            int y = fila - '1';        // 0 a 7
            return y * 8 + x;          // fila*8 + columna
        }

        private string CasillaANotacionAlgebraica(int indice)
        {
            char columna = (char)('a' + (indice % 8));
            char fila = (char)('1' + (indice / 8));
            return $"{columna}{fila}";
        }

        public static bool operator ==(Movimiento a, Movimiento b)
        {
            return a.valor == b.valor;
        }

        public static bool operator !=(Movimiento a, Movimiento b)
        {
            return a.valor != b.valor;
        }

        public override bool Equals(object obj)
        {
            return obj is Movimiento otro && this.valor == otro.valor;
        }

        public override int GetHashCode()
        {
            return valor.GetHashCode();
        }
    }
}
