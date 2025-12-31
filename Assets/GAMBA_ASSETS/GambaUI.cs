using UnityEngine;

public class GambaUI : MonoBehaviour
{
    public static GambaUI Instance;

    

    [Header("References")]
    [SerializeField] private GameObject drawPanel;   // Panel z UI losowania
    [SerializeField] private CardScroll cardScroll;  // Skrypt scrolla

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        drawPanel.SetActive(false);
    }

    private void OnEnable()
    {
        CardScroll.OnScrollFinished += OnDrawFinished;
    }

    private void OnDisable()
    {
        CardScroll.OnScrollFinished -= OnDrawFinished;
    }

    // ---------------- UI ----------------

    public void Show()
    {
        Debug.Log("SHOW DRAW UI");

        drawPanel.SetActive(true);
        GameStateManager.Instance.SetState(GameState.Gambling);
        //Time.timeScale = 0f;   // pauza gry
    }

    // PODPINASZ POD BUTTON "Losuj"
    public void StartDraw()
    {
        Debug.Log("START DRAW");

        //Time.timeScale = 1f;   // Update musi dzia³aæ
        cardScroll.Scroll();   // start animacji
    }

    // ---------------- CALLBACK ----------------

    private void OnDrawFinished()
    {
        Debug.Log("DRAW FINISHED");

      

        if (TowerClick.chosenTower != null)
        {
            
            TowerClick.chosenTower.ReturnFromTower();
        }
        else
        {
            Debug.LogError("chosenTower IS NULL");
        }
        drawPanel.SetActive(false);
    }
}
