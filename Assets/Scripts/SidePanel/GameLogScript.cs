using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameLogScript : MonoBehaviour
{
    public GameObject moveRowPrefab;
    public RectTransform moveContentParent;

    private GameObject currentRow;
    private int turnNumber = 1;

    public void LogMove(Game game)
    {
        List<string> moves = game.GetMoves();

        if (moves.Count % 2 == 1) // White's move
        {
            currentRow = Instantiate(moveRowPrefab, moveContentParent);
            MoveRowUI rowUI = currentRow.GetComponent<MoveRowUI>();
            rowUI.SetWhiteMove(turnNumber, moves[^1]);
        }
        else // Black's move
        {
            MoveRowUI rowUI = currentRow.GetComponent<MoveRowUI>();
            rowUI.SetBlackMove(moves[^1]);
            turnNumber++;
        }

        StartCoroutine(FixLayoutNextFrame());
    }
    
    private IEnumerator FixLayoutNextFrame()
    {
        yield return null;

        LayoutRebuilder.ForceRebuildLayoutImmediate(moveContentParent);
    }
}