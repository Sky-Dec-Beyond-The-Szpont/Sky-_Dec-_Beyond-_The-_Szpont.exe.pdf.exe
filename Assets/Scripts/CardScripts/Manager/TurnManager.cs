using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TurnManager : MonoBehaviour
{
    public CardGameLogicManager gameLogic;
    public PlayCardAgent enemyAgent;
    public SoundManager soundManager;

    public float enemyDelay = 1.0f; // czas przerwy zanim AI zagra
    public float roundEndDelay = 1.0f; // czas przed przejściem do następnej rundy
    public bool isPlayerTurn = true;

    public float endEpisodeDelay = 1.0f; // ile czekamy przed restartem (real time)
    public bool autoRestart = true;


    public bool resetGameToMap = true;
    public string mapSceneName = "MapScene";

    private bool gameEnded = false;
    public bool endEpisodeAfterBothTurns = true;

    bool gameResetForAgentLearning = false;

    void Start()
    {
        if (gameLogic == null)
            gameLogic = FindFirstObjectByType<CardGameLogicManager>();

        if (enemyAgent == null)
            enemyAgent = FindFirstObjectByType<PlayCardAgent>();

        if (soundManager == null)
            soundManager = FindFirstObjectByType<SoundManager>();

        if (gameLogic == null)
        {
            Debug.LogError("TurnManager: brak referencji do GameLogicManager!");
            enabled = false;
        }
    }

    public void EndPlayerTurn()
    {

        if (gameEnded) return;

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
            gameEnded = true;
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
            gameEnded = true;
            NotifyGameEnded();
            yield break;
        }

        if (endEpisodeAfterBothTurns && enemyAgent != null)
        {
            bool playerWin = gameLogic.scalePoints < 0;

            if (endEpisodeDelay > 0f)
                yield return new WaitForSeconds(endEpisodeDelay);

            enemyAgent.FinishEpisode(playerWin, gameEnded: false);

            enemyAgent.OnEpisodeBegin();
        }

        soundManager.PlayEndTurn();

        isPlayerTurn = true;
        Debug.Log("=== NEW PLAYER TURN ===");

        gameLogic.IncreaseSzpontAndRound();

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

            if (!cardDrawn)
                return;

            CardInstance drawn = gameLogic.player.hand[gameLogic.player.hand.Count - 1]; // ostatnio dodana
            if (gameLogic.playerHandVisualizer != null)
                gameLogic.playerHandVisualizer.AnimateDrawToHand(drawn);
        }
        else
        {
            bool cardDrawn = gameLogic.DrawToHand(gameLogic.enemy, 1);

            if (!cardDrawn)
                return;

            CardInstance drawnEnemy = gameLogic.enemy.hand[gameLogic.enemy.hand.Count - 1];
            if (gameLogic.enemyHandVisualizer != null)
                gameLogic.enemyHandVisualizer.AnimateDrawToHand(drawnEnemy);
        }
    }

    public void NotifyGameEnded()
    {
        if (!autoRestart) return;
        StartCoroutine(HandleEndOfGameCoroutine());
    }

    private IEnumerator HandleEndOfGameCoroutine()
    {
        Debug.Log("TurnManager: Game ended. Handling end-of-episode...");

        if (enemyAgent != null)
        {
            bool playerWin = gameLogic.scalePoints <= -gameLogic.endGameScalePoints;
            enemyAgent.FinishEpisode(playerWin, gameEnded: true);
        }

        if (resetGameToMap)
        {
            yield return StartCoroutine(WaitAndLoadSceneWithSpace(mapSceneName, 3f));
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
        gameEnded = false;
        Debug.Log("TurnManager: New episode started (after natural game end).");

        yield break;
    }

    private IEnumerator WaitAndLoadSceneWithSpace(string sceneName, float waitSeconds)
    {
        // Odczekaj wskazany czas
        yield return new WaitForSeconds(waitSeconds);

        // Czekaj, aż użytkownik wciśnie przypisaną akcję (continueAction) LUB spację (fallback)
        while (true)
        {
            // fallback: spróbuj Keyboard.current (Input System). Działa tylko jeśli Input System jest aktywny.
            if (Keyboard.current != null)
            {
                if (Keyboard.current.spaceKey.wasPressedThisFrame)
                    break;
            }

            yield return null;
        }

        // Ładuj scenę
        SceneManager.LoadSceneAsync(sceneName);
    }

}
