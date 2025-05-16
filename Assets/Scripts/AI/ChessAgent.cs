using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using GameLogic;  // your namespace

public class ChessAgent : Agent
{
    [Header("References")]
    public Game gameController;   // ← your Game.cs component :contentReference[oaicite:0]{index=0}:contentReference[oaicite:1]{index=1}
    public bool isWhite;          // which side this agent plays

    // fixed‐size action space of all 4 544 UCI moves
    private Move[] allPossibleMoves;

    public override void Initialize()
    {
        // build the global vocabulary once
        allPossibleMoves = MoveGenerator.GenerateAllMoves();
    }

    public override void OnEpisodeBegin()
    {
        // restart your scene & rebind the controller reference
        gameController = GameObject.FindGameObjectWithTag("GameController")
                                   .GetComponent<Game>();
        gameController.ResetGame();
        isWhite = true; // reset to whichever side starts
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // observe the board as a 8×8 grid of 0–12 ints, normalized by 12
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                var obj = gameController.GetPosition(x, y);
                int id = 0;
                if (obj != null)
                {
                    var piece = obj.GetComponent<Piece>();
                    // map Piece + color → 1..12
                    string p = piece.GetPlayer();
                    if      (piece is Pawn)   id = (p=="white"?1:7);
                    else if (piece is Knight) id = (p=="white"?2:8);
                    else if (piece is Bishop) id = (p=="white"?3:9);
                    else if (piece is Rook)   id = (p=="white"?4:10);
                    else if (piece is Queen)  id = (p=="white"?5:11);
                    else if (piece is King)   id = (p=="white"?6:12);
                }
                sensor.AddObservation(id / 12f);
            }
        }
        // which side to move
        sensor.AddObservation(isWhite ? 1f : 0f);
    }

    // remove any of the 4544 moves that aren’t legal right now
    public override void WriteDiscreteActionMask(IDiscreteActionMask mask)
    {
        string me = isWhite ? "white" : "black";
        for (int i = 0; i < allPossibleMoves.Length; i++)
        {
            var m = allPossibleMoves[i];
            // 1) must have your piece on the from‐square
            var fromObj = gameController.GetPosition(m.FromFile, m.FromRank);
            if (fromObj == null ||
                fromObj.GetComponent<Piece>().GetPlayer() != me)
            {
                mask.SetActionEnabled(0, i, false);
                continue;
            }
            // 2) that piece must list (toX,toY) as a legal move
            var piece = fromObj.GetComponent<Piece>();
            var (move, attack) = piece.GetAllLegalMoves();  // :contentReference[oaicite:2]{index=2}:contentReference[oaicite:3]{index=3}
            bool ok = move.Exists(v => v.x == m.ToFile && v.y == m.ToRank)
                      || attack.Exists(v => v.x == m.ToFile && v.y == m.ToRank);
            if (!ok) mask.SetActionEnabled(0, i, false);
        }
    }
    public override void OnActionReceived(ActionBuffers actions)
    {
        int idx = actions.DiscreteActions[0];
        var m = allPossibleMoves[idx];

        // apply the move by hand‐coding what your MovePlate.OnMouseUp does
        gameController.ExecuteMove(m); 

        // terminal reward on game end
        if (gameController.IsGameOver())
        {
            var result = gameController.GetResult(); // e.g. +1/−1/0
            AddReward(result);
            EndEpisode();
        }
    }

    /*public override void Heuristic(in ActionBuffers actionsOut)
    {
        // optional: map your UI input to the action index
        actionsOut.DiscreteActions[0] = gameController.GetHumanSelectedMoveIndex();
    }*/
}
