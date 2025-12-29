using UnityEngine;

public class Interactable : MonoBehaviour
{
    [Header("Objects to destroy when interacted")]
    public GameObject[] objectsToDestroy;  // dynamic scene objects

    // Called by Raycast logic
    public void OnInteract()
    {
        foreach (GameObject obj in objectsToDestroy)
        {
            if (obj != null)
                Destroy(obj);
        }
    }
}
