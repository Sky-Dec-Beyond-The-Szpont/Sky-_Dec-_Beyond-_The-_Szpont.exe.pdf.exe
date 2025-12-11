using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class IntroVideoController : MonoBehaviour
{
    public VideoPlayer videoPlayer;

    // Nazwa sceny, która ma siê za³adowaæ po filmie
    public string nextSceneName = "MapScene";

    private void Start()
    {
        // Subskrypcja zdarzenia koñca wideo
        videoPlayer.loopPointReached += OnVideoEnd;
    }

    private void OnDestroy()
    {
        // Dobra praktyka: odsubskrybowanie
        videoPlayer.loopPointReached -= OnVideoEnd;
    }

    private void OnVideoEnd(VideoPlayer vp)
    {
        // Prze³¹czenie sceny po zakoñczeniu filmu
        SceneManager.LoadScene(nextSceneName);
        // ALBO przez indeks, jeœli wolisz:
        // SceneManager.LoadScene(1);
    }
}
