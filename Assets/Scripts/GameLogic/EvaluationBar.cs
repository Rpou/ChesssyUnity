using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class EvaluationBar : MonoBehaviour
{
    [SerializeField] private Slider slider;
    public TextMeshProUGUI EvaluationText;

    public void SetEvaluation(Game game)
    {
        GameState gamest = new GameState(game);

        // Clamp so it stays inside the bar range
        var eval = Mathf.Clamp(BoardEvaluator.EvaluateInPawns(gamest), slider.minValue, slider.maxValue);
        slider.value = eval;
        EvaluationText.text = eval.ToString("0.0");
    }
}
