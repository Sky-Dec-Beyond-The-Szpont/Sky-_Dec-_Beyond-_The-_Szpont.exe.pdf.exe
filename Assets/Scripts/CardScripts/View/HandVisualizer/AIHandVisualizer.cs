using UnityEngine;

public class AIHandVisualizer : HandVisualizerBase
{
    // AI nie potrzebuje klikniêæ; dostarczymy prost¹ metodê wykonuj¹c¹ ruch
    // gm powinien wywo³aæ ExecuteAIMove() lub u¿yæ EnemyPlayCardToSlot bezpoœrednio i potem PlayCardVisualByInstance

    // wersja 1: AI proste - zagra pierwsz¹ mo¿liw¹ kartê do pierwszego wolnego slotu
    public void ExecuteAIMove()
    {
        var gm = FindFirstObjectByType<CardGameLogicManager>();
        if (gm == null) { Debug.LogError("Brak CardGameLogicManager w scenie"); return; }

        int freeSlot = owner.GetFirstFreeSlotIndex();
        if (freeSlot == -1) return;

        for (int i = 0; i < owner.hand.Count; i++)
        {
            if (owner.hand[i].Cost <= owner.currentSzpont)
            {
                // wywo³aj logikê: metoda powinna usuwaæ kartê z rêki i przypisaæ do tableSlots
                bool ok = gm.EnemyPlayCardToSlot(i, freeSlot);
                if (ok)
                {
                    // wywo³aj animacjê (próbuje znaleŸæ wizual w spawnedCards i przenieœæ)
                    PlayCardVisualByInstance(owner.tableSlots[freeSlot], freeSlot);
                    return;
                }
            }
        }
    }
}
