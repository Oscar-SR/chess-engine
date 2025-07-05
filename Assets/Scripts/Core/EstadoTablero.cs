using System;
using Ajedrez.Utilities;

namespace Ajedrez.Core
{
    public struct EstadoTablero
    {
        public const int ENROQUE_CORTO_BLANCAS = 0b0001;
        public const int ENROQUE_LARGO_BLANCAS = 0b0010;
        public const int ENROQUE_CORTO_NEGRAS = 0b0100;
        public const int ENROQUE_LARGO_NEGRAS = 0b1000;

        private ushort estado; // XXXCEEEEFPPPPPPT
        private byte numPlysInactivo;
        private ulong zobristHash;

        // Bit 0 - Turno (0 blancas, 1 negras)
        public Pieza.Color Turno
        {
            get
            {
                return (this.estado & 0b0000000000000001) == 0 ? Pieza.Color.Blancas : Pieza.Color.Negras;
            }
            set
            {
                if (AjedrezUtils.MismoColor(value, Pieza.Color.Negras))
                    this.estado |= 0b0000000000000001;
                else
                    this.estado &= 0b1111111111111110;
            }
        }

        // Bits 1–6 - Casilla del peón vulnerable (0–63)
        public int CasillaPeonVulnerable
        {
            get
            {
                return (this.estado & 0b0000000001111110) >> 1;
            }
            set
            {
                this.estado = (ushort)((value << 1) | (this.estado & 0b1111111110000001) | 0b10000000);
            }
        }

        // Bit 7 - ¿Hay peón vulnerable?
        public bool HayPeonVulnerable
        {
            get
            {
                return (this.estado & 0b0000000010000000) != 0;
            }
            set
            {
                if (value)
                    estado |= 0b0000000010000000;
                else
                    estado &= 0b1111111101111111;
            }
        }

        // Bits 8–11 - EnroquesDisponibles (0000 = ninguno, 1111 = todos)
        public int EnroquesDisponibles
        {
            get
            {
                return (this.estado & 0b0000111100000000) >> 8;
            }
        }

        // Bit 12 - ¿Hay captura?
        public bool HayCaptura
        {
            get
            {
                return (this.estado & 0b0001000000000000) != 0;
            }
            set
            {
                if (value)
                    estado |= 0b0001000000000000;
                else
                    estado &= 0b1110111111111111;
            }
        }

        // Propiedad para obtener y establecer el número de plys inactivos
        public byte NumPlysInactivo
        {
            get
            {
                return numPlysInactivo;
            }
            set
            {
                // Asegurarse de que el valor esté dentro del rango válido (0–50)
                numPlysInactivo = (byte)Math.Min((byte)value, (byte)Tablero.MAX_PLYS_INACTIVO);
            }
        }

        // Propiedad para obtener y establecer el Zobrist hash
        public ulong ZobristHash
        {
            get
            {
                return zobristHash;
            }
            set
            {
                zobristHash = value;
            }
        }

        public void InicializarEnroquesDisponibles(int enroquesDisponibles)
        {
            this.estado = (ushort)(this.estado | enroquesDisponibles << 8);
        }

        public void CancelarEnroque(int tipoEnroque)
        {
            this.estado = (ushort)(this.estado & ~(tipoEnroque << 8));
        }

        public bool EnroqueDisponible(int tipoEnroque)
        {
            return (this.estado & (tipoEnroque << 8)) != 0;
        }

        public override string ToString()
        {
            const string ROJO = "#FF0000";
            const string VERDE = "#00FF00";

            string enroques = "";
            if (EnroqueDisponible(ENROQUE_CORTO_BLANCAS)) enroques += "CortoBlancas ";
            if (EnroqueDisponible(ENROQUE_LARGO_BLANCAS)) enroques += "LargoBlancas ";
            if (EnroqueDisponible(ENROQUE_CORTO_NEGRAS)) enroques += "CortoNegras ";
            if (EnroqueDisponible(ENROQUE_LARGO_NEGRAS)) enroques += "LargoNegras ";
            if (string.IsNullOrEmpty(enroques)) enroques = "Ninguno";

            return $"Estado:\n" +
                $"Turno: <color={VERDE}>{Turno}</color>\n" +
                $"¿Hay captura? {(HayCaptura ? $"<color={VERDE}>Sí</color>" : $"<color={ROJO}>No</color>")}\n" +
                $"¿Hay peón al paso? {(HayPeonVulnerable ? $"<color={VERDE}>Sí</color>" : $"<color={ROJO}>No</color>")}\n" +
                $"Casilla del peón vulnerable: {(HayPeonVulnerable ? $"<color={VERDE}>" + CasillaPeonVulnerable.ToString() + "</color>" : $"<color={ROJO}>N/A</color>")}\n" +
                $"Número de plys inactivo: <color={VERDE}>{numPlysInactivo}</color>\n" +
                $"Enroques disponibles: <color={VERDE}>{enroques}</color>\n" +
                $"Zobrist hash: <color={VERDE}>{zobristHash}</color>";
        }
    }
}