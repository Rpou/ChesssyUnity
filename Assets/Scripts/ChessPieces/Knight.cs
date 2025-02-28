using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Knight : Piece
{
    public override void GenerateMovePlates() {
        int[,] moves = new int[,] {
            {1, 2}, {-1, 2}, {2, 1}, {2, -1},
            {1, -2}, {-1, -2}, {-2, 1}, {-2, -1}
        };

        for (int i = 0; i < moves.GetLength(0); i++) {
            int newX = xBoard + moves[i, 0];
            int newY = yBoard + moves[i, 1];
            GeneratePointMovePlate(newX, newY);
        }
    }
}