using UnityEngine;
using UnityEngine.SceneManagement;

public class AxeSoundTrigger : MonoBehaviour
{
    public SoundManager soundManager;
    private bool soundPlayed = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (soundPlayed) return;

        // sprawdzamy czy to stó³
        if (collision.gameObject.CompareTag("Table"))
        {
            soundManager.PlayEndGame();
            soundPlayed = true;
        } 
    }
}
