using UnityEngine;

[CreateAssetMenu(fileName = "Card", menuName = "Cards/Card")]
public class CardSO : ScriptableObject
{
    public string id;
    public string cardName;
    public int attack;
    public int health;
    public int cost;
    [TextArea] public string description;
    public Sprite artwork;
}