using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SittingEnemy : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> enemyPrefabList = new List<GameObject>();

    private GameObject currentPrefab;
    void Start()
    {
        int enemyIndex = 0;

        if (LevelLoader.Instance != null)
        {
            enemyIndex = LevelLoader.Instance.GetCurrentOpponentIndex();
        }

        enemyIndex = Mathf.Clamp(enemyIndex, 0, enemyPrefabList.Count - 1);

        GameObject prefab = enemyPrefabList[enemyIndex];

        Vector3 spawnPosition = transform.position + new Vector3(0f, -0.9f, 0f);
        Quaternion spawnRotation = transform.rotation * Quaternion.Euler(0f, 180f, 0f);

        currentPrefab = Instantiate(
            prefab,
            spawnPosition,
            spawnRotation,
            transform // optional parent
        );
    }

    
}
