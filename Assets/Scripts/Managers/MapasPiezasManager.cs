using UnityEngine;
using TMPro;
using Ajedrez.Utilities;
using Ajedrez.Core;
using Ajedrez.IA;

namespace Ajedrez.Managers
{
    [ExecuteInEditMode]
    public class MapasPiezasManager : MonoBehaviour
    {
        [SerializeField] private Transform mapasPiezas;
        [SerializeField] private Color colorMinimo;
        [SerializeField] private Color colorMaximo;
        [SerializeField] private Pieza.Tipo tipoPieza = Pieza.Tipo.Peon;
        [SerializeField, Range(0f, 1f)] private float valorEndgame;
        [SerializeField] private string salida;

        private int[] mapa;
        private bool necesitaActualizar = false;

        private void OnValidate()
        {
            mapa = ObtenerMapa();
            salida = ObtenerSalida();

            // Marca para regenerar mapa en Update
            necesitaActualizar = true;
        }

        private void Update()
        {
            if (necesitaActualizar)
            {
                BorrarCasillasPrevias(); // Limpia anteriores
                GenerarMapa();
                necesitaActualizar = false;
            }
        }

        private int[] ObtenerMapa()
        {
            int[] mapa;

            switch (tipoPieza)
            {
                case Pieza.Tipo.Peon:
                    {
                        mapa = CalcularMatrizTransicion(MapasPiezas.Peones, MapasPiezas.PeonesEndgame, valorEndgame);
                        break;
                    }

                case Pieza.Tipo.Caballo:
                    {
                        mapa = MapasPiezas.Caballos;
                        break;
                    }

                case Pieza.Tipo.Alfil:
                    {
                        mapa = MapasPiezas.Alfiles;
                        break;
                    }

                case Pieza.Tipo.Torre:
                    {
                        mapa = MapasPiezas.Torres;
                        break;
                    }

                case Pieza.Tipo.Reina:
                    {
                        mapa = MapasPiezas.Reinas;
                        break;
                    }
                case Pieza.Tipo.Rey:
                    {
                        mapa = CalcularMatrizTransicion(MapasPiezas.Rey, MapasPiezas.ReyEndgame, valorEndgame);
                        break;
                    }

                default:
                    mapa = null;
                    break;
            }

            return mapa;
        }

        private string ObtenerSalida()
        {
            if (tipoPieza == Pieza.Tipo.Nada)
                return "";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            for (int i = 0; i < AjedrezUtils.TAM_TABLERO; i++)
            {
                for (int j = 0; j < AjedrezUtils.TAM_TABLERO; j++)
                {
                    sb.Append(mapa[i * AjedrezUtils.TAM_TABLERO + j]);
                    if (j < AjedrezUtils.TAM_TABLERO - 1) sb.Append(", ");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private void BorrarCasillasPrevias()
        {
            for (int i = mapasPiezas.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(mapasPiezas.GetChild(i).gameObject);
            }
        }

        private void GenerarMapa()
        {
            if (mapa == null)
                return;

            int min = mapa[0];
            int max = mapa[0];

            // Buscar el valor mínimo y el máximo
            for (int i = 1; i < AjedrezUtils.MAX_INDICE; i++)
            {
                int valor = mapa[i];
                if (valor < min) min = valor;
                else if (valor > max) max = valor;
            }

            for (int i = 0; i < AjedrezUtils.TAM_TABLERO; i++)
            {
                for (int j = 0; j < AjedrezUtils.TAM_TABLERO; j++)
                {
                    Vector2 posicion = new Vector2(j - 3.5f, 3.5f - i);
                    int valor = mapa[AjedrezUtils.CoordenadasAIndice(i, j)];

                    // Normalizar el valor a [0, 1]
                    float valorNormalizado = (float)(valor - min) / (max - min);

                    // Interpolar entre Color.red (-100) y Color.green (100)
                    Color color = Color.Lerp(colorMinimo, colorMaximo, valorNormalizado);

                    InstanciarCasilla(posicion, color, valor);
                }
            }
        }

        private void InstanciarCasilla(Vector2 posicion, Color color, int valor)
        {
            GameObject casilla = new GameObject("Casilla");
            casilla.transform.parent = mapasPiezas;
            casilla.transform.position = new Vector3(posicion.x, posicion.y, 0f);
            casilla.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

            MeshFilter mf = casilla.AddComponent<MeshFilter>();
            MeshRenderer mr = casilla.AddComponent<MeshRenderer>();

            // Usa un quad de Unity
            mf.mesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

            // Material dinámico
            Shader shader = Shader.Find("Unlit/Color");
            Material material = new Material(shader);
            material.color = color;
            mr.material = material;

            // --- Añadir el TextMeshPro ---
            GameObject textoGO = new GameObject("Valor");
            textoGO.transform.SetParent(casilla.transform);
            textoGO.transform.localPosition = Vector3.zero;
            textoGO.transform.localScale = Vector3.one;

            TextMeshPro tmp = textoGO.AddComponent<TextMeshPro>();
            tmp.text = valor.ToString();
            tmp.fontSize = 5;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }

        private static int[] CalcularMatrizTransicion(int[] mapaInicial, int[] mapaFinal, float valorEndgame)
        {
            int[] resultado = new int[64];

            for (int i = 0; i < 64; i++)
            {
                resultado[i] = MapasPiezas.CalcularInterpolacion(mapaInicial[i], mapaFinal[i], valorEndgame);
            }

            return resultado;
        }
    }
}