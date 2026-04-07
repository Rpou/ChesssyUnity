using UnityEngine;

public struct Move
{
    public Piece Piece { get; }
    public Vector2Int TargetSquare { get; }

    public int MatrixX => TargetSquare.x;
    public int MatrixY => TargetSquare.y;

    public Move(Piece piece, int matrixX, int matrixY)
        : this(piece, new Vector2Int(matrixX, matrixY))
    {
    }

    public Move(Piece piece, Vector2Int targetSquare)
    {
        Piece = piece;
        TargetSquare = targetSquare;
    }

    public Piece GetPiece()
    {
        return Piece;
    }

    public Vector2Int GetTargetSquare()
    {
        return TargetSquare;
    }

    public int GetMatrixX()
    {
        return MatrixX;
    }

    public int GetMatrixY()
    {
        return MatrixY;
    }
}
