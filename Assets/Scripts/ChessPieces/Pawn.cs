using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pawn : Piece
{
    public override void GenerateMovePlates() {
        Game game = controller.GetComponent<Game>();
        // Determine direction: white pawns move upward, black pawns downward.
        int direction = (player == "white") ? 1 : -1;
        int forwardX = xBoard;
        int forwardY = yBoard + direction;

        // Forward move if the square is empty.
        if (game.PositionOnBoard(forwardX, forwardY) && game.GetPosition(forwardX, forwardY) == null) {
            SpawnMovePlate(forwardX, forwardY);

            // Allow a two-square move if the pawn is on its starting row.
            if ((player == "white" && yBoard == 1) || (player == "black" && yBoard == 6)) {
                int doubleForwardY = forwardY + direction;
                if (game.PositionOnBoard(forwardX, doubleForwardY) && game.GetPosition(forwardX, doubleForwardY) == null) {
                    SpawnMovePlate(forwardX, doubleForwardY);
                }
            }
        }

        // Diagonal attacks.
        int attackXRight = xBoard + 1;
        int attackXLeft = xBoard - 1;
        int attackY = yBoard + direction;
        if (game.PositionOnBoard(attackXRight, attackY) && game.GetPosition(attackXRight, attackY) != null) {
            if (game.GetPosition(attackXRight, attackY).GetComponent<Piece>().player != player)
                SpawnAttackPlate(attackXRight, attackY);
        }
        if (game.PositionOnBoard(attackXLeft, attackY) && game.GetPosition(attackXLeft, attackY) != null) {
            if (game.GetPosition(attackXLeft, attackY).GetComponent<Piece>().player != player)
                SpawnAttackPlate(attackXLeft, attackY);
        }
    }
}