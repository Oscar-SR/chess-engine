using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using Ajedrez.Core;
using Ajedrez.UI;
using Ajedrez.IA;
using Ajedrez.Systems;

namespace Ajedrez.Managers
{
    public class GameSettingsManager : MonoBehaviour
    {
        [SerializeField] private GameSettingsUI previousSettingsUI;
        [SerializeField] private BoardUI boardUI;

        private Game game;
        private bool whiteOnBottom = true;
        private TextAsset openingsBook;

        // Start is called before the first frame update
        void Start()
        {
            openingsBook = Resources.Load<TextAsset>("Aperturas");
            game = new Game(new Board(), new HumanPlayer("Player 1", Piece.Color.White), new HumanPlayer("Player 2", Piece.Color.Black), new Timer(60f, 0f));
            boardUI.Init(game.Board);
        }

        public void ChangeBoardPosition(string fen)
        {
            try
            {
                if (string.IsNullOrEmpty(fen))
                {
                    game.Board = new Board();
                }
                else
                {
                    game.Board = new Board(fen);
                }

                // Update AI players' board representation if any
                if (game.WhitePlayer is AIPlayer whiteAIPlayer)
                {
                    whiteAIPlayer.Board = game.Board;
                }
                if (game.BlackPlayer is AIPlayer blackAIPlayer)
                {
                    blackAIPlayer.Board = game.Board;
                }

                previousSettingsUI.ClearError();
            }
            catch (ArgumentException /*ex*/)
            {
                // Debug.LogError(ex.Message);
                game.Board = null;
                previousSettingsUI.ShowError("Invalid board FEN position.");
            }

            // Update UI
            boardUI.ChangePosition(game.Board);
        }

        public void LoadHumanPlayer1()
        {
            Piece.Color color = whiteOnBottom ? Piece.Color.White : Piece.Color.Black;
            HumanPlayer player = new HumanPlayer("Player 1", color);

            if (color == Piece.Color.White)
            {
                game.WhitePlayer = player;
            }
            else
            {
                game.BlackPlayer = player;
            }
        }

        public void LoadAIPlayer1()
        {
            Piece.Color color = whiteOnBottom ? Piece.Color.White : Piece.Color.Black;
            AIPlayer player = new AIPlayer(color, game.Board, game.Timer, AIConfiguration.CreateEasy(openingsBook));

            if (color == Piece.Color.White)
            {
                game.WhitePlayer = player;
            }
            else
            {
                game.BlackPlayer = player;
            }
        }

        public void LoadHumanPlayer2()
        {
            Piece.Color color = whiteOnBottom ? Piece.Color.Black : Piece.Color.White;
            HumanPlayer player = new HumanPlayer("Player 2", color);

            if (color == Piece.Color.White)
            {
                game.WhitePlayer = player;
            }
            else
            {
                game.BlackPlayer = player;
            }
        }

        public void LoadAIPlayer2()
        {
            Piece.Color color = whiteOnBottom ? Piece.Color.Black : Piece.Color.White;
            AIPlayer player = new AIPlayer(color, game.Board, game.Timer, AIConfiguration.CreateEasy(openingsBook));

            if (color == Piece.Color.White)
            {
                game.WhitePlayer = player;
            }
            else
            {
                game.BlackPlayer = player;
            }
        }

        public void SwapColors()
        {
            boardUI.ChangePerspective(game.Board);
            whiteOnBottom = !whiteOnBottom;
            game.SwapPlayerColors();
        }

        public void ChangeDuration(float duration)
        {
            game.Timer = new Timer(duration, game.Timer.IncrementPerMove);
        }

        public void ChangeIncrement(float increment)
        {
            game.Timer = new Timer(game.Timer.InitialDuration, increment);
        }

        public void ChangePlayer1Name(string name)
        {
            if (whiteOnBottom)
            {
                game.WhitePlayer.Name = name;
            }
            else
            {
                game.BlackPlayer.Name = name;
            }
        }

        public void ChangePlayer2Name(string name)
        {
            if (whiteOnBottom)
            {
                game.BlackPlayer.Name = name;
            }
            else
            {
                game.WhitePlayer.Name = name;
            }
        }

        private AIConfiguration GetConfiguration(AIConfiguration.DifficultyType difficulty)
        {
            switch (difficulty)
            {
                case AIConfiguration.DifficultyType.Easy:
                    return AIConfiguration.CreateEasy(openingsBook);
                case AIConfiguration.DifficultyType.Medium:
                    return AIConfiguration.CreateMedium(openingsBook);
                case AIConfiguration.DifficultyType.Maximum:
                    return AIConfiguration.CreateMaximum(openingsBook);
                default:
                    return AIConfiguration.CreateCustom(Search.SearchType.ByTime, AIConfiguration.DYNAMIC_TIME, false, 0, null);
            }
        }

        public void ChangePlayer1Difficulty(AIConfiguration.DifficultyType difficulty)
        {
            if (whiteOnBottom)
            {
                ((AIPlayer)game.WhitePlayer).AIConfiguration = GetConfiguration(difficulty);
            }
            else
            {
                ((AIPlayer)game.BlackPlayer).AIConfiguration = GetConfiguration(difficulty);
            }
        }

        public void ChangePlayer2Difficulty(AIConfiguration.DifficultyType difficulty)
        {
            if (whiteOnBottom)
            {
                ((AIPlayer)game.BlackPlayer).AIConfiguration = GetConfiguration(difficulty);
            }
            else
            {
                ((AIPlayer)game.WhitePlayer).AIConfiguration = GetConfiguration(difficulty);
            }
        }

        public void StartGame()
        {
            PersistenceSystem.Instance.game = game;
            PersistenceSystem.Instance.whiteOnBottom = whiteOnBottom;
            SceneManager.LoadScene("Partida");
        }

        public void ReturnToMainMenu()
        {
            SceneManager.LoadScene("MenuPrincipal");
        }
    }
}