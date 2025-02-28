using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class King : Piece
{
    public override void GenerateMovePlates() {
        // The king can move one square in any direction.
        for (int dx = -1; dx <= 1; dx++) {
            for (int dy = -1; dy <= 1; dy++) {
                if (dx == 0 && dy == 0)
                    continue;
                GeneratePointMovePlate(xBoard + dx, yBoard + dy);
            }
        }
    }
}