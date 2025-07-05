using UnityEngine;
using TMPro;
using Ajedrez.UI;

namespace Ajedrez.Debugging
{
    public class EstadoDebugger : MonoBehaviour
    {
        public static EstadoDebugger Instancia { get; private set; }
        [SerializeField] private TextMeshProUGUI textoDebugEstadoTablero;
        private bool visualizar = false;

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
                InputManager.Instancia.OnToggleEstadoTablero += ToogleDebugEstadoTablero;
            }
        }

        private void OnDisable()
        {
            if (InputManager.Instancia != null)
            {
                InputManager.Instancia.OnToggleEstadoTablero -= ToogleDebugEstadoTablero;
            }
        }

        // Update is called once per frame
        void Update()
        {

        }

        private void ToogleDebugEstadoTablero()
        {
            visualizar = !visualizar;
            textoDebugEstadoTablero.enabled = visualizar;
        }

        public void LogEstadoTablero(string log)
        {
            textoDebugEstadoTablero.text = log;
        }
    }
}