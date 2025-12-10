using System.Collections.Generic;
using UnityEngine;

public class TowerClick : MonoBehaviour, IClickable
{
    // Która wie¿a zosta³a wybrana (pierwszy klik)
    public static TowerClick chosenTower = null;

    public string label = "Wie¿a";

    private PlayerMover player;
    private List<Vector3> pathToTower;         // od startu do tej wie¿y
    private List<Vector3> pathFromTowerToExit; // od tej wie¿y do exita

    public void SetupPlayerPaths(PlayerMover playerMover,
                                 List<Vector3> toTower,
                                 List<Vector3> fromTowerToExit)
    {
        player = playerMover;
        pathToTower = toTower;
        pathFromTowerToExit = fromTowerToExit;
    }

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

            player.MoveAlongWorldPositions(pathToTower);
        }

    }
}
