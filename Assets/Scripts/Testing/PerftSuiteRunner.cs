using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Chess.Core;

namespace Chess.Testing
{
    public static class PerftSuiteRunner
    {
        private struct PerftTestCase
        {
            public int Depth;
            public int ExpectedNodes;
            public string FEN;
        }

        public static void RunSuite(string suiteText, CancellationToken token, Action<string> write)
        {
            string suiteName = ParseSuiteName(suiteText);
            List<PerftTestCase> testCases = ParseSuite(suiteText);

            if (testCases.Count == 0)
            {
                write("No tests found in the suite.\n");
                return;
            }

            write($"══ {suiteName} ══\n\n");

            int passed = 0;
            int total = testCases.Count;

            for (int i = 0; i < total; i++)
            {
                if (token.IsCancellationRequested)
                {
                    write("<color=#B3472B>Execution cancelled</color>\n");
                    return;
                }

                PerftTestCase test = testCases[i];
                Board board = new Board(test.FEN);

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                int result = Perft.Run(test.Depth, board, token);
                stopwatch.Stop();

                if (token.IsCancellationRequested)
                {
                    write("<color=#B3472B>Execution cancelled</color>\n");
                    return;
                }

                bool pass = result == test.ExpectedNodes;
                if (pass) passed++;

                string shortFen = test.FEN.Length > 36 ? test.FEN.Substring(0, 36) + "..." : test.FEN;
                string status = pass
                    ? "<color=#79AD51>PASS</color>"
                    : "<color=#B3472B>FAIL</color>";
                string detail = pass
                    ? ""
                    : $"  <color=#B3472B>(obtained: {result:N0})</color>";

                write(
                    $"<color=#808080>[{i + 1}/{total}]</color> {shortFen}  " +
                    $"<color=#808080>depth:</color><color=#79AD51>{test.Depth}</color>  " +
                    $"<color=#808080>expected:</color><color=#6BCDAE>{test.ExpectedNodes:N0}</color>  " +
                    $"{status}{detail}  " +
                    $"<color=#808080>{stopwatch.ElapsedMilliseconds:N0}ms</color>\n"
                );
            }

            string summaryColor = passed == total ? "#79AD51" : "#B3472B";
            write($"\n<color={summaryColor}>══ Result: {passed}/{total} tests passed ══</color>\n");
        }

        private static string ParseSuiteName(string suiteText)
        {
            string[] lines = suiteText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return lines.Length > 0 ? lines[0].Trim() : "Suite";
        }

        private static List<PerftTestCase> ParseSuite(string suiteText)
        {
            List<PerftTestCase> testCases = new List<PerftTestCase>();
            string[] lines = suiteText.Split(new[] { '\n' });

            bool insideBlock = false;

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();

                if (line == "{")
                {
                    insideBlock = true;
                    continue;
                }

                if (line == "}")
                {
                    break;
                }

                if (!insideBlock || string.IsNullOrEmpty(line))
                    continue;

                // Format: depth,expectedNodes,fen
                int firstComma = line.IndexOf(',');
                if (firstComma < 0) continue;

                int secondComma = line.IndexOf(',', firstComma + 1);
                if (secondComma < 0) continue;

                string depthStr = line.Substring(0, firstComma).Trim();
                string nodesStr = line.Substring(firstComma + 1, secondComma - firstComma - 1).Trim();
                string fen = line.Substring(secondComma + 1).Trim();

                if (int.TryParse(depthStr, out int depth) && int.TryParse(nodesStr, out int expectedNodes))
                {
                    testCases.Add(new PerftTestCase
                    {
                        Depth = depth,
                        ExpectedNodes = expectedNodes,
                        FEN = fen
                    });
                }
            }

            return testCases;
        }
    }
}
