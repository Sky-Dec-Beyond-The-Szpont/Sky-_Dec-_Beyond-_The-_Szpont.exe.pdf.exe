using UnityEngine;
using UnityEngine.UI;

public class CaseCell : MonoBehaviour
{
    [SerializeField] private Image icon;

    public CardSO Card { get; private set; }

    public void Setup(CardSO card)
    {
        Debug.Log("SETUP CELL: " + card.name);

        //if (card.artwork == null)
        //{
        //    Debug.LogError($"CARD HAS NO ARTWORK: {card.name}");
        //    icon.sprite = null;
        //    icon.color = Color.magenta; // widoczny placeholder
        //    return;
        //}

        Card = card;
        icon.sprite = TextureToSprite.Convert(card.artwork);
    }

    public static class TextureToSprite
    {
        public static Sprite Convert(Texture2D texture)
        {
            return Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f
            );
        }
    }
}
