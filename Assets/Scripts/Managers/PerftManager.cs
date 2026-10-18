using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Chess.Core;
using Chess.Utilities;
using Chess.Testing;

namespace Chess.Managers
{
    public class PerftManager : MonoBehaviour
    {
        private const string RED = "#B3472B";
        private const string GREEN = "#79AD51";
        private const string CYAN = "#6BCDAE";
        private const string GRAY = "#808080";
        
        [SerializeField] private TextMeshProUGUI perftText;
        [SerializeField] private TMP_InputField fenInput;
        [SerializeField] private TMP_InputField depthLimitInput;
        [SerializeField] private Button perftButton;
        [SerializeField] private Button divideButton;
        [SerializeField] private Button stopExecutionButton;
        [SerializeField] private RectTransform scrollViewContent;
        [SerializeField] private TextAsset suite;

        private Board board;
        private CancellationTokenSource cancellationTokenSource;
        private CancellationToken cancellationToken;
        private SynchronizationContext unityContext;

        void Start()
        {
            // Capture the main thread context
            unityContext = SynchronizationContext.Current;

            // Warm-up
            Perft.Run(1, new Board(), CancellationToken.None);
        }

        public void ExecuteTask(string task)
        {
            DisableExecutionButtons();

            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }

            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;

            string fen = string.IsNullOrEmpty(fenInput.text)
                ? fenInput.placeholder.GetComponent<TMP_Text>().text
                : fenInput.text;

            int depthLimit = (int.TryParse(depthLimitInput.text, out int result) && result > 0) ? result : int.MaxValue;

            switch(task)
            {
                case "PerftTask":
                {
                    board = new Board(fen);
                    Task.Run(() => PerftTask(cancellationToken, depthLimit));
                    break;
                }

                case "DivideTask":
                {
                    board = new Board(fen);
                    Task.Run(() => DivideTask(cancellationToken, depthLimit));
                    break;
                }

                case "SuiteTask":
                {
                    string suiteText = suite != null ? suite.text : null;
                    Task.Run(() => SuiteTask(cancellationToken, suiteText));
                    break;
                }

                default:
                {
                    UnityEngine.Debug.LogError("Task not found");
                    EnableExecutionButtons();
                    break;
                }
            }
        }

        public void StopExecution()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }

            EnableExecutionButtons();
        }

        private void DisableExecutionButtons()
        {
            perftButton.interactable = false;
            divideButton.interactable = false;
            stopExecutionButton.interactable = true;
        }

        private void EnableExecutionButtons()
        {
            perftButton.interactable = true;
            divideButton.interactable = true;
            stopExecutionButton.interactable = false;
        }

        private void ClearPerftText()
        {
            unityContext.Post(_ =>
            {
                perftText.text = "";
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollViewContent);
            }, null);
        }

        private void WritePerftText(string text)
        {
            // Update the UI on the main thread
            unityContext.Post(_ =>
            {
                perftText.text += text;
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollViewContent);
            }, null);
        }

        private async Task PerftTask(CancellationToken token, int depthLimit)
        {
            int depth = 1;
            ClearPerftText();

            while ((depth <= depthLimit) && !token.IsCancellationRequested)
            {
                Stopwatch stopwatch = new Stopwatch();

                stopwatch.Start();
                int nodeCount = Perft.Run(depth, board, cancellationToken);
                stopwatch.Stop();

                WritePerftText(
                    $"Depth: <color={GREEN}>{depth}</color> <color={GRAY}>ply</color>  " +
                    $"Search: <color={RED}>{nodeCount:N0}</color> <color={GRAY}>positions</color>  " +
                    $"Time: <color={CYAN}>{stopwatch.ElapsedMilliseconds:N0}</color> <color={GRAY}>milliseconds</color>\n"
                );

                if (token.IsCancellationRequested)
                {
                    WritePerftText($"<color={RED}>Execution cancelled</color>");
                    return;
                }

                depth++;
                await Task.Yield();
            }

            WritePerftText($"<color={GREEN}>Test finished</color>");
            unityContext.Post(_ => EnableExecutionButtons(), null); // Enable from the main thread
        }

        private async Task DivideTask(CancellationToken token, int depthLimit)
        {
            (List<Move> moves, _) = board.GenerateLegalMoves();
            int totalNodes = 0;
            ClearPerftText();

            foreach (Move move in moves)
            {
                board.MakeMove(move);
                int nodes = Perft.Run(depthLimit - 1, board, cancellationToken);
                board.UndoMove(move);

                WritePerftText($"{move.ToLAN()}: <color={CYAN}>{nodes}</color>\n");

                if (token.IsCancellationRequested)
                {
                    WritePerftText($"<color={RED}>Execution cancelled</color>");
                    return;
                }

                totalNodes += nodes;
                await Task.Yield();
            }

            WritePerftText($"\nTotal nodes: <color={CYAN}>{totalNodes}</color>\n<color={GREEN}>Test finished</color>");
            unityContext.Post(_ => EnableExecutionButtons(), null); // Enable from the main thread
        }

        private Task SuiteTask(CancellationToken token, string suiteText)
        {
            ClearPerftText();

            if (suiteText == null)
            {
                WritePerftText($"<color={RED}>No suite assigned</color>");
                unityContext.Post(_ => EnableExecutionButtons(), null);
                return Task.CompletedTask;
            }

            PerftSuiteRunner.RunSuite(suiteText, token, WritePerftText);

            unityContext.Post(_ => EnableExecutionButtons(), null);
            return Task.CompletedTask;
        }
    }
}