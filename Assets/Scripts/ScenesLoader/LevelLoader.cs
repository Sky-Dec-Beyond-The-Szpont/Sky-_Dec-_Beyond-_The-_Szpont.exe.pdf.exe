using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    public static LevelLoader Instance { get; private set; }

    public Animator transition;
    public float transitionTime = 1f;

    private int _currentOpponentIndex = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // wa¿ne: transition canvas zostaje miêdzy scenami
    }

    public int GetCurrentOpponentIndex()
    {
        return _currentOpponentIndex;
    }

    public void AdvanceLevel()
    {
        _currentOpponentIndex++;
    }

    public void ResetProgress()
    {
        _currentOpponentIndex = 0;
    }

    public void LoadNextLevel()
    {
        StartCoroutine(LoadLevel(SceneManager.GetActiveScene().buildIndex + 1));
    }

    public void LoadLevelByName(string sceneName)
    {
        StartCoroutine(LoadLevelByNameRoutine(sceneName));
    }

    public void LoadSceneAdditiveWithTransition(string sceneName)
    {
        StartCoroutine(LoadAdditiveRoutine(sceneName));
    }
    public void PlayFadeIn()
    {
        transition.SetTrigger("End");
    }

    private IEnumerator LoadLevel(int levelIndex)
    {
        transition.SetTrigger("Start");
        yield return new WaitForSeconds(transitionTime);
        SceneManager.LoadScene(levelIndex);
    }

    private IEnumerator LoadLevelByNameRoutine(string sceneName)
    {
        // 1. Play Fade OUT (Screen turns black)
        transition.SetTrigger("Start");

        // 2. Wait for animation to finish
        yield return new WaitForSeconds(transitionTime);

        // 3. Load the scene ASYNCHRONOUSLY
        // (This keeps the game running while loading, instead of freezing)
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        // 4. Wait until the scene is fully loaded
        while (!operation.isDone)
        {
            yield return null;
        }

        // 5. Play Fade IN (Screen turns clear)
        // Since this script is DontDestroyOnLoad, it is still alive and can run this line!
        transition.SetTrigger("End");
    }

    private IEnumerator LoadAdditiveRoutine(string sceneName)
    {
        transition.SetTrigger("Start");
        yield return new WaitForSeconds(transitionTime);

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!op.isDone)
            yield return null;
    }
}
