using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rook : Piece
{
    public override void GenerateMovePlates() {
        GenerateLineMovePlates(1, 0);
        GenerateLineMovePlates(0, 1);
        GenerateLineMovePlates(-1, 0);
        GenerateLineMovePlates(0, -1);
    }
}