using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.SceneManagement;


public class PlayCardAgent : Agent
{
    public CardGameLogicManager manager;
    public PlayerModel controlledAgent; // should be manager.player
    public TurnManager turnManager;

    public float maxStatNorm = 10f; // used to normalize attack/health/cost

    private int lastScalePoints = 0;
    private int actionsThisTurn = 0;
    private int maxActionsPerTurn = 8;

    [HideInInspector]
    public bool turnEnded = false;

    [Header("Telemetry")]
    public int totalEpisodes = 0;
    public int totalWins = 0;
    public int totalStepsThisEpisode = 0;      // counter for current episode
    public int lastEpisodeSteps = 0;

    public float lastEpisodeReward = 0f;       // final reward of last finished ep
    public float runningRewardAvg = 0f;        // moving average of episode reward
    public float runningWinRate = 0f;          // moving average of wins

    public int recentWindow = 100;             // window for moving averages
    private System.Collections.Generic.Queue<float> recentRewards = new System.Collections.Generic.Queue<float>();
    private System.Collections.Generic.Queue<int> recentWins = new System.Collections.Generic.Queue<int>();

    public DebugOverlay overlay;

    private int lastPlayerDeadSlotsCount = 0;
    private int lastEnemyDeadSlotsCount = 0;

    private int illegalActions = 0;

    void Update()
    {
        if (overlay != null)
        {
            overlay.SetText(
                $"episode: {CompletedEpisodes}\n" +
                $"lastReward: {GetCumulativeReward():F3}\n" +
                $"rewardAvg: {runningRewardAvg}" +
                $"illegal actions: {illegalActions}"
            );
        }
    }

    public override void Initialize()
    {
        // dodatkowa defensywna inicjalizacja
        if (manager == null) manager = FindFirstObjectByType<CardGameLogicManager>();
        if (turnManager == null) turnManager = FindFirstObjectByType<TurnManager>();

        if (manager != null)
        {
            manager.InitGame();
            controlledAgent = manager.enemy;
        }
    }

    private void RecordEpisodeOutcome(bool agentWin)
    {
        totalEpisodes++;
        lastEpisodeSteps = totalStepsThisEpisode;
        lastEpisodeReward = (float)GetCumulativeReward();

        // push into windows
        recentRewards.Enqueue(lastEpisodeReward);
        if (recentRewards.Count > recentWindow) recentRewards.Dequeue();

        recentWins.Enqueue(agentWin ? 1 : 0);
        if (recentWins.Count > recentWindow) recentWins.Dequeue();

        // running averages
        float sum = 0f;
        foreach (var r in recentRewards) sum += r;
        runningRewardAvg = sum / (recentRewards.Count > 0 ? recentRewards.Count : 1);

        int sumWins = 0;
        foreach (var w in recentWins) sumWins += w;
        runningWinRate = (float)sumWins / (recentWins.Count > 0 ? recentWins.Count : 1);

        if (agentWin) totalWins++;

        // reset per-episode counters
        totalStepsThisEpisode = 0;
    }

    public void FinishEpisode(bool playerWin, bool gameEnded)
    {
        if (gameEnded)
        {
            if (!playerWin) SetReward(1f); // agent win
            else SetReward(-1f); // agent lose
        }

        CheckForCardDeaths();

        RecordEpisodeOutcome(!playerWin);
        // mo¿esz dodaæ tutaj dodatkowe nagrody/penalty przed zakoñczeniem epizodu
        EndEpisode();
        turnEnded = true;
        actionsThisTurn = 0;
    }

    public override void OnEpisodeBegin()
    {
        if (manager != null)
        {
            //manager.ResetGame();

            controlledAgent = manager.enemy;
            lastScalePoints = manager.scalePoints;
        }

        totalStepsThisEpisode = 0;
        actionsThisTurn = 0;
        turnEnded = false;
    }

    private void CheckForCardDeaths()
    {
        var playerDeadSlots = manager.GetDeadSlotsFor(manager.player);
        var enemyDeadSlots = manager.GetDeadSlotsFor(manager.enemy);

        int playerDeadCount = playerDeadSlots?.Count ?? 0;
        int enemyDeadCount = enemyDeadSlots?.Count ?? 0;

        int newKillsByAgent = Mathf.Max(0, playerDeadCount - lastPlayerDeadSlotsCount);

        if (newKillsByAgent > 0)
        {
            // Tutaj musia³byœ mieæ dostêp do listy OSTATNIO zabitych kart, aby oceniæ ich si³ê.
            // Jeœli nie masz, zwiêksz ogóln¹ nagrodê.
            float baseKillReward = 1.0f; // Zwiêkszono z 0.3f
            AddReward(baseKillReward * newKillsByAgent);
        }

        int newLossesForAgent = Mathf.Max(0, enemyDeadCount - lastEnemyDeadSlotsCount);
        if (newLossesForAgent > 0)
        {
            float lossPenalty = -0.05f; // Zmniejszono z -0.1f
            AddReward(lossPenalty * newLossesForAgent);
        }

        lastPlayerDeadSlotsCount = playerDeadCount;
        lastEnemyDeadSlotsCount = enemyDeadCount;
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        if (manager == null || controlledAgent == null)
        {
            actionMask.SetActionEnabled(0, 1, false); // Blokuj zagrywanie kart
            return;
        }

        // --- Branch 0: Typ akcji (0: End Turn, 1: Play Card) ---
        // Zawsze mo¿emy zakoñczyæ turê, wiêc 0 jest zawsze true.
        // Sprawdzamy czy MO¯EMY zagraæ jak¹kolwiek kartê
        bool canPlayAnyCard = false;
        int freeSlotIndex = controlledAgent.GetFirstFreeSlotIndex();

        // SprawdŸ czy mamy manê i miejsce na stole
        if (freeSlotIndex != -1)
        {
            foreach (var card in controlledAgent.hand)
            {
                if (card != null && card.Cost <= controlledAgent.currentSzpont)
                {
                    canPlayAnyCard = true;
                    break;
                }
            }
        }

        if (!canPlayAnyCard)
        {
            actionMask.SetActionEnabled(0, 1, false); // Wy³¹cz opcjê "Zagraj kartê"
        }


        var hand = controlledAgent.hand;
        for (int i = 0; i < manager.maxHand; i++)
        {
            bool isPlayable = false;
            if (i < hand.Count)
            {
                var c = hand[i];
                // Karta musi istnieæ ORAZ jej koszt musi byæ <= mana
                if (c != null && c.Cost <= controlledAgent.currentSzpont)
                {
                    isPlayable = true;
                }
            }
            actionMask.SetActionEnabled(1, i, isPlayable);
        }


        var slots = controlledAgent.tableSlots;
        for (int i = 0; i < manager.slotCountPerSide; i++)
        {
            bool isSlotFree = (slots[i] == null);
            actionMask.SetActionEnabled(2, i, isSlotFree);
        }
    }

    private void CheckForBoardAdvantage()
    {
        float myTotalPower = 0;
        float oppTotalPower = 0;

        foreach (var card in manager.enemy.tableSlots)
            if (card != null) myTotalPower += card.Attack + card.currentHealth;

        foreach (var card in manager.player.tableSlots)
            if (card != null) oppTotalPower += card.Attack + card.currentHealth;

        float powerDiff = myTotalPower - oppTotalPower;

        // Nagroda za posiadanie silniejszego sto³u
        AddReward(powerDiff * 0.01f);
    }

    public void BeginTurn()
    {
        turnEnded = false;
        actionsThisTurn = 0;
        lastScalePoints = manager != null ? manager.scalePoints : 0;

        if (manager != null)
        {
            CheckForBoardAdvantage();
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation((float)manager.scalePoints / (float)manager.endGameScalePoints);

        sensor.AddObservation((float)manager.player.currentSzpont / 10f);
        sensor.AddObservation((float)manager.enemy.currentSzpont / 10f);

        int slots = manager.slotCountPerSide;

        // 3) per-slot features for player then enemy: presence, attack, health
        for (int i = 0; i < slots; i++)
        {
            var pCard = manager.player.tableSlots[i];
            if (pCard != null)
            {
                sensor.AddObservation(1f); // present
                sensor.AddObservation(pCard.Attack / maxStatNorm);
                sensor.AddObservation((float)pCard.currentHealth / maxStatNorm);
            }
            else
            {
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
            }
        }

        for (int i = 0; i < slots; i++)
        {
            var eCard = manager.enemy.tableSlots[i];
            if (eCard != null)
            {
                sensor.AddObservation(1f); // present
                sensor.AddObservation(eCard.Attack / maxStatNorm);
                sensor.AddObservation((float)eCard.currentHealth / maxStatNorm);
            }
            else
            {
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
            }
        }

        for (int i = 0; i < manager.maxHand; i++)
        {
            if (i < controlledAgent.hand.Count)
            {
                var c = controlledAgent.hand[i];
                if (c != null)
                {
                    sensor.AddObservation(1f);
                    sensor.AddObservation((float)c.Cost / maxStatNorm);
                    sensor.AddObservation((float)c.Attack / maxStatNorm);
                    sensor.AddObservation((float)c.currentHealth / maxStatNorm);
                }
                else
                {
                    sensor.AddObservation(0f);
                    sensor.AddObservation(0f);
                    sensor.AddObservation(0f);
                    sensor.AddObservation(0f);
                }
            }
            else
            {
                // brak karty -> wype³niamy zerami
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
            }
        }

        for (int i = 0; i < slots; i++)
        {
            var myCard = manager.enemy.tableSlots[i];   // Karta Agenta
            var oppCard = manager.player.tableSlots[i]; // Karta Przeciwnika

            float myAtt = myCard != null ? myCard.Attack : 0f;
            float myHp = myCard != null ? myCard.currentHealth : 0f;

            float oppAtt = oppCard != null ? oppCard.Attack : 0f;
            float oppHp = oppCard != null ? oppCard.currentHealth : 0f;

            // Logika: 1 = wygrywam, -1 = przegrywam, -2 = puste pole i dostanê obra¿enia
            float laneStatus = 0f;

            if (myCard != null && oppCard != null)
            {
                // Mamy starcie bezpoœrednie
                if (myAtt >= oppHp) laneStatus = 1.0f;       // Zabijê go
                else if (oppAtt >= myHp) laneStatus = -0.5f; // On mnie zabije (a ja jego nie)
                                                             // Mo¿esz dodaæ 0f jeœli obaj prze¿yj¹ lub zgin¹ (wymiana)
            }
            else if (myCard == null && oppCard != null)
            {
                laneStatus = -1.0f; // Bardzo Ÿle: puste pole naprzeciw wroga!
            }
            else if (myCard != null && oppCard == null)
            {
                laneStatus = 0.5f; // Dobrze: atakujê wroga bezpoœrednio (lub jego bazê)
            }

            sensor.AddObservation(laneStatus);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        totalStepsThisEpisode++;

        if (manager == null || controlledAgent == null)
        {
            AddReward(-0.01f);
            turnEnded = true;
            return;
        }

        // ignore actions if it's not the enemy turn
        if (turnManager != null && turnManager.isPlayerTurn)
        {
            // je¿eli to nie jest tura agenta, karzemy bardzo lekko i ignorujemy
            AddReward(-0.01f);
            return;
        }

        var act = actions.DiscreteActions;
        int actionType = act[0]; // 0 end-turn, 1 play card
        int handIndexRaw = act[1];
        int slotIndexRaw = act[2];

        int handIndex = handIndexRaw;
        int slotIndex = slotIndexRaw;

        if (actionType == 0)
        {
            turnEnded = true;

            EvaluateScaleDelta();

            return;
        }

        actionsThisTurn++;

        if (actionsThisTurn > maxActionsPerTurn)
        {
            AddReward(-0.01f);
            turnEnded = true;
            return;
        }

        bool played = manager.EnemyPlayCardToSlot(handIndex, slotIndex);

        if (played)
        {
            AddReward(0.05f); // ma³e pozytywne info za poprawne zagranie
            // opcjonalnie: odpal wizualizacje
            manager.enemyHandVisualizer?.PlayCardVisualByInstance(manager.enemy.tableSlots[slotIndex], slotIndex);
        }
        else
        {
            illegalActions++;
            AddReward(-0.002f);
        }
    }

    void EvaluateScaleDelta()
    {
        int delta = manager.scalePoints - lastScalePoints;
        if (delta > 0)
        {
            AddReward(delta * 0.05f);
        }
        else if (delta < 0)
        {
            AddReward(delta * 0.05f);
        }
        lastScalePoints = manager.scalePoints;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discrete = actionsOut.DiscreteActions;
        int freeSlot = controlledAgent.GetFirstFreeSlotIndex();
        if (freeSlot == -1 || controlledAgent.hand.Count == 0)
        {
            discrete[0] = 0; // end turn
            discrete[1] = manager.maxHand; // no-op
            discrete[2] = manager.slotCountPerSide; // no-op
            return;
        }

        for (int i = 0; i < controlledAgent.hand.Count; i++)
        {
            if (controlledAgent.hand[i].Cost <= controlledAgent.currentSzpont)
            {
                discrete[0] = 1;
                discrete[1] = i; // karta i
                discrete[2] = freeSlot;
                return;
            }
        }

        discrete[0] = 0;
        discrete[1] = manager.maxHand; // no-op
        discrete[2] = manager.slotCountPerSide; // no-op
    }
}
