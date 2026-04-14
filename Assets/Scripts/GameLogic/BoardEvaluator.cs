public static class BoardEvaluator
{
    public const int MaterialWeight = 100;
    public const int UnmovedPiecePenalty = 5;
    public const int CheckPenalty = 80;

    /// <summary>
    /// Scores the board in centipawns from White's perspective.
    /// Positive values favor White, negative values favor Black.
    /// </summary>
    public static int EvaluateCentipawns(GameState game)
    {
        int score = EvaluateSide(game, true) - EvaluateSide(game, false);

        if (game.IsKingInCheck(true))
        {
            score -= CheckPenalty;
        }

        if (game.IsKingInCheck(false))
        {
            score += CheckPenalty;
        }

        return score;
    }

    public static float EvaluateInPawns(GameState game)
    {
        return EvaluateCentipawns(game) / 100.0f;
    }

    private static int EvaluateSide(GameState game, bool currentPlayerIsWhite)
    {
        int score = 0;

        foreach (var piece in game.GetPieces(currentPlayerIsWhite))
        {
            if (!piece.IsActive)
            {
                continue;
            }

            score += piece.GetWorth() * MaterialWeight;

            if (!piece.HasMoved && !piece.IsKing && !piece.IsPawn)
            {
                score -= UnmovedPiecePenalty;
            }
        }

        return score;
    }
}
