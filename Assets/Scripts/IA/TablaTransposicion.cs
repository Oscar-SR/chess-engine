using Ajedrez.Core;

namespace Ajedrez.IA
{
    public class TablaTransposicion
    {
        public const int BUSQUEDA_FALLIDA = -1;

        // No todas las evaluaciones tienen el mismo grado de certeza.
        // Por eso, se etiqueta cada entrada con uno de estos tres tipos de valor.
        public enum TipoEvaluacion : byte
        {
            Exacta = 0, // Se evaluaron todos los movimientos desde esta posición sin realizar una poda
            CotaInferior = 1, // Se encontró un movimiento demasiado bueno y se hizo una poda beta, así que podría haber uno incluso mejor. Significa que se encontró una jugada tan buena que el oponente nunca permitirá llegar a esta posición, porque evitará esa línea.
            CotaSuperior = 2 // Se refiere a cuando ningún movimiento supera a alpha, así que todos fueron descartados como peores
        }

        private Entrada[] entradas;
        private readonly ulong numEntradas;

        public struct Entrada
        {

            private ulong clave;
            private int valor;
            private byte profundidad;
            private TipoEvaluacion tipoNodo;
            private Movimiento movimiento;

            public Entrada(ulong clave, int valor, byte profundidad, TipoEvaluacion tipoNodo, Movimiento movimiento)
            {
                this.clave = clave;
                this.valor = valor;
                this.profundidad = profundidad; // equivalente a profundidadRestante en cada caso
                this.tipoNodo = tipoNodo;
                this.movimiento = movimiento;
            }

            public ulong Clave => clave;
            public int Valor => valor;
            public byte Profundidad => profundidad;
            public TipoEvaluacion TipoNodo => tipoNodo;
            public Movimiento Movimiento => movimiento;
        }

        public TablaTransposicion(int tamMB)
        {
            int tamBytesEntrada = System.Runtime.InteropServices.Marshal.SizeOf<Entrada>();
            int tamBytesTabla = tamMB * 1024 * 1024;
            numEntradas = (ulong)(tamBytesTabla / tamBytesEntrada);
            entradas = new Entrada[numEntradas];
        }

        private ulong ObtenerIndice(ulong hash)
        {
            return hash % numEntradas;
        }

        public int BuscarEvaluacion(ulong hash, int profundidadRestante, int profundidadDesdeRaiz, int alfa, int beta)
        {
            Entrada entrada = entradas[ObtenerIndice(hash)];

            // Si la entrada está rellenada con la información de esa posición
            if (entrada.Clave == hash)
            {
                // Solo reutilizar evaluaciones de igual o mayor profundidad que la actual
                if (entrada.Profundidad >= profundidadRestante)
                {
                    int evaluacion = CorregirEvaluacionJaqueMateBuscar(entrada.Valor, profundidadDesdeRaiz);

                    // Si es una evaluación exacta, puede devolverse directamente
                    if (entrada.TipoNodo == TipoEvaluacion.Exacta)
                    {
                        return evaluacion;
                    }

                    // Si lo mejor que el oponente puede hacer no supera alfa, esta rama es segura.
                    if (entrada.TipoNodo == TipoEvaluacion.CotaSuperior && evaluacion <= alfa)
                    {
                        return evaluacion;
                    }

                    // Si lo peor que el oponente puede hacer es mayor que beta, entonces puedes hacer una poda beta
                    if (entrada.TipoNodo == TipoEvaluacion.CotaInferior && evaluacion >= beta)
                    {
                        return evaluacion;
                    }
                }
            }

            return BUSQUEDA_FALLIDA;
        }

        public void GuardarEvaluacion(ulong hash, int profundidadRestante, int profundidadDesdeRaiz, int evaluacion, TipoEvaluacion tipoEvaluacion, Movimiento movimiento)
        {
            ulong indice = ObtenerIndice(hash);

            //if (profundidadRestante >= entradas[indice].profundidadRestante) {
            Entrada entrada = new Entrada(hash, CorregirEvaluacionJaqueMateGuardar(evaluacion, profundidadDesdeRaiz), (byte)profundidadRestante, tipoEvaluacion, movimiento);
            entradas[indice] = entrada;
            //}
        }

        public Movimiento ObtenerMovimientoGuardado(ulong hash)
        {
            return entradas[ObtenerIndice(hash)].Movimiento;
        }

        public int ObtenerEvaluacionGuardada(ulong hash)
        {
            return entradas[ObtenerIndice(hash)].Valor;
        }

        // Convierte un valor de mate local (desde la posición actual), en un valor de mate absoluto (relativo a la raíz del árbol de búsqueda)
        private int CorregirEvaluacionJaqueMateGuardar(int evaluacion, int profundidadDesdeRaiz)
        {
            if (Busqueda.EsJaqueMate(evaluacion))
            {
                int signo = System.Math.Sign(evaluacion);
                return (evaluacion * signo + profundidadDesdeRaiz) * signo;
            }

            return evaluacion;
        }

        // Convierte un valor de mate absoluto (relativo a la raíz del árbol de búsqueda), en un valor de mate local (desde la posición actual)
        private int CorregirEvaluacionJaqueMateBuscar(int evaluacion, int profundidadDesdeRaiz)
        {
            if (Busqueda.EsJaqueMate(evaluacion))
            {
                int signo = System.Math.Sign(evaluacion);
                return (evaluacion * signo - profundidadDesdeRaiz) * signo;
            }

            return evaluacion;
        }

        public void Limpiar()
		{
			for (ulong i = 0; i < numEntradas; i++)
			{
				entradas[i] = new Entrada();
			}
		}

        public float PorcentajeOcupacion
        {
            get
            {
                ulong ocupadas = 0;

                for (ulong i = 0; i < numEntradas; i++)
                {
                    if (entradas[i].Clave != 0) // Asumiendo clave=0 significa entrada vacía
                    {
                        ocupadas++;
                    }
                }

                return (ocupadas / (float)numEntradas) * 100f;
            }
        }
    }
}