using System.Collections.Generic;
using UnityEngine;

public class PlayerModel
{
    public string name;
    public int maxSzpont = 1;
    public int currentSzpont;

    public List<CardInstance> deck = new List<CardInstance>();
    public List<CardInstance> hand = new List<CardInstance>();

    public CardInstance[] tableSlots;

    public PlayerModel(string name, int slotCount = 4)
    {
        this.name = name;
        currentSzpont = 1;
        tableSlots = new CardInstance[slotCount];
    }

    public int SlotCount => tableSlots.Length;

    public int GetFirstFreeSlotIndex()
    {
        for (int i = 0; i < tableSlots.Length; i++)
        {
            if (tableSlots[i] == null) return i;
        }
        return -1;
    }
}
