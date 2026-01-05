using System;
using System.Collections.Generic;
using System.Linq;
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

    [SerializeField] private List<DeckSO> opponentDecks = new List<DeckSO>();
    
    private DeckSO opponentDeck;
    [SerializeField] private DeckSO playerDeck;



    private readonly List<CaseCell> _cells = new List<CaseCell>();

    private float _speed;
    private bool _isScrolling;

    private RectTransform _rect;

    private void Start()
    {
        _rect = GetComponent<RectTransform>();
        CreateCells();
    }

    private void CreateCells()
    {
        _cells.Clear();

        int opponentIndex = LevelLoader.Instance.GetCurrentOpponentIndex();

        opponentDeck = opponentDecks.ElementAt(opponentIndex);

        for (int i = 0; i < cellsCount; i++)
        {
            var cell = Instantiate(cellPrefab, transform)
                .GetComponentInChildren<CaseCell>();

            CardSO randomCard =
                opponentDeck.cardPool[UnityEngine.Random.Range(0, opponentDeck.cardPool.Length)];

            cell.Setup(randomCard);
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

        _speed = UnityEngine.Random.Range(4f, 5f);
        _isScrolling = true;
    }


    public void TransferCard(CardSO card)
    {
        // usuñ z decka przeciwnika
        var opponentList = new List<CardSO>(opponentDeck.cardPool);
        opponentList.Remove(card);
        opponentDeck.cardPool = opponentList.ToArray();

        // dodaj do decka gracza
        var playerList = new List<CardSO>(playerDeck.cardPool);
        playerList.Add(card);
        playerDeck.cardPool = playerList.ToArray();

        Debug.Log($"Przeniesiono kartê {card.name} do decka gracza");
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

            CardSO wonCard = winCell.Card;

            Debug.Log("SCROLL FINISHED");
            Debug.Log("WYGRANA KARTA:");
            Debug.Log("Card: " + winCell.Card.name);
            Debug.Log("Card: " + winCell.Card.artwork);

            TransferCard(wonCard);



            //Time.timeScale = 0f;


            OnScrollFinished?.Invoke();
        }

    }
}
