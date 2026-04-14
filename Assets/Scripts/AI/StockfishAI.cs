using System;
using System.Diagnostics;
using System.IO;
using GameLogic;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class StockfishAI : AI
{
    private const int MinimumElo = 1320;
    private const int MaximumElo = 3190;

    [SerializeField] private string enginePath = "/home/rasmus/Desktop/Code/HomeProjects/chessBots/MinMax3Basic/engines/stockfish/stockfish";
    [SerializeField] private string engineArguments = "";
    [SerializeField, Min(1)] private int threads = 1;
    [SerializeField, Min(1)] private int hashMb = 16;
    [SerializeField, Range(0, 20)] private int skillLevel = 20;
    [SerializeField] private bool limitStrength;
    [SerializeField, Range(MinimumElo, MaximumElo)] private int elo = MinimumElo;
    [SerializeField, Min(0)] private int searchDepth;
    [SerializeField, Min(0)] private int searchNodes = 200;
    [SerializeField, Min(1)] private int moveTimeMs = 1;

    private Process _engineProcess;
    private StreamWriter _engineInput;
    private StreamReader _engineOutput;
    private bool _warnedAboutUnderpromotion;

    public override void MakeMove(Game game)
    {
        if (game == null || game.IsGameOver())
        {
            return;
        }

        try
        {
            EnsureEngineStarted();

            SendCommand($"position fen {FenSerializer.Serialize(game)}");
            SendCommand(BuildGoCommand());

            var bestMove = ReadBestMove();
            if (string.IsNullOrWhiteSpace(bestMove) || bestMove == "(none)" || bestMove == "0000")
            {
                return;
            }

            if (!TryParseUciMove(bestMove, out var fromX, out var fromY, out var toX, out var toY, out var promotion))
            {
                Debug.LogError($"Unable to parse Stockfish move '{bestMove}'.");
                return;
            }

            if (promotion != '\0' && promotion != 'q' && !_warnedAboutUnderpromotion)
            {
                Debug.LogWarning("Stockfish requested an underpromotion. This project currently auto-promotes to queens.");
                _warnedAboutUnderpromotion = true;
            }

            var pieceObject = game.GetPosition(fromX, fromY);
            if (pieceObject == null)
            {
                Debug.LogError($"Stockfish tried to move from an empty square: {bestMove}.");
                return;
            }

            var move = new Move(fromX, fromY, toX, toY);
            game.MakeNextMove(pieceObject, toX, toY, game.IsAttackMove(move));
        }
        catch (Exception ex)
        {
            Debug.LogError($"StockfishAI failed: {ex.Message}");
            ShutdownEngine();
        }
    }

    private void OnDisable()
    {
        ShutdownEngine();
    }

    private void OnDestroy()
    {
        ShutdownEngine();
    }

    private void EnsureEngineStarted()
    {
        if (_engineProcess != null && !_engineProcess.HasExited)
        {
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = string.IsNullOrWhiteSpace(enginePath) ? "stockfish" : enginePath,
            Arguments = engineArguments,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        _engineProcess = new Process
        {
            StartInfo = startInfo
        };

        _engineProcess.Start();
        _engineInput = _engineProcess.StandardInput;
        _engineOutput = _engineProcess.StandardOutput;
        _engineInput.AutoFlush = true;

        SendCommand("uci");
        ReadUntil("uciok");

        SendCommand($"setoption name Threads value {Mathf.Max(1, threads)}");
        SendCommand($"setoption name Hash value {Mathf.Max(1, hashMb)}");
        ApplyStrengthOptions();
        SendCommand("isready");
        ReadUntil("readyok");
    }

    private void ShutdownEngine()
    {
        try
        {
            if (_engineProcess == null)
            {
                return;
            }

            if (!_engineProcess.HasExited)
            {
                try
                {
                    SendCommand("quit");
                }
                catch
                {
                    // Ignore write failures while shutting down.
                }

                if (!_engineProcess.WaitForExit(100))
                {
                    _engineProcess.Kill();
                }
            }
        }
        catch
        {
            // Ignore shutdown errors from already closed processes.
        }
        finally
        {
            _engineInput = null;
            _engineOutput = null;

            if (_engineProcess != null)
            {
                _engineProcess.Dispose();
                _engineProcess = null;
            }
        }
    }

    private void SendCommand(string command)
    {
        _engineInput.WriteLine(command);
    }

    private void ReadUntil(string expectedLine)
    {
        while (true)
        {
            var line = _engineOutput.ReadLine();
            if (line == null)
            {
                throw new IOException($"Stockfish closed before returning '{expectedLine}'.");
            }

            if (line == expectedLine)
            {
                return;
            }
        }
    }

    private string ReadBestMove()
    {
        while (true)
        {
            var line = _engineOutput.ReadLine();
            if (line == null)
            {
                throw new IOException("Stockfish closed before returning a best move.");
            }

            if (!line.StartsWith("bestmove ", StringComparison.Ordinal))
            {
                continue;
            }

            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 1 ? parts[1] : string.Empty;
        }
    }

    private string BuildGoCommand()
    {
        if (searchNodes > 0)
        {
            return $"go nodes {Mathf.Max(1, searchNodes)}";
        }

        if (searchDepth > 0)
        {
            return $"go depth {Mathf.Max(1, searchDepth)}";
        }

        return $"go movetime {Mathf.Max(1, moveTimeMs)}";
    }

    private void ApplyStrengthOptions()
    {
        SendCommand($"setoption name UCI_LimitStrength value {ToUciBoolean(limitStrength)}");

        if (limitStrength)
        {
            SendCommand($"setoption name UCI_Elo value {Mathf.Clamp(elo, MinimumElo, MaximumElo)}");
            return;
        }

        SendCommand($"setoption name Skill Level value {Mathf.Clamp(skillLevel, 0, 20)}");
    }

    private static bool TryParseUciMove(string moveText, out int fromX, out int fromY,
        out int toX, out int toY, out char promotion)
    {
        fromX = -1;
        fromY = -1;
        toX = -1;
        toY = -1;
        promotion = '\0';

        if (string.IsNullOrWhiteSpace(moveText) || moveText.Length < 4)
        {
            return false;
        }

        if (!TryParseSquare(moveText[0], moveText[1], out fromX, out fromY) ||
            !TryParseSquare(moveText[2], moveText[3], out toX, out toY))
        {
            return false;
        }

        if (moveText.Length >= 5)
        {
            promotion = char.ToLowerInvariant(moveText[4]);
        }

        return true;
    }

    private static bool TryParseSquare(char file, char rank, out int x, out int y)
    {
        x = char.ToLowerInvariant(file) - 'a';
        y = rank - '1';

        return x >= 0 && x < 8 && y >= 0 && y < 8;
    }

    private static string ToUciBoolean(bool value)
    {
        return value ? "true" : "false";
    }
}
