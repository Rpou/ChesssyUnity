using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class EvaluationBar : MonoBehaviour
{
    [SerializeField] private Slider slider;
    public TextMeshProUGUI EvaluationText;
    private const int MaterialWeight = 100;
    private const int UnmovedPiecePenalty = 5;

    public void SetEvaluation(Game game)
    {
        GameState gamest = new GameState(game);

        // Clamp so it stays inside the bar range
        var eval = Mathf.Clamp(Utility(gamest), slider.minValue, slider.maxValue);
        slider.value = eval;
        EvaluationText.text = eval.ToString("0.00");
    }

    private float Utility(GameState game)
    {
        float score = 0;
        // white pieces
        foreach (var piece in game.GetPieces(true))
        {
            if (piece.IsActive)
            {
                score += piece.GetWorth() * MaterialWeight;
                if (!piece.HasMoved && (!piece.IsKing || !piece.IsPawn)) score -= UnmovedPiecePenalty;
                if (piece.IsInCheck) score -= 80;
            }

        }

        // black pieces.
        foreach (var piece in game.GetPieces(false))
        {
            if (piece.IsActive)
            {
                score -= piece.GetWorth() * MaterialWeight;
                if (!piece.HasMoved && (!piece.IsKing || !piece.IsPawn)) score += UnmovedPiecePenalty;
                if (piece.IsInCheck) score += 80;
            }
        }

        //int whiteMobility = game.GetPossibleMoveCount("white");
        //int blackMobility = game.GetPossibleMoveCount("black");
        //score += (int)((whiteMobility - blackMobility) * 0.2f);

        return score / 100.0f;
    }
}
