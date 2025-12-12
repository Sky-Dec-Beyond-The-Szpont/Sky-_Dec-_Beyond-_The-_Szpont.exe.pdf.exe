using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    public static LevelLoader Instance { get; private set; }

    public Animator transition;
    public float transitionTime = 1f;

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
        transition.SetTrigger("Start");
        yield return new WaitForSeconds(transitionTime);
        SceneManager.LoadScene(sceneName);
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
