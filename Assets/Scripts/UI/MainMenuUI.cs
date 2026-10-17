using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ajedrez.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        public void PlayButton()
        {
            SceneManager.LoadScene("ConfiguracionesPrevias");
        }

        public void OptionsButton()
        {
            SceneManager.LoadScene("Opciones");
        }

        public void ExitButton()
        {
            Application.Quit();
        }
    }
}