using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ajedrez.UI
{
    public class MenuPrincipalUI : MonoBehaviour
    {
        public void BotonJugar()
        {
            SceneManager.LoadScene("ConfiguracionesPrevias");
        }

        public void BotonOpciones()
        {
            SceneManager.LoadScene("Opciones");
        }

        public void BotonSalir()
        {
            Application.Quit();
        }
    }
}