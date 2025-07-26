using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Ajedrez.Debugging.Enfrentamiento
{
    public class JugadorEnfrentamientoUI : MonoBehaviour
    {
        [SerializeField] private JugadorEnfrentamientoManager jugadorEnfrentamientoManager;
        [SerializeField] private Color colorVerde;
        [SerializeField] private TMP_Text TMP_nombre;
        [SerializeField] private TMP_Text TMP_conectado;
        [SerializeField] private TMP_Text TMP_tablero;
        [SerializeField] private TMP_Text TMP_colorPiezas;
        [SerializeField] private TMP_Text TMP_tiempoPensar;
        [SerializeField] private TMP_Text TMP_movimientoRecibido;
        [SerializeField] private TMP_Text TMP_movimientoEnviado;
        [SerializeField] private TMP_Text textoDebug;
        [SerializeField] private string enlaceCommitGithub;

        // Start is called before the first frame update
        void Start()
        {
            // Establecer los ajustes de la ventana
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            Application.runInBackground = true;
        }

        public void MostrarConectado()
        {
            TMP_conectado.text = "Conectado";
            TMP_conectado.color = colorVerde;
        }

        public void MostrarNombre(string nombre)
        {
            TMP_nombre.text = nombre;
        }

        public void MostrarTablero(string fen)
        {
            TMP_tablero.text = fen;
        }

        public void MostrarColorPiezas(string colorPiezas)
        {
            TMP_colorPiezas.text = colorPiezas;
        }

        public void MostrarTiempoPensar(string tiempoPensar)
        {
            TMP_tiempoPensar.text = tiempoPensar;
        }

        public void MostrarMovimientoRecibido(string movimientoRecibido)
        {
            TMP_movimientoRecibido.text = movimientoRecibido;
        }

        public void MostrarMovimientoEnviado(string movimientoEnviado)
        {
            TMP_movimientoEnviado.text = movimientoEnviado;
        }

        public void AbrirCommit()
        {
            Application.OpenURL(enlaceCommitGithub);
        }

        public void EscribirDebug(string mensaje)
        {
            textoDebug.text += mensaje;
        }
    }
}