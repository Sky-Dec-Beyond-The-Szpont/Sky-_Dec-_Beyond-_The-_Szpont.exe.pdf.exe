using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class TurnManager : MonoBehaviour
{
    public CardGameLogicManager gameLogic;
    public PlayCardAgent enemyAgent;
    public SoundManager soundManager;
    public GameInfo gameInfo;

    public InputActionReference confirmAction;

    public float enemyDelay = 1.0f; // czas przerwy zanim AI zagra
    public float roundEndDelay = 1.0f; // czas przed przejściem do następnej rundy
    public bool isPlayerTurn = true;

    public float endEpisodeDelay = 1.0f; // ile czekamy przed restartem (real time)

    public bool resetGameToMap = true;
    public string mapSceneName = "MapScene";
    public string winGameSceneName = "WinScene";
    public string menuSceneName = "MenuScene";

    public bool gameResetForAgentLearning = true;

    private void OnEnable()
    {
        confirmAction?.action?.Enable();
    }

    private void OnDisable()
    {
        confirmAction?.action?.Disable();
    }


    void Start()
    {
        if (gameLogic == null)
            gameLogic = FindFirstObjectByType<CardGameLogicManager>();

        if (enemyAgent == null)
            enemyAgent = FindFirstObjectByType<PlayCardAgent>();

        if (soundManager == null)
            soundManager = FindFirstObjectByType<SoundManager>();

        if (gameInfo == null)
            gameInfo = FindFirstObjectByType<GameInfo>();

        if (gameLogic == null)
        {
            Debug.LogError("TurnManager: brak referencji do GameLogicManager!");
            enabled = false;
        }
    }

    public void EndPlayerTurn()
    {

        if (gameLogic.GameEnded()) return;

        soundManager.PlayEndTurn();

        Debug.Log("=== PLAYER ENDS TURN ===");

        ResetRound();

        isPlayerTurn = false;

        StartCoroutine(EnemyTurnRoutine());
    }

    private IEnumerator EnemyTurnRoutine()
    {
        if (gameLogic.GameEnded())
        {
            NotifyGameEnded();
            yield break;
        }

        DrawCardAnimated(playerTurn: false);

        yield return new WaitForSeconds(enemyDelay);

        if (enemyAgent != null)
        {
            enemyAgent.BeginTurn();

            while (!enemyAgent.turnEnded && !gameLogic.GameEnded())
            {
                enemyAgent.RequestDecision();
                yield return new WaitForSeconds(0.05f);
            }
        }
        else
        {
            // fallback: stary prosty AI (jeśli agent nie ustawiony)
            if (gameLogic.enemyHandVisualizer != null)
                gameLogic.enemyHandVisualizer.ExecuteAIMove();
            yield return new WaitForSeconds(roundEndDelay);
        }

        // ✳️ odczekaj by zobaczyć ruch AI
        yield return new WaitForSeconds(roundEndDelay);

        // ✳️ Rozwiąż rundę (atak, śmierć kart itd.)
        ResetRound();

        // Jeżeli gra kończy się naturalnie -> obsłuż (tu zrobimy reset + dodatkowe nagrody)
        if (gameLogic.GameEnded())
        {
            NotifyGameEnded();
            yield break;
        }

        soundManager.PlayEndTurn();

        isPlayerTurn = true;
        Debug.Log("=== NEW PLAYER TURN ===");

        gameLogic.IncreaseSzpontAndRound();

        if (gameLogic.turnNumber > 50)
        {
            // Remis / Przeciąganie
            if (enemyAgent != null)
            {
                enemyAgent.EndEpisode();     // Koniec epizodu bez flagi win/lose
            }
            gameLogic.ResetGame(); // Reset planszy
            yield break;
        }

        DrawCardAnimated(playerTurn: true);

        yield break;
    }

    private void ResetRound()
    {
        gameLogic.EndRoundResolve(isPlayerTurn);
        gameLogic.playerHandVisualizer.RefreshTable();
        gameLogic.enemyHandVisualizer.RefreshTable();
    }

    private void DrawCardAnimated(bool playerTurn)
    {
        if (playerTurn)
        {
            bool cardDrawn = gameLogic.DrawToHand(gameLogic.player, 1);

            // ZABEZPIECZENIE: Sprawdzamy czy faktycznie mamy karty w ręce
            if (!cardDrawn || gameLogic.player.hand.Count == 0)
                return;

            CardInstance drawn = gameLogic.player.hand[gameLogic.player.hand.Count - 1];
            if (gameLogic.playerHandVisualizer != null)
                gameLogic.playerHandVisualizer.AnimateDrawToHand(drawn);
        }
        else
        {
            bool cardDrawn = gameLogic.DrawToHand(gameLogic.enemy, 1);

            // ZABEZPIECZENIE: To samo dla wroga. Jeśli Count == 0, wychodzimy.
            if (!cardDrawn || gameLogic.enemy.hand.Count == 0)
                return;

            CardInstance drawnEnemy = gameLogic.enemy.hand[gameLogic.enemy.hand.Count - 1];
            if (gameLogic.enemyHandVisualizer != null)
                gameLogic.enemyHandVisualizer.AnimateDrawToHand(drawnEnemy);
        }
    }

    public void NotifyGameEnded()
    {
        StartCoroutine(HandleEndOfGameCoroutine());
    }

    private IEnumerator HandleEndOfGameCoroutine()
    {
        Debug.Log("TurnManager: Game ended. Handling end-of-episode...");

        bool playerWin = gameLogic.scalePoints <= -gameLogic.endGameScalePoints;

        if (gameInfo != null)
        {
            gameInfo.TriggerGameOver(playerWin);
        }

        if (enemyAgent != null)
        {
            enemyAgent.FinishEpisode(playerWin, gameEnded: true);
        }

        if (resetGameToMap)
        {
            yield return StartCoroutine(WaitAndLoadSceneWithSpace(playerWin));
            yield break;
        }

        if (!gameResetForAgentLearning)
        {
            yield break;
        }

        yield return new WaitForSeconds(0.5f);

        if (gameLogic != null)
        {
            gameLogic.ResetGame();
        }

        if (enemyAgent != null) enemyAgent.OnEpisodeBegin();

        isPlayerTurn = true;
        Debug.Log("TurnManager: New episode started (after natural game end).");

        yield break;
    }

    private IEnumerator WaitAndLoadSceneWithSpace(bool playerWin)
    {
        // Odczekaj wskazany czas
        yield return new WaitForSeconds(3.0f);

        bool pressed = false;

        void OnPressed(InputAction.CallbackContext ctx)
        {
            pressed = true;
        }

        confirmAction.action.performed += OnPressed;

        while (!pressed)
        {
            yield return null;
        }

        confirmAction.action.performed -= OnPressed;


        if (LevelLoader.Instance != null)
        {
            if (playerWin)
            {
                int currentIndex = LevelLoader.Instance.GetCurrentOpponentIndex();

                if (currentIndex < 2)
                {
                    // WON (0 or 1) -> Advance and go back to Map
                    Debug.Log("Player Won! Advancing to next opponent.");
                    LevelLoader.Instance.AdvanceLevel();
                    LevelLoader.Instance.LoadLevelByName(mapSceneName);
                }
                else
                {
                    // WON (2) -> Game Finished!
                    Debug.Log("Player Defeated Final Boss! Loading End Scene.");
                    LevelLoader.Instance.ResetProgress(); // Reset for next time
                    LevelLoader.Instance.LoadLevelByName(winGameSceneName);
                }
            }
            else
            {
                // LOST -> Game Over -> Main Menu
                Debug.Log("Player Lost. Resetting progress and going to Menu.");
                LevelLoader.Instance.ResetProgress();
                LevelLoader.Instance.LoadLevelByName(menuSceneName);
            }
        }
    }

}
