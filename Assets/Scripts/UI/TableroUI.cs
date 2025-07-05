using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Ajedrez.Core;
using Ajedrez.Systems;
using Ajedrez.Utilities;
using Ajedrez.Data;

namespace Ajedrez.UI
{
    public class TableroUI : MonoBehaviour
    {
        private const int SIN_CASILLA = -1;
        private const float VALOR_OSCURECIMIENTO = 0.4f;
        private const float DURACION_ANIMACION_MOVIMIENTO = 0.2f;

        [SerializeField] private Transform tableroTransform;
        [SerializeField] private GameObject casillaPrefab;
        [SerializeField] private ColoresTablero coloresTablero;
        [SerializeField] private PiezasSet piezasSet;
        [SerializeField] private GameObject textoCoordenadaPrefab;

        private GameObject[] casillas;
        private Dictionary<int, Transform> casillaAPieza;
        private Movimiento ultimoMovimiento = Movimiento.Nulo;
        private int ultimaCasillaReyJaque = SIN_CASILLA;
        private bool blancasAbajo = true;

        public void Init(Tablero tablero, bool blancasAbajo = true, bool interactuable = true)
        {
            // Establecer la orientación
            this.blancasAbajo = blancasAbajo;

            // Se genera el tablero
            GenerarTablero();

            // Se generan las piezas
            GenerarPiezas(tablero);

            // Inicializar el diccionario de piezas para poder moverlas
            if (interactuable)
                InicializarDiccionario();
        }

        public void RegenerarPiezas(Tablero tablero)
        {
            LimpiarPiezas();
            if (tablero != null)
                GenerarPiezas(tablero);
        }

        private void InicializarDiccionario()
        {
            casillaAPieza = new Dictionary<int, Transform>();
            GameObject[] piezas = GameObject.FindGameObjectsWithTag("Pieza");
            int casilla;

            foreach (GameObject pieza in piezas)
            {
                if (ObtenerCasillaDeCoordenada(pieza.transform.position, out casilla))
                {
                    casillaAPieza[casilla] = pieza.transform;
                }
            }
        }

        private void GenerarTablero()
        {
            // Calcula el offset para centrar el tablero en el origen
            const float offset = (AjedrezUtils.TAM_TABLERO - 1) / 2f;

            casillas = new GameObject[AjedrezUtils.MAX_INDICE];

            for (int i = 0; i < AjedrezUtils.TAM_TABLERO; i++)
            {
                for (int j = 0; j < AjedrezUtils.TAM_TABLERO; j++)
                {
                    Vector2 posicion = new Vector2((j - offset) * tableroTransform.localScale.x, (i - offset) * tableroTransform.localScale.y) + (Vector2)tableroTransform.localPosition; ;

                    // Calcula el índice de la casilla
                    int indice = AjedrezUtils.CoordenadasAIndice(i, j);
                    if (!blancasAbajo)
                        indice = AjedrezUtils.MAX_INDICE - 1 - indice;

                    // Instancia la casilla en las posición correcta
                    GameObject casilla = Instantiate(casillaPrefab, posicion, Quaternion.identity);
                    casilla.transform.localScale = tableroTransform.localScale;
                    casilla.transform.SetParent(tableroTransform);

                    // Asigna un nombre a la casilla
                    casilla.name = $"Casilla {indice}";
                    casilla.tag = "Casilla";

                    // Cambia el color de la casilla
                    casilla.GetComponent<SpriteRenderer>().color = AjedrezUtils.EsCasillaOscura(i, j) ? coloresTablero.colorCasillaOscura : coloresTablero.colorCasillaClara;
                    casillas[indice] = casilla;

                    // Letras (a-h) abajo de la primera fila (i == 0)
                    if (i == 0)
                    {
                        GameObject letra = Instantiate(textoCoordenadaPrefab, posicion, Quaternion.identity);
                        letra.transform.localScale = tableroTransform.localScale;
                        letra.transform.SetParent(tableroTransform);

                        TextMeshPro tmp = letra.GetComponent<TextMeshPro>();
                        tmp.text = ((char)('a' + j)).ToString();
                        tmp.alignment = TextAlignmentOptions.BottomLeft;
                        tmp.margin = new Vector4(0.05f, 0, 0, 0);

                        // Cambiar el color si el índice columna es par
                        if (j % 2 == 0)
                        {
                            tmp.color = coloresTablero.colorCasillaClara;
                        }
                        else
                        {
                            tmp.color = OscurecerColor(coloresTablero.colorCasillaOscura, VALOR_OSCURECIMIENTO);
                        }
                    }

                    // Números (1–8) a la izquierda de la primera columna (j == 0)
                    if (j == AjedrezUtils.TAM_TABLERO - 1)
                    {
                        GameObject numero = Instantiate(textoCoordenadaPrefab, posicion, Quaternion.identity);
                        numero.transform.localScale = tableroTransform.localScale;
                        numero.transform.SetParent(tableroTransform);

                        TextMeshPro tmp = numero.GetComponent<TextMeshPro>();
                        tmp.text = (i + 1).ToString();
                        tmp.alignment = TextAlignmentOptions.TopRight;
                        tmp.margin = new Vector4(0, 0, 0.05f, 0);

                        // Cambiar el color si el índice fila es par
                        if (i % 2 == 0)
                        {
                            tmp.color = OscurecerColor(coloresTablero.colorCasillaOscura, VALOR_OSCURECIMIENTO);
                        }
                        else
                        {
                            tmp.color = coloresTablero.colorCasillaClara;
                        }
                    }
                }
            }
        }

        Color OscurecerColor(Color color, float oscurecimiento)
        {
            float r = Mathf.Max(color.r - oscurecimiento, 0f);
            float g = Mathf.Max(color.g - oscurecimiento, 0f);
            float b = Mathf.Max(color.b - oscurecimiento, 0f);
            return new Color(r, g, b, color.a);
        }

        private void LimpiarTablero()
        {
            for (int i = tableroTransform.childCount - 1; i >= 0; i--)
            {
                Transform hijo = tableroTransform.GetChild(i);
                Destroy(hijo.gameObject);
            }
        }

        private void GenerarPiezas(Tablero tablero)
        {
            ulong piezas;

            piezas = tablero.BitboardReyBlanco;
            while (piezas != 0)
            {
                GenerarPieza(Pieza.Tipo.Rey, Pieza.Color.Blancas, BitboardUtils.PrimerBitActivo(piezas));
                piezas &= piezas - 1;
            }

            piezas = tablero.BitboardReinasBlancas;
            while (piezas != 0)
            {
                GenerarPieza(Pieza.Tipo.Reina, Pieza.Color.Blancas, BitboardUtils.PrimerBitActivo(piezas));
                piezas &= piezas - 1;
            }

            piezas = tablero.BitboardTorresBlancas;
            while (piezas != 0)
            {
                GenerarPieza(Pieza.Tipo.Torre, Pieza.Color.Blancas, BitboardUtils.PrimerBitActivo(piezas));
                piezas &= piezas - 1;
            }

            piezas = tablero.BitboardAlfilesBlancos;
            while (piezas != 0)
            {
                GenerarPieza(Pieza.Tipo.Alfil, Pieza.Color.Blancas, BitboardUtils.PrimerBitActivo(piezas));
                piezas &= piezas - 1;
            }

            piezas = tablero.BitboardCaballosBlancos;
            while (piezas != 0)
            {
                GenerarPieza(Pieza.Tipo.Caballo, Pieza.Color.Blancas, BitboardUtils.PrimerBitActivo(piezas));
                piezas &= piezas - 1;
            }

            piezas = tablero.BitboardPeonesBlancos;
            while (piezas != 0)
            {
                GenerarPieza(Pieza.Tipo.Peon, Pieza.Color.Blancas, BitboardUtils.PrimerBitActivo(piezas));
                piezas &= piezas - 1;
            }

            piezas = tablero.BitboardReyNegro;
            while (piezas != 0)
            {
                GenerarPieza(Pieza.Tipo.Rey, Pieza.Color.Negras, BitboardUtils.PrimerBitActivo(piezas));
                piezas &= piezas - 1;
            }

            piezas = tablero.BitboardReinasNegras;
            while (piezas != 0)
            {
                GenerarPieza(Pieza.Tipo.Reina, Pieza.Color.Negras, BitboardUtils.PrimerBitActivo(piezas));
                piezas &= piezas - 1;
            }

            piezas = tablero.BitboardTorresNegras;
            while (piezas != 0)
            {
                GenerarPieza(Pieza.Tipo.Torre, Pieza.Color.Negras, BitboardUtils.PrimerBitActivo(piezas));
                piezas &= piezas - 1;
            }

            piezas = tablero.BitboardAlfilesNegros;
            while (piezas != 0)
            {
                GenerarPieza(Pieza.Tipo.Alfil, Pieza.Color.Negras, BitboardUtils.PrimerBitActivo(piezas));
                piezas &= piezas - 1;
            }

            piezas = tablero.BitboardCaballosNegros;
            while (piezas != 0)
            {
                GenerarPieza(Pieza.Tipo.Caballo, Pieza.Color.Negras, BitboardUtils.PrimerBitActivo(piezas));
                piezas &= piezas - 1;
            }

            piezas = tablero.BitboardPeonesNegros;
            while (piezas != 0)
            {
                GenerarPieza(Pieza.Tipo.Peon, Pieza.Color.Negras, BitboardUtils.PrimerBitActivo(piezas));
                piezas &= piezas - 1;
            }
        }

        private void GenerarPieza(Pieza.Tipo tipoPieza, Pieza.Color color, int casilla)
        {
            string nombre;
            Sprite sprite;

            switch (tipoPieza)
            {
                case Pieza.Tipo.Rey:
                    {
                        if (AjedrezUtils.MismoColor(color, Pieza.Color.Blancas))
                        {
                            nombre = "Rey blanco";
                            sprite = piezasSet.reyBlanco;
                        }
                        else
                        {
                            nombre = "Rey negro";
                            sprite = piezasSet.reyNegro;
                        }
                        break;
                    }

                case Pieza.Tipo.Reina:
                    {
                        if (AjedrezUtils.MismoColor(color, Pieza.Color.Blancas))
                        {
                            nombre = "Reina blanca";
                            sprite = piezasSet.reinaBlanca;
                        }
                        else
                        {
                            nombre = "Reina negra";
                            sprite = piezasSet.reinaNegra;
                        }
                        break;
                    }

                case Pieza.Tipo.Torre:
                    {
                        if (AjedrezUtils.MismoColor(color, Pieza.Color.Blancas))
                        {
                            nombre = "Torre blanca";
                            sprite = piezasSet.torreBlanca;
                        }
                        else
                        {
                            nombre = "Torre negra";
                            sprite = piezasSet.torreNegra;
                        }
                        break;
                    }

                case Pieza.Tipo.Alfil:
                    {
                        if (AjedrezUtils.MismoColor(color, Pieza.Color.Blancas))
                        {
                            nombre = "Alfil blanco";
                            sprite = piezasSet.alfilBlanco;
                        }
                        else
                        {
                            nombre = "Alfil negro";
                            sprite = piezasSet.alfilNegro;
                        }
                        break;
                    }

                case Pieza.Tipo.Caballo:
                    {
                        if (AjedrezUtils.MismoColor(color, Pieza.Color.Blancas))
                        {
                            nombre = "Caballo blanco";
                            sprite = piezasSet.caballoBlanco;
                        }
                        else
                        {
                            nombre = "Caballo negro";
                            sprite = piezasSet.caballoNegro;
                        }
                        break;
                    }

                case Pieza.Tipo.Peon:
                    {
                        if (AjedrezUtils.MismoColor(color, Pieza.Color.Blancas))
                        {
                            nombre = "Peón blanco";
                            sprite = piezasSet.peonBlanco;
                        }
                        else
                        {
                            nombre = "Peón negro";
                            sprite = piezasSet.peonNegro;
                        }
                        break;
                    }

                // Indeterminada
                default:
                    {
                        nombre = "Pieza";
                        sprite = piezasSet.reyBlanco;
                        break;
                    }
            }

            // Crea el game object de la pieza
            GameObject piezaObject = new GameObject(nombre);
            piezaObject.tag = "Pieza";

            // Crear un sprite renderer para la pieza
            SpriteRenderer sr = piezaObject.AddComponent<SpriteRenderer>();

            // Asigna el sprite de la pieza al Game Object
            sr.sprite = sprite;

            // Ajusta el tamaño de la pieza
            const float factorEscala = 0.8f;
            piezaObject.transform.localScale = tableroTransform.localScale * factorEscala;

            // Asigna la capa de las piezas
            piezaObject.layer = LayerMask.NameToLayer("Piezas");
            sr.sortingLayerName = "Piezas";
            sr.sortingOrder = 0;

            // Asigna a la casilla sobre la que está posicionada la pieza, como padre
            piezaObject.transform.SetParent(casillas[casilla].transform);

            // Posiciona la pieza en el centro respecto del padre
            piezaObject.transform.localPosition = Vector3.zero;
        }

        private void LimpiarPiezas()
        {
            GameObject[] piezas = GameObject.FindGameObjectsWithTag("Pieza");

            foreach (GameObject pieza in piezas)
            {
                Destroy(pieza);
            }
        }

        public bool ObtenerCasillaDeCoordenada(Vector2 worldPos, out int casilla)
        {
            int file = (int)(worldPos.x + 4);
            int rank = (int)(worldPos.y + 4);

            if (!blancasAbajo)
            {
                file = 7 - file;
                rank = 7 - rank;
            }

            casilla = AjedrezUtils.CoordenadasAIndice(rank, file);
            return AjedrezUtils.EnTablero(rank, file);
        }

        private void ManejarFlagsHacerMovimiento(Movimiento movimiento)
        {
            switch (movimiento.Flag)
            {
                case Movimiento.ENROQUE:
                    {
                        switch (movimiento.Destino)
                        {
                            case AjedrezUtils.ENROQUE_LARGO_BLANCAS:
                                {
                                    // Enroque largo blancas
                                    CambiarCasilla(casillaAPieza[0], casillas[3].transform, 0, 3);
                                    break;
                                }

                            case AjedrezUtils.ENROQUE_CORTO_BLANCAS:
                                {
                                    // Enroque corto blancas
                                    CambiarCasilla(casillaAPieza[7], casillas[5].transform, 7, 5);
                                    break;
                                }

                            case AjedrezUtils.ENROQUE_LARGO_NEGRAS:
                                {
                                    // Enroque largo negras
                                    CambiarCasilla(casillaAPieza[56], casillas[59].transform, 56, 59);
                                    break;
                                }

                            case AjedrezUtils.ENROQUE_CORTO_NEGRAS:
                                {
                                    // Enroque corto negras
                                    CambiarCasilla(casillaAPieza[63], casillas[61].transform, 63, 61);
                                    break;
                                }
                        }

                        // Se reproduce el sonido de enroque
                        AudioSystem.Instancia.ReproducirSonido(AudioSystem.TipoSonido.Enroque);

                        break;
                    }

                case Movimiento.CAPTURA_AL_PASO:
                    {
                        // Destruir el peón al paso
                        EliminarPieza(movimiento.Destino + (AjedrezUtils.ObtenerFila(movimiento.Destino) == 5 ? -8 : 8));
                        AudioSystem.Instancia.ReproducirSonido(AudioSystem.TipoSonido.Captura);
                        break;
                    }

                case Movimiento.PROMOVER_A_REINA:
                case Movimiento.PROMOVER_A_TORRE:
                case Movimiento.PROMOVER_A_CABALLO:
                case Movimiento.PROMOVER_A_ALFIL:
                    {
                        PromoverPeon(casillaAPieza[movimiento.Destino].GetComponent<SpriteRenderer>(), movimiento.Flag, AjedrezUtils.ObtenerFila(movimiento.Destino) == 7 ? Pieza.Color.Blancas : Pieza.Color.Negras);
                        break;
                    }
            }
        }

        private void PromoverPeon(SpriteRenderer sr, int flagPromocion, Pieza.Color color)
        {
            switch (flagPromocion)
            {
                case Movimiento.PROMOVER_A_REINA:
                    if (AjedrezUtils.MismoColor(color, Pieza.Color.Blancas))
                    {
                        sr.sprite = piezasSet.reinaBlanca;
                    }
                    else
                    {
                        sr.sprite = piezasSet.reinaNegra;
                    }
                    break;

                case Movimiento.PROMOVER_A_CABALLO:
                    if (AjedrezUtils.MismoColor(color, Pieza.Color.Blancas))
                    {
                        sr.sprite = piezasSet.caballoBlanco;
                    }
                    else
                    {
                        sr.sprite = piezasSet.caballoNegro;
                    }
                    break;

                case Movimiento.PROMOVER_A_TORRE:
                    if (AjedrezUtils.MismoColor(color, Pieza.Color.Blancas))
                    {
                        sr.sprite = piezasSet.torreBlanca;
                    }
                    else
                    {
                        sr.sprite = piezasSet.torreNegra;
                    }
                    break;

                case Movimiento.PROMOVER_A_ALFIL:
                    if (AjedrezUtils.MismoColor(color, Pieza.Color.Blancas))
                    {
                        sr.sprite = piezasSet.alfilBlanco;
                    }
                    else
                    {
                        sr.sprite = piezasSet.alfilNegro;
                    }
                    break;
            }

            AudioSystem.Instancia.ReproducirSonido(AudioSystem.TipoSonido.Promocion);
        }

        public IEnumerator HacerMovimientoConAnimacion(Movimiento movimiento)
        {
            Vector3 origenPos = casillas[movimiento.Origen].transform.position;
            Vector3 destinoPos = casillas[movimiento.Destino].transform.position;
            Transform piezaTransform = casillaAPieza[movimiento.Origen];
            SpriteRenderer sr = piezaTransform.GetComponent<SpriteRenderer>();

            float tiempo = 0f;
            sr.sortingOrder = 1;
            ColorearCasillaUltimoMovimiento(movimiento.Origen);

            while (tiempo < DURACION_ANIMACION_MOVIMIENTO)
            {
                piezaTransform.position = Vector3.Lerp(origenPos, destinoPos, tiempo / DURACION_ANIMACION_MOVIMIENTO);
                tiempo += Time.deltaTime;
                yield return null;
            }

            PosicionarPieza(piezaTransform, movimiento.Origen, movimiento.Destino, movimiento.Flag);
            sr.sortingOrder = 0;

            DevolverColoresCasillasUltimoMovimiento();
            ColorearCasillaUltimoMovimiento(movimiento.Destino);
            ManejarFlagsHacerMovimiento(movimiento);

            // Guardamos el último movimiento
            ultimoMovimiento = movimiento;
        }

        public void ActualizarTablero(Movimiento movimiento)
        {
            // Se actualiza la representación de la interfaz en casos especiales
            ManejarFlagsHacerMovimiento(movimiento);

            // Guardamos el último movimiento
            ultimoMovimiento = movimiento;
        }

        private void PosicionarPieza(Transform piezaOrigen, int origen, int destino, int flagMovimiento)
        {
            Transform casillaDestino = casillas[destino].transform;

            if (casillaDestino.childCount > 0)
            {
                // Si hay una captura
                EliminarPieza(destino);
                AudioSystem.Instancia.ReproducirSonido(AudioSystem.TipoSonido.Captura);
            }
            else if (flagMovimiento != Movimiento.ENROQUE && flagMovimiento != Movimiento.CAPTURA_AL_PASO)
            {
                // Si es un movimiento normal o promoción
                AudioSystem.Instancia.ReproducirSonidoConPitchAleatorio(AudioSystem.TipoSonido.Movimiento);
            }

            CambiarCasilla(piezaOrigen, casillaDestino, origen, destino);
        }

        public void PosicionarPieza(int origen, int destino, int flagMovimiento)
        {
            PosicionarPieza(casillaAPieza[origen], origen, destino, flagMovimiento);
        }

        public void ColorearCasillaUltimoMovimiento(int casilla)
        {
            ColorearCasillaConTransparencia(casilla, coloresTablero.colorCasillaUltimoMovimiento);
        }

        public void DevolverColoresCasillasUltimoMovimiento()
        {
            if (ultimoMovimiento != Movimiento.Nulo)
            {
                // Devolvemos los colores originales a las casillas del último movimiento
                ColorearCasillaOriginal(ultimoMovimiento.Origen);
                ColorearCasillaOriginal(ultimoMovimiento.Destino);
            }
        }

        private void CambiarCasilla(Transform pieza, Transform casillaDestino, int origen, int destino)
        {
            // Cambiar posición de la pieza
            pieza.position = new Vector2(casillaDestino.position.x, casillaDestino.position.y);

            // Actualizar diccionario de piezas
            casillaAPieza.Remove(origen);
            casillaAPieza[destino] = pieza;

            // Cambiar padre de la pieza
            pieza.SetParent(casillaDestino);
        }

        private void EliminarPieza(int casilla)
        {
            Destroy(casillaAPieza[casilla].gameObject);
            casillaAPieza.Remove(casilla);
        }

        public void ColorearCasillaJaque(bool jaque, int casillaRey)
        {
            if (ultimaCasillaReyJaque != SIN_CASILLA)
            {
                // Devolver el color original a la casilla del rey en jaque anterior
                ColorearCasillaOriginal(ultimaCasillaReyJaque);
            }

            if (jaque)
            {
                // Reproducir el sonido de jaque
                AudioSystem.Instancia.ReproducirSonido(AudioSystem.TipoSonido.Jaque);

                // Colorear la casilla del rey
                ultimaCasillaReyJaque = casillaRey;
                ColorearCasilla(ultimaCasillaReyJaque, coloresTablero.colorCasillaJaque);
            }
            else
            {
                ultimaCasillaReyJaque = SIN_CASILLA;
            }
        }

        private void ColorearCasillaConTransparencia(int casilla, Color color)
        {
            ColorearCasilla(casilla, Color.Lerp(AjedrezUtils.EsCasillaOscura(casilla) ? coloresTablero.colorCasillaOscura : coloresTablero.colorCasillaClara, color, 0.5f));
        }

        private void ColorearCasilla(int casilla, Color color)
        {
            casillas[casilla].GetComponent<SpriteRenderer>().color = color;
        }

        public void ColorearCasillaOriginal(int casilla)
        {
            ColorearCasilla(casilla, AjedrezUtils.EsCasillaOscura(casilla) ? coloresTablero.colorCasillaOscura : coloresTablero.colorCasillaClara);
        }

        public void MostrarMovimientosLegales(List<Movimiento> movimientosLegales)
        {
            foreach (Movimiento movimiento in movimientosLegales)
            {
                // Cambiamos la tonalidad de la casilla de destino a rojo
                ColorearCasillaConTransparencia(movimiento.Destino, coloresTablero.colorCasillaMovimientos);
            }
        }

        public void OcultarMovimientosLegales(List<Movimiento> movimientosLegales)
        {
            foreach (Movimiento movimiento in movimientosLegales)
            {
                // Cambiamos la tonalidad de la casilla de destino a la original
                ColorearCasillaOriginal(movimiento.Destino);
            }

            if (ultimoMovimiento != Movimiento.Nulo)
            {
                // Nos aseguramos de volver a colorear las casillas del último movimiento
                ColorearCasillaUltimoMovimiento(ultimoMovimiento.Origen);
                ColorearCasillaUltimoMovimiento(ultimoMovimiento.Destino);
            }

            if (ultimaCasillaReyJaque != SIN_CASILLA)
            {
                // Colorear la casilla del rey en jaque
                ColorearCasilla(ultimaCasillaReyJaque, coloresTablero.colorCasillaJaque);
            }
        }

        public void ArrastrarPieza(int casillaPieza, Vector2 cursor)
        {
            casillaAPieza[casillaPieza].transform.position = cursor;
        }

        public void DevolverPiezaOrigen(int casilla)
        {
            casillaAPieza[casilla].position = casillas[casilla].transform.position;
        }

        public void DibujarPiezaPorEncima(int casilla)
        {
            casillaAPieza[casilla].GetComponent<SpriteRenderer>().sortingOrder = 1;
        }

        public void DevolverPiezaACapa(int casilla)
        {
            casillaAPieza[casilla].GetComponent<SpriteRenderer>().sortingOrder = 0;
        }

        public void CambiarPerspectiva(Tablero tablero)
        {
            blancasAbajo = !blancasAbajo;
            LimpiarTablero();
            GenerarTablero();
            if (tablero != null)
                GenerarPiezas(tablero);
        }

        public (Pieza.Color colorJugador1, Pieza.Color colorJugador2) ObtenerColoresJugadores()
        {
            if (blancasAbajo)
            {
                return (Pieza.Color.Blancas, Pieza.Color.Negras);
            }
            else
            {
                return (Pieza.Color.Negras, Pieza.Color.Blancas);
            }
        }
    }
}