using UnityEngine;

public struct Move
{
    public Piece Piece { get; }
    public Vector2Int FromSquare { get; }
    public Vector2Int TargetSquare { get; }
    public bool IsAttack { get; }

    public int FromMatrixX => FromSquare.x;
    public int FromMatrixY => FromSquare.y;
    public int MatrixX => TargetSquare.x;
    public int MatrixY => TargetSquare.y;

    public Move(Piece piece, int matrixX, int matrixY)
        : this(piece, new Vector2Int(matrixX, matrixY), false)
    {
    }

    public Move(int fromMatrixX, int fromMatrixY, int matrixX, int matrixY, bool isAttack = false)
        : this(new Vector2Int(fromMatrixX, fromMatrixY), new Vector2Int(matrixX, matrixY), isAttack)
    {
    }

    public Move(Piece piece, Vector2Int targetSquare, bool isAttack = false)
    {
        Piece = piece;
        FromSquare = piece == null
            ? default
            : new Vector2Int(piece.GetxBoard(), piece.GetyBoard());
        TargetSquare = targetSquare;
        IsAttack = isAttack;
    }

    public Move(Vector2Int fromSquare, Vector2Int targetSquare, bool isAttack = false)
    {
        Piece = null;
        FromSquare = fromSquare;
        TargetSquare = targetSquare;
        IsAttack = isAttack;
    }

    public Piece GetPiece()
    {
        return Piece;
    }

    public Vector2Int GetFromSquare()
    {
        return FromSquare;
    }

    public Vector2Int GetTargetSquare()
    {
        return TargetSquare;
    }

    public int GetFromMatrixX()
    {
        return FromMatrixX;
    }

    public int GetFromMatrixY()
    {
        return FromMatrixY;
    }

    public int GetMatrixX()
    {
        return MatrixX;
    }

    public int GetMatrixY()
    {
        return MatrixY;
    }

    public bool GetIsAttack()
    {
        return IsAttack;
    }
}
