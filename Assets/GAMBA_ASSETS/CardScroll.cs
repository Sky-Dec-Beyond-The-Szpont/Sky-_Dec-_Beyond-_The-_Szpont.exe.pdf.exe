using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardScroll : MonoBehaviour
{
    public static event Action OnScrollFinished;

    [Header("Setup")]
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private int cellsCount = 50;
    [SerializeField] private float scrollDistance = 600f;

    [SerializeField] private RectTransform drawPanel;



    private readonly List<CaseCell> _cells = new List<CaseCell>();

    private float _speed;
    private bool _isScrolling;

    private RectTransform _rect;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        CreateCells();
    }

    private void CreateCells()
    {
        for (int i = 0; i < cellsCount; i++)
        {
            var cell = Instantiate(cellPrefab, transform)
                .GetComponentInChildren<CaseCell>();

            _cells.Add(cell);
        }
    }

    public CaseCell GetCellAtCenter()
    {
        // ŒRODEK drawPanel w WORLD SPACE
        Vector3 panelCenterWorld =
            drawPanel.TransformPoint(new Vector3(0f, 0f, 0f));

        CaseCell closest = null;
        float minDistance = float.MaxValue;

        foreach (var cell in _cells)
        {
            RectTransform rect = cell.GetComponent<RectTransform>();

            // ŒRODEK karty w WORLD SPACE
            Vector3 cellCenterWorld =
                rect.TransformPoint(rect.rect.center);

            float distance = Mathf.Abs(cellCenterWorld.x - panelCenterWorld.x);

            if (distance < minDistance)
            {
                minDistance = distance;
                closest = cell;
            }
        }

        return closest;
    }


    public void Scroll()
    {
        if (_isScrolling)
            return;

        Debug.Log("SCROLL STARTED");

        _rect.anchoredPosition = new Vector2(scrollDistance, 0);

        foreach (var cell in _cells)
            cell.Setup();

        _speed = UnityEngine.Random.Range(4f, 5f);
        _isScrolling = true;
    }

    private void Update()
    {
        if (!_isScrolling)
            return;

        _rect.anchoredPosition += Vector2.left * _speed * 1000f * Time.unscaledDeltaTime;
        _speed -= Time.unscaledDeltaTime;

        if (_speed <= 0)
        {
            _speed = 0;
            _isScrolling = false;

            CaseCell winCell = GetCellAtCenter();

            Debug.Log("SCROLL FINISHED");
            Debug.Log("WYGRANA:");
            Debug.Log("Index: " + winCell.SelectedIndex);
            Debug.Log("Sprite: " + winCell.SelectedSprite.name);

            //Time.timeScale = 0f;


            OnScrollFinished?.Invoke();
        }

    }
}
