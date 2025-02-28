using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Queen : Piece
{
    public override void GenerateMovePlates() {
        // Queen moves like both a Rook and a Bishop.
        GenerateLineMovePlates(1, 0);
        GenerateLineMovePlates(0, 1);
        GenerateLineMovePlates(-1, 0);
        GenerateLineMovePlates(0, -1);
        GenerateLineMovePlates(1, 1);
        GenerateLineMovePlates(1, -1);
        GenerateLineMovePlates(-1, 1);
        GenerateLineMovePlates(-1, -1);
    }
}