using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Ajedrez.Managers;
using Ajedrez.Core;
using Ajedrez.Utilities;
using Ajedrez.Debugging;

namespace Ajedrez.UI
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instancia { get; private set; }
        public event Action<ModoDebugBitboard> OnTeclaDebugBitboard;
        public event Action OnToggleEstadoTablero;
        public event Action OnToggleDebugBusqueda;

        // Comunicarse con el tablero con eventos también, quizás

        [SerializeField] private PartidaManager partidaManager;
        [SerializeField] private PromocionUI promocionUI;
        [SerializeField] private TableroUI tableroUI;

        private Mouse cursor;
        private Keyboard keyboard;
        private Camera cam;
        private LayerMask capaPiezas;
        private int casilla;

        private Pieza.Color colorInteractuable = Pieza.Color.Nada;
        private bool piezaSeleccionada = false;
        private bool promocionEnCurso = false;
        private Vector2 offsetCursorPieza;

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

        // Start is called before the first frame update
        void Start()
        {
            cam = Camera.main;
            capaPiezas = LayerMask.GetMask("Piezas");
            cursor = Mouse.current;
            keyboard = Keyboard.current;

            if (cursor == null)
            {
                Debug.LogError("No se detectó un dispositivo de cursor");
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (colorInteractuable != Pieza.Color.Nada)
            {
                if (piezaSeleccionada)
                {
                    if (!promocionEnCurso)
                    {
                        // Seguir al ratón con la pieza
                        partidaManager.ArrastrarPieza(casilla, PosicionCursor());

                        if (cursor.leftButton.wasReleasedThisFrame)
                        {
                            // Comprobar si se puede colocar la pieza
                            ColocarPieza();
                        }
                    }
                }
                else
                {
                    if (!promocionEnCurso && cursor.leftButton.wasPressedThisFrame)
                    {
                        // Comprobar si se ha seleccionado una pieza
                        SeleccionarPieza();
                    }
                }
            }

#if UNITY_EDITOR
            // BitboardDebugger
            if (keyboard.qKey.wasPressedThisFrame)
                OnTeclaDebugBitboard?.Invoke(ModoDebugBitboard.Apagado);
            if (keyboard.digit1Key.wasPressedThisFrame)
                OnTeclaDebugBitboard?.Invoke(keyboard.tabKey.isPressed ? ModoDebugBitboard.ReyNegro : ModoDebugBitboard.ReyBlanco);
            if (keyboard.digit2Key.wasPressedThisFrame)
                OnTeclaDebugBitboard?.Invoke(keyboard.tabKey.isPressed ? ModoDebugBitboard.ReinasNegras : ModoDebugBitboard.ReinasBlancas);
            if (keyboard.digit3Key.wasPressedThisFrame)
                OnTeclaDebugBitboard?.Invoke(keyboard.tabKey.isPressed ? ModoDebugBitboard.TorresNegras : ModoDebugBitboard.TorresBlancas);
            if (keyboard.digit4Key.wasPressedThisFrame)
                OnTeclaDebugBitboard?.Invoke(keyboard.tabKey.isPressed ? ModoDebugBitboard.AlfilesNegros : ModoDebugBitboard.AlfilesBlancos);
            if (keyboard.digit5Key.wasPressedThisFrame)
                OnTeclaDebugBitboard?.Invoke(keyboard.tabKey.isPressed ? ModoDebugBitboard.CaballosNegros : ModoDebugBitboard.CaballosBlancos);
            if (keyboard.digit6Key.wasPressedThisFrame)
                OnTeclaDebugBitboard?.Invoke(keyboard.tabKey.isPressed ? ModoDebugBitboard.PeonesNegros : ModoDebugBitboard.PeonesBlancos);
            if (keyboard.cKey.wasPressedThisFrame)
                OnTeclaDebugBitboard?.Invoke(ModoDebugBitboard.PiezasClavadas);
            if (keyboard.rKey.wasPressedThisFrame)
                OnTeclaDebugBitboard?.Invoke(ModoDebugBitboard.AtacantesRey);
            if (keyboard.pKey.wasPressedThisFrame)
                OnTeclaDebugBitboard?.Invoke(ModoDebugBitboard.Pinners);

            // EstadoDebugger
            if (keyboard.eKey.wasPressedThisFrame)
                OnToggleEstadoTablero?.Invoke();

            // BusquedaDebugger
            if (keyboard.bKey.wasPressedThisFrame)
                OnToggleDebugBusqueda?.Invoke();
#endif
        }

        private Vector2 PosicionCursor()
        {
            Vector2 posCursorPantalla = cursor.position.ReadValue();
            return cam.ScreenToWorldPoint(posCursorPantalla);
        }

        void SeleccionarPieza()
        {
            Vector2 clickPosition = PosicionCursor();

            if (partidaManager.ObtenerCasillaDeCoordenada(clickPosition, out casilla))
            {
                Pieza pieza = partidaManager.ObtenerPieza(casilla);
                if (pieza.TipoPieza != Pieza.Tipo.Nada && AjedrezUtils.MismoColor(pieza.ColorPieza, colorInteractuable))
                {
                    piezaSeleccionada = true;
                    partidaManager.DibujarPiezaPorEncima(casilla);
                    partidaManager.ColorearCasillaUltimoMovimiento(casilla);
                    partidaManager.MostrarMovimientosLegalesDePieza(casilla);
                }
            }
        }

        private void ColocarPieza()
        {
            Vector2 clickPosition = PosicionCursor();
            int nuevaCasilla;

            if (partidaManager.ObtenerCasillaDeCoordenada(clickPosition, out nuevaCasilla))
            {
                if (nuevaCasilla != casilla)
                {
                    Movimiento movimiento = partidaManager.ObtenerMovimientoLegal(nuevaCasilla);

                    // Comprobar si se puede hacer el movimiento
                    if (movimiento != Movimiento.Nulo)
                    {
                        // Sí se puede hacer el movimiento
                        partidaManager.PosicionarPieza(casilla, nuevaCasilla, movimiento.Flag);
                        partidaManager.OcultarMovimientosLegales();
                        partidaManager.DevolverPiezaACapa(nuevaCasilla);
                        partidaManager.DevolverColoresCasillasUltimoMovimiento();
                        partidaManager.ColorearCasillaUltimoMovimiento(nuevaCasilla);
                        StartCoroutine(HacerMovimientoTrasPromocion(movimiento));
                        return;
                    }
                }
            }

            DeseleccionarPieza();
        }

        private void DeseleccionarPieza()
        {
            partidaManager.ColorearCasillaOriginal(casilla);
            partidaManager.OcultarMovimientosLegales();
            partidaManager.DevolverPiezaACapa(casilla);
            partidaManager.DevolverPiezaOrigen(casilla);
            piezaSeleccionada = false;
        }

        private IEnumerator HacerMovimientoTrasPromocion(Movimiento movimiento)
        {
            // Desactivar la interacción con las piezas
            colorInteractuable = Pieza.Color.Nada;
            piezaSeleccionada = false;

            if (movimiento.EsPromocion())
            {
                promocionEnCurso = true;
                int tipoPromocion = Movimiento.PROMOVER_A_REINA;

                promocionUI.Mostrar(partidaManager.ObtenerPieza(casilla).ColorPieza, tipo =>
                {
                    tipoPromocion = tipo;
                    promocionEnCurso = false;
                });

                yield return new WaitUntil(() => !promocionEnCurso);

                movimiento = new Movimiento(movimiento.Origen, movimiento.Destino, tipoPromocion);
            }

            StartCoroutine(partidaManager.HacerMovimiento(movimiento));
        }

        public Pieza.Color ColorInteractuable
        {
            set
            {
                colorInteractuable = value;

                if (value == Pieza.Color.Nada && piezaSeleccionada)
                {
                    // Soltar la pieza si se termina la partida y hay una pieza seleccionada
                    DeseleccionarPieza();
                }
            }
        }
    }
}