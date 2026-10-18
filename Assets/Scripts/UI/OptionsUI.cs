using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Ajedrez.Systems;

namespace Ajedrez.UI
{
    public class OptionsUI : MonoBehaviour
    {
        [SerializeField] private Slider generalVolumeSlider;
        [SerializeField] private Slider effectsVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;

        private void OnEnable()
        {
            // Load values from the AudioSystem when opening the screen
            generalVolumeSlider.value = AudioSystem.Instance.GetGeneralVolume();
            effectsVolumeSlider.value = AudioSystem.Instance.GetEffectsVolume();
            musicVolumeSlider.value = AudioSystem.Instance.GetMusicVolume();
        }

        public void BackToMainMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }

        public void ChangeGeneralVolume(float volume)
        {
            AudioSystem.Instance.SetGeneralVolume(volume);
        }

        public void ChangeEffectsVolume(float volume)
        {
            AudioSystem.Instance.SetEffectsVolume(volume);
        }

        public void ChangeMusicVolume(float volume)
        {
            AudioSystem.Instance.SetMusicVolume(volume);
        }
    }
}