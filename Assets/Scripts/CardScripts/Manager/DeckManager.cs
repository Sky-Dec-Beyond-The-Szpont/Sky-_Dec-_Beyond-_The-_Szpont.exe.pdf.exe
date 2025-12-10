using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    [SerializeField]
    private DeckSO playerDeckSO;
    [SerializeField] private List<DeckSO> opponentDecks = new List<DeckSO>();


    [SerializeField]
    public int multiplier = 2;

    public List<CardInstance> CreateUserDeck()
    {
        return CreateDeckFromDeckSO(playerDeckSO);
    }

    public List<CardInstance> CreateOpponentDeck(int deckIndex)
    {
        if (deckIndex < 0 || deckIndex >= opponentDecks.Count)
        {
            Debug.LogWarning($"Requested opponent deck index {deckIndex} is out of range.");
            return new List<CardInstance>();
        }

        return CreateDeckFromDeckSO(opponentDecks[deckIndex]);
    }

    private List<CardInstance> CreateDeckFromDeckSO(DeckSO deckSO)
    {
        var list = new List<CardInstance>();
        if (deckSO == null)
        {
            Debug.LogWarning("DeckSO not assigned in DeckManager.");
            return list;
        }

        // jeœli cardPool pusty - nic do robienia
        if (deckSO.cardPool == null || deckSO.cardPool.Length == 0) return list;

        // prosty sposób: powiela karty z poolu a¿ do defaultDeckSize (albo u¿yj multiplier)
        int desiredSize = deckSO.defaultDeckSize;
        int poolCount = deckSO.cardPool.Length;

        int idx = 0;
        while (list.Count < desiredSize)
        {
            var so = deckSO.cardPool[idx % poolCount];
            list.Add(new CardInstance(so));
            idx++;
        }

        List<CardInstance> multipliedList = new List<CardInstance>();

        for (int m = 0; m < multiplier; m++)
        {
            foreach (var so in list)
            {
                multipliedList.Add(so);
            }
        }

        ShuffleDeck(list);
        return list;
    }

    void ShuffleDeck(List<CardInstance> list)
    {
        System.Random r = new System.Random();

        for (int i = 0; i < list.Count; i++)
        {
            int j = r.Next(i, list.Count);
            var tmp = list[i]; list[i] = list[j]; list[j] = tmp;
        }
    }

    public CardInstance DrawFromListDeck(List<CardInstance> deckList)
    {
        if (deckList == null || deckList.Count == 0) return null;
        var c = deckList[0];
        deckList.RemoveAt(0);
        return c;
    }
}


