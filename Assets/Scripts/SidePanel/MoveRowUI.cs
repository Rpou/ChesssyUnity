using TMPro;
using UnityEngine;

public class MoveRowUI : MonoBehaviour
{
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI whiteMoveText;
    public TextMeshProUGUI blackMoveText;

    public void SetWhiteMove(int turn, string move)
    {
        turnText.text = turn + ".";
        whiteMoveText.text = move;
    }

    public void SetBlackMove(string move)
    {
        blackMoveText.text = move;
    }
}