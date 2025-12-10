using UnityEngine;

public class PlayerHandVisualizer : HandVisualizerBase
{
    private GameObject selectedCardObj = null;
    private Material originalMaterial = null;
    public Color selectedColor = Color.yellow;

    private Color originalSpriteColor;
    private MaterialPropertyBlock spriteBlock;

    public override void RefreshHand()
    {
        base.RefreshHand();

        // dodaj clickable do ka¿dego obiektu (tylko dla gracza)
        foreach (var go in spawnedCards)
        {
            var clickable = go.GetComponent<CardClickable3D>();
            if (clickable == null) clickable = go.AddComponent<CardClickable3D>();
            clickable.view = go.GetComponent<CardView>();
        }
    }

    // wywo³ywane przez CardClickable3D
    public void OnCardClicked(GameObject cardObj)
    {
        if (selectedCardObj != null)
            ResetCardVisual(selectedCardObj);

        soundManager.PlaySelectCard();

        selectedCardObj = cardObj;

        SpriteRenderer spr = selectedCardObj.GetComponentInChildren<SpriteRenderer>();
        if (spr == null)
        {
            Debug.LogWarning("Card clicked, but no SpriteRenderer found!");
            return;
        }

        // zapisz oryginalny kolor, jeœli pierwszy raz
        originalSpriteColor = spr.color;

        if (spriteBlock == null)
            spriteBlock = new MaterialPropertyBlock();

        spr.GetPropertyBlock(spriteBlock);

        // „HDR” kolor — efekt œwiecenia
        // im wy¿szy mno¿nik, tym jaœniejszy efekt
        Color hdrGlow = new Color(1.2f, 1f, 0.6f, 1f) * 2.5f; // z³oty, jasnoœæ ×2.5
        spriteBlock.SetColor("_Color", hdrGlow);

        spr.SetPropertyBlock(spriteBlock);
    }

    // wywo³ywane przez SlotTarget
    public void OnSlotClicked(int slotIndex, SlotTarget slot)
    {
        if (selectedCardObj == null) return;

        var view = selectedCardObj.GetComponent<CardView>();
        if (view == null || view.model == null)
        {
            CancelSelection();
            return;
        }

        int handIndex = owner.hand.IndexOf(view.model);
        if (handIndex == -1) { CancelSelection(); return; }

        var gm = FindFirstObjectByType<CardGameLogicManager>();
        if (gm == null) { Debug.LogError("Brak CardGameLogicManager w scenie"); return; }

        var played = gm.PlayerPlayCardToSlot(handIndex, slotIndex);
        if (!played) { CancelSelection(); return; }

        PlayCardVisualByInstance(view.model, slotIndex);
        ResetCardVisual(selectedCardObj);
        selectedCardObj = null;
    }

    void CancelSelection()
    {
        if (selectedCardObj != null)
        {
            ResetCardVisual(selectedCardObj);
            selectedCardObj = null;
        }
    }

    void ResetCardVisual(GameObject cardObj)
    {
        SpriteRenderer spr = cardObj.GetComponentInChildren<SpriteRenderer>();
        if (spr == null || spriteBlock == null)
            return;

        spriteBlock.SetColor("_Color", originalSpriteColor);
        spr.SetPropertyBlock(spriteBlock);
    }
}
