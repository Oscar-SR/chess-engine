using System;

namespace Ajedrez.Core
{
    public class PilaRepeticiones
    {
        private const int MAX_POSICIONES_ALMACENADAS = 128;

        private readonly ulong[] hashes; // Array que contiene los hashes de las posiciones (0 -> Base de la pila, 64 -> Cima de la pila)
        private readonly int[] indicesComienzo; // Array que contiene los índices en los que empieza un segmento válido (un segmento válido viene determinado después de ocurrir un movimiento irreversible)
        private int contador; // Índice de la última posición registrada en hashes

        public PilaRepeticiones()
        {
            hashes = new ulong[MAX_POSICIONES_ALMACENADAS];
            indicesComienzo = new int[MAX_POSICIONES_ALMACENADAS + 1];
            contador = 0;
        }

        public PilaRepeticiones(int maxPosiciones)
        {
            hashes = new ulong[maxPosiciones];
            indicesComienzo = new int[maxPosiciones + 1];
            contador = 0;
        }

        public void Push(ulong hash, bool movimientoIrreversible)
        {
            // Comprobar límites
            if (contador < hashes.Length)
            {
                hashes[contador] = hash;
                indicesComienzo[contador + 1] = movimientoIrreversible ? contador : indicesComienzo[contador];
            }
            contador++;
        }

        public void Pop()
        {
            contador = Math.Max(0, contador - 1);
        }

        public bool TripleRepeticion(ulong hash)
        {
            int comienzo = indicesComienzo[contador];
            int repeticiones = 0;

            for (int i = comienzo; i < contador - 1; i++) // excluye la posición actual (hasta contador - 1)
            {
                if (hashes[i] == hash)
                {
                    repeticiones++;
                    if (repeticiones == 2) // si se repite 2 veces (ya apareció 2 veces antes)
                        return true;
                }
            }

            /// TODO:
            /// Si se encuentra 1 coincidencia
            /// y el ply actual es mayor que el ply de la raíz + 2
            /// (es decir, que ya se ha avanzado un poco en la búsqueda),
            /// también devuelve un empate.

            return false;
        }
        
        public void Clear()
        {
            Array.Clear(hashes, 0, contador);
            Array.Clear(indicesComienzo, 0, contador + 1);
            contador = 0;
        }
    }
}