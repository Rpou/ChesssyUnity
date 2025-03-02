using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SpriteManager", menuName = "Chess/SpriteManager")]
public class SpriteManager : ScriptableObject
{
    public Sprite black_queen, black_king, black_knight, black_bishop, black_rook, black_pawn;
    public Sprite white_queen, white_king, white_knight, white_bishop, white_rook, white_pawn;

    private Dictionary<string, Sprite> spriteDict;

    private void OnEnable()
    {
        spriteDict = new Dictionary<string, Sprite>
        {
            { "black_queen", black_queen }, { "black_king", black_king }, { "black_knight", black_knight },
            { "black_bishop", black_bishop }, { "black_rook", black_rook }, { "black_pawn", black_pawn },
            { "white_queen", white_queen }, { "white_king", white_king }, { "white_knight", white_knight },
            { "white_bishop", white_bishop }, { "white_rook", white_rook }, { "white_pawn", white_pawn }
        };
    }

    public Sprite GetSprite(string pieceName)
    {
        return spriteDict.ContainsKey(pieceName) ? spriteDict[pieceName] : null;
    }
}