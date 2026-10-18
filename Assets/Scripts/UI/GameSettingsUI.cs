using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;
using Ajedrez.Core;
using Ajedrez.Managers;

namespace Ajedrez.UI
{
    public class GameSettingsUI : MonoBehaviour
    {
        public enum GameDuration
        {
            OneMinute = 1,
            ThreeMinutes = 3,
            FiveMinutes = 5,
            TenMinutes = 10,
            FifteenMinutes = 15,
            ThirtyMinutes = 30,
            SixtyMinutes = 60
        }

        public enum Increment
        {
            None = 0,
            OneSecond = 1,
            TwoSeconds = 2,
            ThreeSeconds = 3,
            FiveSeconds = 5,
            TenSeconds = 10,
            ThirtySeconds = 30
        }

        [SerializeField] private TMP_Dropdown durationDropdown;
        [SerializeField] private TMP_Dropdown incrementDropdown;
        [SerializeField] private TMP_Dropdown difficultyDropdown1;
        [SerializeField] private TMP_Dropdown difficultyDropdown2;
        [SerializeField] private TMP_Dropdown playerDropdown1;
        [SerializeField] private TMP_Dropdown playerDropdown2;
        [SerializeField] private TMP_InputField player1NameInput;
        [SerializeField] private TMP_InputField player2NameInput;
        [SerializeField] private TMP_InputField fenPositionInput;
        [SerializeField] private TMP_Text errorText;
        [SerializeField] private TMP_Text player1Text;
        [SerializeField] private TMP_Text player2Text;
        [SerializeField] private Button startButton;
        [SerializeField] private GameSettingsManager gameSettingsManager;

        // Start is called before the first frame update
        void Start()
        {
            playerDropdown1.onValueChanged.AddListener(OnPlayer1SelectorChanged);
            playerDropdown2.onValueChanged.AddListener(OnPlayer2SelectorChanged);
            durationDropdown.onValueChanged.AddListener(OnDurationChanged);
            incrementDropdown.onValueChanged.AddListener(OnIncrementChanged);
            fenPositionInput.onValueChanged.AddListener(OnFenChanged);
            player1NameInput.onEndEdit.AddListener(OnPlayer1NameEndEdit);
            player2NameInput.onEndEdit.AddListener(OnPlayer2NameEndEdit);
            difficultyDropdown1.onValueChanged.AddListener(OnPlayer1DifficultyChanged);
            difficultyDropdown2.onValueChanged.AddListener(OnPlayer2DifficultyChanged);
            LoadDropdowns();
        }

        private void OnPlayer1SelectorChanged(int index)
        {
            Player.Type selectedType = (Player.Type)index;

            if (selectedType == Player.Type.Human)
            {
                player1NameInput.gameObject.SetActive(true);
                difficultyDropdown1.gameObject.SetActive(false);
                gameSettingsManager.LoadHumanPlayer1();
            }
            else
            {
                difficultyDropdown1.gameObject.SetActive(true);
                player1NameInput.gameObject.SetActive(false);
                gameSettingsManager.LoadAIPlayer1();
            }
        }

        private void OnPlayer2SelectorChanged(int index)
        {
            Player.Type selectedType = (Player.Type)index;

            if (selectedType == Player.Type.Human)
            {
                player2NameInput.gameObject.SetActive(true);
                difficultyDropdown2.gameObject.SetActive(false);
                gameSettingsManager.LoadHumanPlayer2();
            }
            else
            {
                difficultyDropdown2.gameObject.SetActive(true);
                player2NameInput.gameObject.SetActive(false);
                gameSettingsManager.LoadAIPlayer2();
            }
        }

        private void OnFenChanged(string fen)
        {
            gameSettingsManager.ChangeBoardPosition(fen);
        }

        public void ShowError(string message)
        {
            errorText.text = message;
            startButton.interactable = false;
        }

        public void ClearError()
        {
            errorText.text = "";
            startButton.interactable = true;
        }

        private void LoadDropdowns()
        {
            durationDropdown.ClearOptions();
            incrementDropdown.ClearOptions();
            difficultyDropdown1.ClearOptions();
            difficultyDropdown2.ClearOptions();
            playerDropdown1.ClearOptions();
            playerDropdown2.ClearOptions();

            // Build the list of readable duration options
            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
            foreach (GameDuration duration in Enum.GetValues(typeof(GameDuration)))
            {
                options.Add(new TMP_Dropdown.OptionData(((int)duration).ToString()));
            }
            durationDropdown.AddOptions(options);

            options.Clear();
            foreach (Increment increment in Enum.GetValues(typeof(Increment)))
            {
                options.Add(new TMP_Dropdown.OptionData(((int)increment).ToString()));
            }
            incrementDropdown.AddOptions(options);

            options.Clear();
            foreach (AIConfiguration.DifficultyType difficulty in Enum.GetValues(typeof(AIConfiguration.DifficultyType)))
            {
                options.Add(new TMP_Dropdown.OptionData(difficulty.ToString()));
            }
            difficultyDropdown1.AddOptions(options);
            difficultyDropdown2.AddOptions(options);

            options.Clear();
            foreach (Player.Type player in Enum.GetValues(typeof(Player.Type)))
            {
                options.Add(new TMP_Dropdown.OptionData(player.ToString()));
            }
            playerDropdown1.AddOptions(options);
            playerDropdown2.AddOptions(options);
        }

        private void SwapColors()
        {
            string temporary = player1Text.text;
            player1Text.text = player2Text.text;
            player2Text.text = temporary;
            gameSettingsManager.SwapColors();
        }

        private void OnDurationChanged(int index)
        {
            // The dropdown text holds the number of minutes
            string selectedText = durationDropdown.options[index].text;

            // Convert minutes to seconds
            if (int.TryParse(selectedText, out int minutes))
            {
                gameSettingsManager.ChangeDuration(minutes * 60f);
            }
        }

        private void OnIncrementChanged(int index)
        {
            // The dropdown text holds the number of seconds
            string selectedText = incrementDropdown.options[index].text;

            if (int.TryParse(selectedText, out int seconds))
            {
                gameSettingsManager.ChangeIncrement(seconds);
            }
        }

        private void OnPlayer1NameEndEdit(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                gameSettingsManager.ChangePlayer1Name(name);
            }
        }

        private void OnPlayer2NameEndEdit(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                gameSettingsManager.ChangePlayer2Name(name);
            }
        }

        private void OnPlayer1DifficultyChanged(int index)
        {
            AIConfiguration.DifficultyType difficulty = (AIConfiguration.DifficultyType)index;
            gameSettingsManager.ChangePlayer1Difficulty(difficulty);
        }

        private void OnPlayer2DifficultyChanged(int index)
        {
            AIConfiguration.DifficultyType difficulty = (AIConfiguration.DifficultyType)index;
            gameSettingsManager.ChangePlayer2Difficulty(difficulty);
        }

        public void StartButton()
        {
            gameSettingsManager.StartGame();
        }

        public void BackButton()
        {
            gameSettingsManager.ReturnToMainMenu();
        }
    }
}