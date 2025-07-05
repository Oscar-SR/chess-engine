using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Ajedrez.Systems;

namespace Ajedrez.UI
{
    public class OpcionesUI : MonoBehaviour
    {
        [SerializeField] private Slider sliderVolumenGeneral;
        [SerializeField] private Slider sliderVolumenEfectos;
        [SerializeField] private Slider sliderVolumenMusica;

        private void OnEnable()
        {
            // Cargar valores desde el AudioSystem al abrir la pantalla
            sliderVolumenGeneral.value = AudioSystem.Instancia.ObtenerVolumenGeneral();
            sliderVolumenEfectos.value = AudioSystem.Instancia.ObtenerVolumenEfectos();
            sliderVolumenMusica.value = AudioSystem.Instancia.ObtenerVolumenMusica();
        }

        public void VolverAlMenuPrincipal()
        {
            SceneManager.LoadScene("MenuPrincipal");
        }

        public void CambiarVolumenGeneral(float volumen)
        {
            AudioSystem.Instancia.EstablecerVolumenGeneral(volumen);
        }

        public void CambiarVolumenEfectos(float volumen)
        {
            AudioSystem.Instancia.EstablecerVolumenEfectos(volumen);
        }

        public void CambiarVolumenMusica(float volumen)
        {
            AudioSystem.Instancia.EstablecerVolumenMusica(volumen);
        }
    }
}