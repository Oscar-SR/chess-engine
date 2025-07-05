using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using Ajedrez.Managers;
using Ajedrez.Core;
using Ajedrez.Utilities;
using Ajedrez.IA;
using Ajedrez.UI;

namespace Ajedrez.Debugging
{
    public enum ModoDebugBitboard : byte
    {
        Apagado,
        ReyBlanco,
        ReinasBlancas,
        TorresBlancas,
        AlfilesBlancos,
        CaballosBlancos,
        PeonesBlancos,
        ReyNegro,
        ReinasNegras,
        TorresNegras,
        AlfilesNegros,
        CaballosNegros,
        PeonesNegros,
        PiezasClavadas,
        AtacantesRey,
        Pinners
    }

    public class BitboardDebugger : MonoBehaviour
    {
        public static BitboardDebugger Instancia { get; private set; }

        private const string ROJO_CLARO = "#00FFFF80";
        private const string CIAN = "#FF000080";

        [SerializeField] private Transform tableroTransform;
        [SerializeField] private Sprite spriteMarcaDebug;
        [SerializeField] private TextMeshProUGUI textoModoDebug;

        private bool iniciado = false;
        private Color colorBitboard1;
        private Color colorBitboard0;
        private Vector2 posicionTablero;
        private Tablero tablero;
        private ModoDebugBitboard modoActual = ModoDebugBitboard.Apagado;

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                Instancia = this;
            }
        }

        private void OnEnable()
        {
            // Cuidado con el script execution order
            if (InputManager.Instancia != null)
            {
                InputManager.Instancia.OnTeclaDebugBitboard += CambiarModoDebugBitboard;
            }
        }

        private void OnDisable()
        {
            if (InputManager.Instancia != null)
            {
                InputManager.Instancia.OnTeclaDebugBitboard -= CambiarModoDebugBitboard;
            }
        }

        void Start()
        {
            // Inicializar la posición del tablero
            posicionTablero = tableroTransform.position;

            // Cargar colores desde hex
            if (!ColorUtility.TryParseHtmlString(ROJO_CLARO, out colorBitboard1))
                Debug.LogError("Error al convertir ROJO_CLARO a Color.");

            if (!ColorUtility.TryParseHtmlString(CIAN, out colorBitboard0))
                Debug.LogError("Error al convertir CIAN a Color.");
        }

        public void Init(Tablero tablero)
        {
            this.tablero = tablero;
            iniciado = true;
        }

        public void ActualizarModoDebugBitboard()
        {
            if (modoActual != ModoDebugBitboard.Apagado)
            {
                MostrarModoDebugBitboard(modoActual);
            }
        }

        private void MostrarModoDebugBitboard(ModoDebugBitboard modo)
        {
            if (!iniciado)
                return;
            
            switch (modo)
            {
                case ModoDebugBitboard.Apagado:
                    {
                        BorrarDebug();
                        textoModoDebug.text = "";
                        break;
                    }

                    case ModoDebugBitboard.ReyBlanco:
                    {
                        MostrarDebugBitboard(tablero.BitboardReyBlanco);
                        break;
                    }

                    case ModoDebugBitboard.ReinasBlancas:
                    {
                        MostrarDebugBitboard(tablero.BitboardReinasBlancas);
                        break;
                    }

                    case ModoDebugBitboard.TorresBlancas:
                    {
                        MostrarDebugBitboard(tablero.BitboardTorresBlancas);
                        break;
                    }

                    case ModoDebugBitboard.AlfilesBlancos:
                    {
                        MostrarDebugBitboard(tablero.BitboardAlfilesBlancos);
                        break;
                    }

                    case ModoDebugBitboard.CaballosBlancos:
                    {
                        MostrarDebugBitboard(tablero.BitboardCaballosBlancos);
                        break;
                    }

                    case ModoDebugBitboard.PeonesBlancos:
                    {
                        MostrarDebugBitboard(tablero.BitboardPeonesBlancos);
                        break;
                    }

                    case ModoDebugBitboard.ReyNegro:
                    {
                        MostrarDebugBitboard(tablero.BitboardReyNegro);
                        break;
                    }

                    case ModoDebugBitboard.ReinasNegras:
                    {
                        MostrarDebugBitboard(tablero.BitboardReinasNegras);
                        break;
                    }

                    case ModoDebugBitboard.TorresNegras:
                    {
                        MostrarDebugBitboard(tablero.BitboardTorresNegras);
                        break;
                    }

                    case ModoDebugBitboard.AlfilesNegros:
                    {
                        MostrarDebugBitboard(tablero.BitboardAlfilesNegros);
                        break;
                    }

                    case ModoDebugBitboard.CaballosNegros:
                    {
                        MostrarDebugBitboard(tablero.BitboardCaballosNegros);
                        break;
                    }

                    case ModoDebugBitboard.PeonesNegros:
                    {
                        MostrarDebugBitboard(tablero.BitboardPeonesNegros);
                        break;
                    }

                    case ModoDebugBitboard.PiezasClavadas:
                    {
                        MostrarDebugBitboard(tablero.ObtenerPiezasClavadasDebug());
                        break;
                    }

                    case ModoDebugBitboard.AtacantesRey:
                    {
                        MostrarDebugBitboard(tablero.ObtenerPiezasAtacandoReyDebug());
                        break;
                    }

                    case ModoDebugBitboard.Pinners:
                    {
                        MostrarDebugBitboard(tablero.ObtenerPinnersDebug());
                        break;
                    }
            }
        }

        private void CambiarModoDebugBitboard(ModoDebugBitboard nuevoModo)
        {
            if(modoActual != nuevoModo)
            {
                modoActual = nuevoModo;
                MostrarModoDebugBitboard(nuevoModo);
            }
        }

        private void BorrarDebug()
        {
            GameObject[] objetosDebug = GameObject.FindGameObjectsWithTag("Debug");

            foreach (GameObject obj in objetosDebug)
            {
                Destroy(obj);
            }
        }

        private string FormatearBitboard(ulong bitboard)
        {
            var sb = new System.Text.StringBuilder(64 * 40); // más espacio por las etiquetas
            for (int i = 63; i >= 0; i--)
            {
                bool bit = (bitboard & BitboardUtils.SetBit(i)) != 0;
                string color = bit ? ROJO_CLARO : CIAN;
                sb.Append($"<color={color}>{(bit ? '1' : '0')}</color>");
                
                // Opcional: para separar cada 8 bits visualmente
                if (i % 8 == 0 && i != 0)
                    sb.Append(" ");
            }
            return sb.ToString();
        }

        private void MostrarDebugBitboard(ulong bitboard)
        {
            BorrarDebug();
            textoModoDebug.text = $"{modoActual}\n{FormatearBitboard(bitboard)}";

            for (int casilla = 0; casilla < AjedrezUtils.MAX_INDICE; casilla++)
            {
                (int i, int j) = AjedrezUtils.IndiceACoordenadas(casilla);
                Vector2 posicion = new Vector2(-3.5f + j, -3.5f + i);

                // Verificamos si el bit está activo en esa casilla
                if ((bitboard & BitboardUtils.SetBit(casilla)) != 0)
                {
                    InstanciarCasillaDebug(posicionTablero + posicion, 1, colorBitboard1);
                }
                else
                {
                    InstanciarCasillaDebug(posicionTablero + posicion, 0, colorBitboard0);
                }
            }
        }

        public void InstanciarCasillaDebug(Vector2 posicion, int numero, Color color)
        {
            GameObject marcaDebug = new GameObject("DebugBitboard");

            // Le asignar el tag "Debug"
            marcaDebug.tag = "Debug";

            // SpriteRenderer para la casilla
            SpriteRenderer sr = marcaDebug.AddComponent<SpriteRenderer>();
            sr.sprite = spriteMarcaDebug;
            sr.color = color;
            sr.sortingLayerName = "Debug";
            sr.sortingOrder = 0;
            marcaDebug.transform.position = posicion;

            // Crear el texto como hijo
            GameObject texto = new GameObject("Numero");
            texto.tag = "Debug";
            texto.transform.SetParent(marcaDebug.transform);
            texto.transform.localPosition = new Vector3(0, 0, -0.1f);

            // Componente TextMesh
            TextMesh textMesh = texto.AddComponent<TextMesh>();
            textMesh.text = numero.ToString();
            textMesh.fontSize = 100;
            textMesh.characterSize = 0.05f;
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.color = Color.white;

            // Para que se vea encima de la casilla
            MeshRenderer renderer = texto.GetComponent<MeshRenderer>();
            renderer.sortingLayerName = "Debug";
            renderer.sortingOrder = 1;
        }
    }
}
