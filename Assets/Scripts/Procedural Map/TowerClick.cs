using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TowerClick : MonoBehaviour, IClickable
{
    // Która wie¿a zosta³a wybrana (pierwszy klik)
    public static TowerClick chosenTower = null;

    public string label = "Wie¿a";

    [SerializeField] private Camera baseCamera;

    public string towerSceneName = "SampleScene";

    private PlayerMover player;
    private List<Vector3> pathToTower;         // od startu do tej wie¿y
    private List<Vector3> pathFromTowerToExit; // od tej wie¿y do exita

    private bool isTowerSceneLoaded = false;
    private Scene baseScene;

    private void Awake()
    {
        // zapamiêtujemy scenê bazow¹ (tê, w której jest wie¿a)
        baseScene = gameObject.scene;

        if (baseCamera == null)
        {
            baseCamera = Camera.main;
        }
    }

    public void SetupPlayerPaths(PlayerMover playerMover,
                                 List<Vector3> toTower,
                                 List<Vector3> fromTowerToExit)
    {
        player = playerMover;
        pathToTower = toTower;
        pathFromTowerToExit = fromTowerToExit;
    }

    private List<GameObject> baseSceneRoots = new();

    // u¿yje ExitClick
    public List<Vector3> GetPathFromTowerToExit() => pathFromTowerToExit;
    public PlayerMover GetPlayer() => player;

    public void OnClicked()
    {
        if (player == null || pathToTower == null || pathToTower.Count == 0)
            return;

        // Pierwszy wybór – wybieramy tê wie¿ê i idziemy do niej
        if (chosenTower == null)
        {
            chosenTower = this;
            Debug.Log(label + " - wybrano trasê do wie¿y");

            // po dojœciu do wie¿y wywo³a siê OnPlayerArrivedToTower
            player.MoveAlongWorldPositions(pathToTower, OnPlayerArrivedToTower);
        }
    }

    /// <summary>
    /// Wywo³ywane przez PlayerMover po dojœciu do ostatniego punktu pathToTower.
    /// </summary>
    private void OnPlayerArrivedToTower()
    {
        if (string.IsNullOrEmpty(towerSceneName))
        {
            Debug.LogWarning($"TowerClick ({label}): towerSceneName nie jest ustawione.");
            return;
        }

        if (isTowerSceneLoaded)
            return;

        if (player != null)
            player.enabled = false;

        StartCoroutine(LoadTowerSceneAdditive_WithTransition());
    }

    private IEnumerator LoadTowerSceneAdditive_WithTransition()
    {
        if (LevelLoader.Instance == null)
        {
            Debug.LogError("Brak LevelLoader.Instance w scenie. Upewnij siê, ¿e LevelLoader jest w scenie bazowej.");
            yield break;
        }

        // fade + load additive
        LevelLoader.Instance.LoadSceneAdditiveWithTransition(towerSceneName);

        // czekamy a¿ scena faktycznie siê pojawi
        while (!SceneManager.GetSceneByName(towerSceneName).IsValid() ||
               !SceneManager.GetSceneByName(towerSceneName).isLoaded)
        {
            yield return null;
        }

        isTowerSceneLoaded = true;

        Scene towerScene = SceneManager.GetSceneByName(towerSceneName);
        SceneManager.SetActiveScene(towerScene);

        if (baseCamera != null)
            baseCamera.enabled = false;

        LevelLoader.Instance.PlayFadeIn();
        Debug.Log($"TowerClick ({label}): za³adowano scenê wie¿y z przejœciem: {towerSceneName}");
    }


    [ContextMenu("Debug Return From Tower")]
    private void DebugReturnFromTower()
    {
        ReturnFromTower();
    }

    /// <summary>
    /// Wo³asz to ze sceny wie¿y, gdy mini-poziom jest ukoñczony.
    /// </summary>
    public void ReturnFromTower()
    {
        if (!isTowerSceneLoaded)
            return;

        StartCoroutine(UnloadTowerSceneAndReturn());
    }

    private IEnumerator UnloadTowerSceneAndReturn()
    {
        // 1) Fade OUT (animacja przejœcia) przed powrotem
        if (LevelLoader.Instance != null)
        {
            LevelLoader.Instance.transition.SetTrigger("Start");
            yield return new WaitForSeconds(LevelLoader.Instance.transitionTime);
        }
        else
        {
            Debug.LogWarning("UnloadTowerSceneAndReturn: brak LevelLoader.Instance – powrót bez animacji.");
        }

        // 2) Unload sceny wie¿y
        Scene towerScene = SceneManager.GetSceneByName(towerSceneName);
        if (towerScene.IsValid())
        {
            AsyncOperation op = SceneManager.UnloadSceneAsync(towerScene);
            while (!op.isDone)
                yield return null;
        }

        isTowerSceneLoaded = false;

        // 3) Przywrócenie sceny bazowej, kamery i sterowania
        if (baseScene.IsValid())
            SceneManager.SetActiveScene(baseScene);

        if (baseCamera != null)
            baseCamera.enabled = true;

        if (player != null)
            player.enabled = true;

        LevelLoader.Instance.PlayFadeIn();

        Debug.Log($"TowerClick ({label}): powrót z wie¿y do sceny bazowej.");
    }

}
