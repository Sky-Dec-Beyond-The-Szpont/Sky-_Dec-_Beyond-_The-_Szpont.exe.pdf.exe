using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitClick : MonoBehaviour, IClickable
{
    public string nextSceneName = "SampleScene";

    public void OnClicked()
    {
        var chosen = TowerClick.chosenTower;
        if (chosen == null)
            return;

        var player = chosen.GetPlayer();
        var pathFromTowerToExit = chosen.GetPathFromTowerToExit();

        if (player == null || pathFromTowerToExit == null || pathFromTowerToExit.Count == 0)
            return;

        Debug.Log("Exit/Boss - idziemy z wybranej wie¿y do wyjœcia");

        player.MoveAlongWorldPositions(pathFromTowerToExit, () =>
        {
            Debug.Log("Dotarliœmy na exit - ³adowanie sceny: " + nextSceneName);
            SceneManager.LoadScene(nextSceneName);
        });

        TowerClick.chosenTower = null;
    }
}
