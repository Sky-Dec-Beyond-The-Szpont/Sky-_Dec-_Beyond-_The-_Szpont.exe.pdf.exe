using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAutoController : MonoBehaviour
{
    public CardGameLogicManager manager;
    public TurnManager turnManager;

    [Tooltip("Delay miêdzy kolejnymi akcjami (w sekundach, real time). U¿yj ma³ej wartoœci np. 0.02 dla szybkiego treningu.")]
    public float actionDelayRealtime = 0.6f;

    [Tooltip("Maksymalna liczba akcji na turê (safety).")]
    public int maxActionsPerTurn = 1;

    [Header("Stochastic behaviour")]
    [Range(0f, 1f)]
    [Tooltip("Prawdopodobieñstwo, ¿e AI w ogóle spróbuje zagraæ kartê (otherwise przeka¿e turê).")]
    public float playProbability = 0.8f;

    [Range(0f, 1f)]
    [Tooltip("Jeœli true: z podan¹ szans¹ zagra losow¹ kartê zamiast heurystycznie wybranej (wiêcej eksploracji).")]
    public float randomPlayRate = 0.25f;

    [Range(0f, 1f)]
    [Tooltip("Z jak¹ szans¹ wybierze slot losowo zamiast 'pierwszego wolnego'.")]
    public float randomSlotRate = 0.2f;

    [Tooltip("Losowy dodatek do actionDelay (±).")]
    public float actionDelayNoise = 0.05f;

    [Tooltip("Seed generatora losowego; -1 = u¿yj Unity random.")]
    public int rngSeed = -1;

    private System.Random rng;
    private Coroutine runningRoutine = null;

    void Start()
    {
        if (manager == null) manager = FindFirstObjectByType<CardGameLogicManager>();
        if (turnManager == null) turnManager = FindFirstObjectByType<TurnManager>();
        rng = (rngSeed >= 0) ? new System.Random(rngSeed) : null;
    }

    void Update()
    {
        if (turnManager != null && turnManager.isPlayerTurn && runningRoutine == null)
        {
            runningRoutine = StartCoroutine(PlayerTurnRoutine());
        }
    }

    private IEnumerator PlayerTurnRoutine()
    {
        if (manager == null || turnManager == null)
        {
            runningRoutine = null;
            yield break;
        }

        int actions = 0;

        // krótki offset na animacje/rysowanie kart
        yield return new WaitForSeconds(0.5f);

        // decyzja: czy w ogóle próbujemy graæ w tej turze
        if (!RandomChance(playProbability))
        {
            // symulujemy krótk¹ "zastanawê" i pasujemy
            yield return new WaitForSeconds(0.15f + RandomRange(0f, 0.15f));
            turnManager.EndPlayerTurn();
            runningRoutine = null;
            yield break;
        }

        while (actions < maxActionsPerTurn && manager.player.GetFirstFreeSlotIndex() != -1)
        {
            bool anyPlayedThisIteration = false;

            int freeSlot = manager.player.GetFirstFreeSlotIndex();
            if (freeSlot == -1) break;

            // ewentualnie wybierz random slot zamiast first-free
            if (RandomChance(randomSlotRate))
            {
                var freeSlots = new List<int>();
                for (int s = 0; s < manager.slotCountPerSide; s++)
                    if (manager.player.tableSlots[s] == null) freeSlots.Add(s);

                if (freeSlots.Count > 0)
                    freeSlot = freeSlots[RandomInt(0, freeSlots.Count)];
            }

            // zbuduj lista mo¿liwych kart do zagrania (affordable)
            var playableIndices = new List<int>();
            for (int i = 0; i < manager.player.hand.Count; i++)
            {
                var card = manager.player.hand[i];
                if (card == null) continue;
                if (card.Cost <= manager.player.currentSzpont && manager.player.tableSlots[freeSlot] == null)
                {
                    playableIndices.Add(i);
                }
            }

            if (playableIndices.Count == 0) break;

            int chosenIndex = -1;

            // z szans¹ losow¹ wybierz przypadkow¹ kartê (suboptimal)
            if (RandomChance(randomPlayRate))
            {
                chosenIndex = playableIndices[RandomInt(0, playableIndices.Count)];
            }
            else
            {
                // heurystyka: wybierz najmniejszy koszt (lub najwy¿szy attack/cost ratio)
                chosenIndex = playableIndices[0];
                int best = chosenIndex;
                float bestScore = -999f;
                for (int j = 0; j < playableIndices.Count; j++)
                {
                    var c = manager.player.hand[playableIndices[j]];
                    if (c == null) continue;
                    // prosty scoring: preferuj wy¿sz¹ wartoœæ ataku wzglêdnie do cost, break ties cost ni¿szy
                    float score = (float)c.Attack - (0.2f * c.Cost) + (0.1f * c.currentHealth);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = playableIndices[j];
                    }
                }
                chosenIndex = best;
            }

            // spróbuj zagraæ
            bool ok = manager.PlayerPlayCardToSlot(chosenIndex, freeSlot);
            if (ok)
            {
                manager.playerHandVisualizer?.PlayCardVisualByInstance(manager.player.tableSlots[freeSlot], freeSlot);
                anyPlayedThisIteration = true;
                actions++;
            }

            if (!anyPlayedThisIteration) break;

            // noise w delayzie
            float delay = Mathf.Max(0f, actionDelayRealtime + RandomRange(-actionDelayNoise, actionDelayNoise));
            yield return new WaitForSeconds(delay);
        }

        // opcjonalne krótkie opóŸnienie dla czytelnoœci
        yield return new WaitForSeconds(0.25f + RandomRange(0f, 0.25f));

        turnManager.EndPlayerTurn();

        runningRoutine = null;
    }

    // pomocnicze losowe funkcje (u³atwiaj¹ testowalnoœæ seed'em)
    private bool RandomChance(float p)
    {
        if (rng != null) return rng.NextDouble() < p;
        return UnityEngine.Random.value < p;
    }

    private int RandomInt(int minInclusive, int maxExclusive)
    {
        if (rng != null) return rng.Next(minInclusive, maxExclusive);
        return UnityEngine.Random.Range(minInclusive, maxExclusive);
    }

    private float RandomRange(float a, float b)
    {
        if (rng != null) return (float)(a + rng.NextDouble() * (b - a));
        return UnityEngine.Random.Range(a, b);
    }
}
