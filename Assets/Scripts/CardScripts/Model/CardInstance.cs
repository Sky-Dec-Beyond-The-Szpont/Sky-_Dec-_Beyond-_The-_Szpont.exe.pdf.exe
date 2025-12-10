using System;

[Serializable]
public class CardInstance
{
    public CardSO card;
    public int currentHealth;

    public CardInstance(CardSO d)
    {
        card = d;
        currentHealth = d.health;
    }

    public int Attack => card.attack;
    public int Cost => card.cost;
    public string Name => card.cardName;
    public string Description => card.description;
}