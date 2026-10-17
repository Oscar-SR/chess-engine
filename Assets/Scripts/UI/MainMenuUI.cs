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
            SceneManager.LoadScene("GameSettings");
        }

        public void OptionsButton()
        {
            SceneManager.LoadScene("Options");
        }

        public void ExitButton()
        {
            Application.Quit();
        }
    }
}