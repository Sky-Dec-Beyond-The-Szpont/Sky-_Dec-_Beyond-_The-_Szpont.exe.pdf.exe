// GameLogicManager.cs
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class // Przygotuj now¹ turê:
CardGameLogicManager : MonoBehaviour
{
    public DeckManager deckManager;
    public int slotCountPerSide = 4;
    public int maxHand = 6;

    public PlayerModel player;
    public PlayerModel enemy;

    [Header("Visualizers")]
    public PlayerHandVisualizer playerHandVisualizer;
    public AIHandVisualizer enemyHandVisualizer;

    private Dictionary<PlayerModel, CardInstance[]> lastBoardSnapshot = new Dictionary<PlayerModel, CardInstance[]>();
    private Dictionary<PlayerModel, List<int>> deadSlotsThisTurn = new Dictionary<PlayerModel, List<int>>();

    public int turnNumber = 1;
    public int endGameScalePoints = 10;

    public int scalePoints = 0;

    public int opponentIndex = 1;

    public void InitGame()
    {
        player = new PlayerModel("Player", slotCountPerSide);
        enemy = new PlayerModel("Enemy", slotCountPerSide);

        player.deck = deckManager.CreateUserDeck();
        enemy.deck = deckManager.CreateOpponentDeck(opponentIndex);

        DrawToHand(player, 4);
        DrawToHand(enemy, 4);

        if (playerHandVisualizer != null)
        {
            playerHandVisualizer.owner = player;
            playerHandVisualizer.RefreshHand();
        }

        if (enemyHandVisualizer != null)
        {
            enemyHandVisualizer.owner = enemy;
            enemyHandVisualizer.RefreshHand();
        }

        InitializeSnapshotsForPlayers();

        Debug.Log($"Game initialized. Scale weight = {scalePoints}");
        LogHands();
    }

    public void ResetGame()
    {
        Debug.Log("CardGameLogicManager: ResetGame - restarting match.");

        if (playerHandVisualizer != null)
        {
            playerHandVisualizer.owner = null;
            playerHandVisualizer.RefreshHand();
        }
        if (enemyHandVisualizer != null)
        {
            enemyHandVisualizer.owner = null;
            enemyHandVisualizer.RefreshHand();
        }

        turnNumber = 1;
        scalePoints = 0;

        player = new PlayerModel("Player", slotCountPerSide);
        enemy = new PlayerModel("Enemy", slotCountPerSide);

        player.deck = deckManager.CreateUserDeck();
        enemy.deck = deckManager.CreateOpponentDeck(opponentIndex);

        DrawToHand(player, 4);
        DrawToHand(enemy, 4);

        // 8) Przypisz w³aœcicieli wizualizatorów i odœwie¿ (teraz ownery wskazuj¹ nowe modele)
        if (playerHandVisualizer != null)
        {
            playerHandVisualizer.owner = player;
            playerHandVisualizer.RefreshHand();
            playerHandVisualizer.RefreshTable();
        }
        if (enemyHandVisualizer != null)
        {
            enemyHandVisualizer.owner = enemy;
            enemyHandVisualizer.RefreshHand();
            enemyHandVisualizer.RefreshTable();
        }

        InitializeSnapshotsForPlayers();

        // 10) Debug
        Debug.Log($"CardGameLogicManager: Reset complete. scalePoints={scalePoints}. Player hand={player.hand.Count}, Enemy hand={enemy.hand.Count}");
        LogHands();
    }


    void InitializeSnapshotsForPlayers()
    {
        // prepare dictionaries for both players
        lastBoardSnapshot[player] = new CardInstance[slotCountPerSide];
        lastBoardSnapshot[enemy] = new CardInstance[slotCountPerSide];

        deadSlotsThisTurn[player] = new List<int>();
        deadSlotsThisTurn[enemy] = new List<int>();

        // fill snapshots with current references (may be all null)
        UpdateSnapshotForPlayer(player);
        UpdateSnapshotForPlayer(enemy);
    }

    void UpdateSnapshotForPlayer(PlayerModel p)
    {
        if (!lastBoardSnapshot.ContainsKey(p) || lastBoardSnapshot[p] == null || lastBoardSnapshot[p].Length != slotCountPerSide)
            lastBoardSnapshot[p] = new CardInstance[slotCountPerSide];

        if (p != null && p.tableSlots != null)
        {
            for (int i = 0; i < slotCountPerSide; i++)
                lastBoardSnapshot[p][i] = (i < p.tableSlots.Length) ? p.tableSlots[i] : null;
        }
        else
        {
            for (int i = 0; i < slotCountPerSide; i++)
                lastBoardSnapshot[p][i] = null;
        }
    }

    public List<int> GetDeadSlotsFor(PlayerModel p)
    {
        if (p == null) return new List<int>();
        if (!deadSlotsThisTurn.ContainsKey(p)) return new List<int>();
        // zwracamy kopiê, aby caller nie modyfikowa³ wewnêtrznej listy
        return new List<int>(deadSlotsThisTurn[p]);
    }

    public bool DrawToHand(PlayerModel p, int n)
    {
        if (p.hand.Count >= maxHand)
        {
            return false;
        }

        for (int i = 0; i < n; i++)
        {
            var c = deckManager.DrawFromListDeck(p.deck);
            if (c == null) break;
            p.hand.Add(c);
        }

        return true;
    }

    // pomocnik debugowy
    public void LogHands()
    {
        Debug.Log("--- PLAYER HAND ---");
        for (int i = 0; i < player.hand.Count; i++)
        {
            var c = player.hand[i];
            Debug.Log($"[{i}] {c.Name} ATK:{c.Attack} HP:{c.currentHealth} COST:{c.Cost}");
        }
        Debug.Log("--- ENEMY HAND ---");
        for (int i = 0; i < enemy.hand.Count; i++)
        {
            var c = enemy.hand[i];
            Debug.Log($"[{i}] {c.Name} ATK:{c.Attack} HP:{c.currentHealth} COST:{c.Cost}");
        }
    }

    // publiczna metoda: player próbuje zagraæ kartê z rêki na slot index
    // zwraca true jeœli zagrana; zak³ada, ¿e walidacja szpontu wykonana
    public bool PlayerPlayCardToSlot(int handIndex, int slotIndex)
    {
        if (handIndex < 0 || handIndex >= player.hand.Count)
        {
            Debug.LogWarning("PlayerPlayCardToSlot: nieprawid³owy indeks rêki.");
            return false;
        }
        if (slotIndex < 0 || slotIndex >= player.SlotCount)
        {
            Debug.LogWarning("PlayerPlayCardToSlot: nieprawid³owy slotIndex.");
            return false;
        }
        if (player.tableSlots[slotIndex] != null)
        {
            Debug.Log("Slot zajêty.");
            return false;
        }

        var card = player.hand[handIndex];
        if (card.Cost > player.currentSzpont)
        {
            Debug.Log("Za ma³o szpontu ¿eby zagraæ kartê.");
            return false;
        }

        // zap³aæ koszt, przenieœ z rêki na slot
        player.currentSzpont -= card.Cost;
        player.hand.RemoveAt(handIndex);
        player.tableSlots[slotIndex] = card;
        Debug.Log($"Player zagra³ {card.Name} na slot {slotIndex}. Szpont left: {player.currentSzpont}");

        return true;
    }

    // analogiczna metoda dla enemy (mo¿na wywo³aæ AI by zagraæ)
    public bool EnemyPlayCardToSlot(int handIndex, int slotIndex)
    {
        if (handIndex < 0 || handIndex >= enemy.hand.Count) return false;
        if (slotIndex < 0 || slotIndex >= enemy.SlotCount) return false;
        if (enemy.tableSlots[slotIndex] != null) return false;

        var card = enemy.hand[handIndex];
        if (card.Cost > enemy.currentSzpont) return false;

        enemy.currentSzpont -= card.Cost;
        enemy.hand.RemoveAt(handIndex);
        enemy.tableSlots[slotIndex] = card;
        Debug.Log($"Enemy zagra³ {card.Name} na slot {slotIndex}. Szpont left: {enemy.currentSzpont}");
        return true;
    }

    // EndRound: rozwi¹¿ walkê dla ka¿dego slotu (i zadawanie dmg do player/enemy HP jeœli naprzeciwko puste)
    public void EndRoundResolve(bool isPlayerTurn)
    {
        Debug.Log($"--- End of Round {turnNumber} resolve ---");

        int slots = slotCountPerSide;

        int[] playerCardHpAfter = new int[slots];
        int[] enemyCardHpAfter = new int[slots];

        // inicjalizacja
        for (int i = 0; i < slots; i++)
        {
            playerCardHpAfter[i] = player.tableSlots[i] != null ? player.tableSlots[i].currentHealth : -1;
            enemyCardHpAfter[i] = enemy.tableSlots[i] != null ? enemy.tableSlots[i].currentHealth : -1;
        }

        DetectAndStoreDeathsForPlayers();

        // Po detekcji zaktualizuj snapshoty do bie¿¹cego stanu (¿eby kolejne porównanie dzia³a³o poprawnie)
        UpdateSnapshotForPlayer(player);
        UpdateSnapshotForPlayer(enemy);

        PerformCardAttacks(slots, playerCardHpAfter, enemyCardHpAfter, isPlayerTurn);

        // Usuñ karty które dosta³y <=0 HP
        CleanTableFromDeadCards(slots, playerCardHpAfter, enemyCardHpAfter);

        Debug.Log($"Turn {turnNumber} start. Player szpont={player.currentSzpont} Enemy szpont={enemy.currentSzpont}");
        LogTableState();
    }

    public bool GameEnded()
    {
        if (scalePoints >= endGameScalePoints)
        {
            return true;
        }

        if (scalePoints <= endGameScalePoints * (-1))
        {
            return true;
        }

        return false;
    }

    public void IncreaseSzpontAndRound()
    {
        turnNumber++;

        player.maxSzpont = Mathf.Min(10, player.maxSzpont + 1);
        enemy.maxSzpont = Mathf.Min(10, enemy.maxSzpont + 1);

        player.currentSzpont = player.maxSzpont;
        enemy.currentSzpont = enemy.maxSzpont;
    }

    void CleanTableFromDeadCards(int slots, int[] playerCardHpAfter, int[] enemyCardHpAfter)
    {
        for (int i = 0; i < slots; i++)
        {
            if (player.tableSlots[i] != null)
            {
                if (playerCardHpAfter[i] <= 0)
                {
                    Debug.Log($"Player card {player.tableSlots[i].Name} died on slot {i}.");
                    player.tableSlots[i] = null;
                }
                else
                {
                    player.tableSlots[i].currentHealth = playerCardHpAfter[i];
                    Debug.Log($"Player card {player.tableSlots[i].Name} survives slot {i} with HP {player.tableSlots[i].currentHealth}");
                }
            }
            if (enemy.tableSlots[i] != null)
            {
                if (enemyCardHpAfter[i] <= 0)
                {
                    Debug.Log($"Enemy card {enemy.tableSlots[i].Name} died on slot {i}.");
                    enemy.tableSlots[i] = null;
                }
                else
                {
                    enemy.tableSlots[i].currentHealth = enemyCardHpAfter[i];
                    Debug.Log($"Enemy card {enemy.tableSlots[i].Name} survives slot {i} with HP {enemy.tableSlots[i].currentHealth}");
                }
            }
        }

        // TERAZ: porównaj aktualne boardy z lastBoardSnapshot i zapisz zgony w deadSlotsThisTurn.
        DetectAndStoreDeathsForPlayers();

        // Po detekcji zaktualizuj snapshoty do bie¿¹cego stanu (¿eby kolejne porównanie dzia³a³o poprawnie)
        UpdateSnapshotForPlayer(player);
        UpdateSnapshotForPlayer(enemy);
    }

    public void AcknowledgeDeath(PlayerModel p, int slotIndex)
    {
        if (p == null) return;
        if (!deadSlotsThisTurn.ContainsKey(p)) return;
        deadSlotsThisTurn[p].RemoveAll(i => i == slotIndex);
    }

    void DetectAndStoreDeathsForPlayers()
    {
        // Helper local
        void checkPlayer(PlayerModel p)
        {
            if (p == null) return;
            if (!lastBoardSnapshot.ContainsKey(p))
                lastBoardSnapshot[p] = new CardInstance[slotCountPerSide];

            if (!deadSlotsThisTurn.ContainsKey(p))
                deadSlotsThisTurn[p] = new List<int>();

            var prev = lastBoardSnapshot[p];

            for (int i = 0; i < slotCountPerSide; i++)
            {
                CardInstance previousInst = (prev != null && i < prev.Length) ? prev[i] : null;
                CardInstance currentInst = (p.tableSlots != null && i < p.tableSlots.Length) ? p.tableSlots[i] : null;

                if (previousInst != null && currentInst == null)
                {
                    // detected death on this slot
                    if (!deadSlotsThisTurn[p].Contains(i))
                        deadSlotsThisTurn[p].Add(i);
                }
            }
        }

        checkPlayer(player);
        checkPlayer(enemy);
    }

    void PerformCardAttacks(int slots, int[] playerCardHpAfter, int[] enemyCardHpAfter, bool isPlayerTurn)
    {
        for (int i = 0; i < slots; i++)
        {
            var pCard = player.tableSlots[i];
            var eCard = enemy.tableSlots[i];

            if (pCard != null && eCard != null)
            {
                // przewidywane HP po wymianie ciosów
                int resultingPlayerHp = playerCardHpAfter[i] - eCard.Attack;
                int resultingEnemyHp = enemyCardHpAfter[i] - pCard.Attack;

                bool playerWillDie = resultingPlayerHp <= 0;
                bool enemyWillDie = resultingEnemyHp <= 0;

                // znajdŸ widoki
                CardView playerView = playerHandVisualizer.FindCardViewInSlotsByInstance(pCard);
                CardView enemyView = enemyHandVisualizer.FindCardViewInSlotsByInstance(eCard);

                // Wyznacz kto atakuje (tylko attacker animuje)
                CardView attackerView = isPlayerTurn ? playerView : enemyView;
                CardView defenderView = isPlayerTurn ? enemyView : playerView;

                Transform defenderTransform = isPlayerTurn ? (enemyView != null ? enemyView.transform : null)
                                            : (playerView != null ? playerView.transform : null);

                // wybór wariantu animacji wzglêdem regu³:
                // - obie prze¿ywaj¹ -> Simple
                // - jedna umiera -> SpinLunge dla killer (ale animuje tylko attacker, wiêc tylko gdy attacker jest killer)
                // - obie umieraj¹ -> SlashSquash (animuje attacker)
                if (attackerView != null)
                {
                    if (!playerWillDie && !enemyWillDie)
                    {
                        // obie prze¿ywaj¹ -> simple animacja attacker
                        attackerView.PlayAttackAnimationVariant(defenderTransform, AttackAnimationVariant.Simple, null);
                    }
                    else if (playerWillDie && enemyWillDie)
                    {
                        // obie umieraj¹ -> attacker robi SlashSquash (tylko attacker animuje)
                        attackerView.PlayAttackAnimationVariant(defenderTransform, AttackAnimationVariant.SlashSquash, null);
                        defenderView.PlayAttackAnimationVariant(defenderView?.transform, AttackAnimationVariant.SlashSquash, null);
                    }
                    else
                    {
                        // tylko jedna umiera -> jeœli attacker zabija (czyli defenderWillDie jeœli attacker to gracz lub enemy w zale¿noœci od tury)
                        bool attackerKillsTarget = isPlayerTurn ? enemyWillDie : playerWillDie;
                        if (attackerKillsTarget)
                        {
                            // attacker jest zabójc¹ -> dostaje SpinLunge
                            attackerView.PlayAttackAnimationVariant(defenderTransform, AttackAnimationVariant.SpinLunge, null);
                        }
                        else
                        {
                            // attacker nie zabija -> prosty atak
                            attackerView.PlayAttackAnimationVariant(defenderTransform, AttackAnimationVariant.Simple, null);
                        }
                    }
                }

                playerCardHpAfter[i] -= eCard.Attack;
                enemyCardHpAfter[i] -= pCard.Attack;

                Debug.Log($"Slot {i}: {pCard.Name} (atk {pCard.Attack}) VS {eCard.Name} (atk {eCard.Attack})");
            }
            else if (pCard != null && eCard == null && isPlayerTurn)
            {
                // pusta strona przeciwnika -> dmg na enemy HP
                scalePoints -= pCard.Attack;
                Debug.Log($"Slot {i}: {pCard.Name} uderza w ENEMY za {pCard.Attack}. Waga: {scalePoints}");
            }
            else if (pCard == null && eCard != null && !isPlayerTurn)
            {
                // przeciwnik uderza w gracza
                scalePoints += eCard.Attack;
                Debug.Log($"Slot {i}: {eCard.Name} uderza w PLAYER za {eCard.Attack}. Waga: {scalePoints}");
            }
        }
    }

    void LogTableState()
    {
        for (int i = 0; i < slotCountPerSide; i++)
        {
            string p = player.tableSlots[i] != null ? $"{player.tableSlots[i].Name}({player.tableSlots[i].currentHealth})" : "empty";
            string e = enemy.tableSlots[i] != null ? $"{enemy.tableSlots[i].Name}({enemy.tableSlots[i].currentHealth})" : "empty";
            Debug.Log($"Slot {i}: Player:{p}  ||  Enemy:{e}");
        }
        Debug.Log($"Waga: {scalePoints}");
    }
}
