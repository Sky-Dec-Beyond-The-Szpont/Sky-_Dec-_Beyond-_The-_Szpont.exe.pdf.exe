using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameInfo : MonoBehaviour
{
    [SerializeField]
    private GameObject winInfo;
    [SerializeField]
    private GameObject loseInfo;

    [SerializeField]
    private GameObject scalePointPrefab;
    [SerializeField]
    private float scalePointsSpacing = 0.1f;
    [SerializeField]
    private Transform scalePointLocalization;

    [SerializeField]
    private Transform szpontObjLocalization;
    [SerializeField]
    private float szpontSpacing = 0.1f;
    [SerializeField]
    private GameObject szpontPrefab;

    [SerializeField] private GameObject axeObject;

    private CardGameLogicManager gameLogicManager;
    private SoundManager soundManager;

    private List<GameObject> spawnedCubes = new List<GameObject>();
    private List<GameObject> spawnedSzpont = new List<GameObject>();

    private int lastScalePoints;
    private int lastSzpont;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameLogicManager = FindFirstObjectByType<CardGameLogicManager>();
        soundManager = FindFirstObjectByType<SoundManager>();

        winInfo.SetActive(false);
        loseInfo.SetActive(false);

        if (gameLogicManager == null)
        {
            lastScalePoints = gameLogicManager.scalePoints;
            lastSzpont = gameLogicManager.player.currentSzpont;
        }
    }

    void Update()
    {
        if (gameLogicManager == null)
        {
            return;
        }

        CheckIfScalePointsChange();
        CheckIfSzpontChange();

        ShowWinLoseInfo();
    }

    private void ShowWinLoseInfo()
    {
        if (!gameLogicManager.GameEnded())
            return;

        bool playerWon = gameLogicManager.scalePoints < 0;

        winInfo.SetActive(playerWon);
        loseInfo.SetActive(!playerWon);

        axeObject.SetActive(true);
        
        Rigidbody axeRb = axeObject.GetComponent<Rigidbody>();
        axeRb.isKinematic = false;
        axeRb.useGravity = true;
    }

    private void CheckIfSzpontChange()
    {
        int currentValue = gameLogicManager.player.currentSzpont;

        if (currentValue != lastSzpont)
        {
            lastSzpont = currentValue;
            UpdateSzpontView();
        }
    }

    private void CheckIfScalePointsChange()
    {
        int currentValue = gameLogicManager.scalePoints;
        if (currentValue != lastScalePoints)
        {
            lastScalePoints = currentValue;
            UpdateScalePointsView();
        }
    }

    private void UpdateScalePointsView()
    {
        // Destroy only previously spawned cubes
        foreach (var cube in spawnedCubes)
        {
            if (cube != null)
                Destroy(cube);
        }

        spawnedCubes.Clear();

        int value = gameLogicManager.scalePoints;

        if (value == 0)
            return;

        int count = Mathf.Abs(value);

        // Decide direction:
        // positive (red) → downward from center
        // negative (green) → upward from center
        int direction = value > 0 ? -1 : 1;

        for (int i = 0; i < count; i++)
        {
            Vector3 targetPos = scalePointLocalization.position + new Vector3(0, 0, (i + 1) * direction * scalePointsSpacing);
            Vector3 spawnPos = targetPos + new Vector3(0, 2f, 0); // start 2 units above

            GameObject cube = Instantiate(scalePointPrefab, spawnPos, Quaternion.identity, scalePointLocalization);

            float yRotation = (direction == -1) ? 180f : 0f;
            cube.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);

            spawnedCubes.Add(cube);

            // Animate fall
            soundManager.PlayWeightChange();
            StartCoroutine(SmoothFall(cube.transform, targetPos, 0.4f + i * 0.05f));
        }
    }

    private void UpdateSzpontView()
    {
        // Destroy only previously spawned cubes
        foreach (var obj in spawnedSzpont)
        {
            if (obj != null)
                Destroy(obj);
        }

        spawnedSzpont.Clear();

        int value = gameLogicManager.player.currentSzpont;

        if (value == 0)
            return;

        for (int i = 0; i < value; i++)
        {
            Vector3 targetPos = szpontObjLocalization.position + new Vector3((i + 1) * szpontSpacing, 0, 0);
            Vector3 spawnPos = targetPos + new Vector3(0, 2f, 0); // fall from above

            GameObject prefab = Instantiate(szpontPrefab, spawnPos, Quaternion.identity, szpontObjLocalization);
            spawnedSzpont.Add(prefab);

            // Animate fall
            StartCoroutine(SmoothFall(prefab.transform, targetPos, 0.3f + i * 0.05f));
        }
    }

    private IEnumerator SmoothFall(Transform obj, Vector3 target, float duration)
    {
        Vector3 start = obj.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.Sin(t * Mathf.PI * 0.5f); // ease-out for smoother motion

            if (obj != null)
            {
                obj.position = Vector3.Lerp(start, target, t);
            }
            
            yield return null;
        }
        
        if(obj != null)
        {
            obj.position = target;
        }
    }


}

