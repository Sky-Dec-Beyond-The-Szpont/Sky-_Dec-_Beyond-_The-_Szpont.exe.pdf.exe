using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [Header("AudioSource (single source for SFX)")]
    public AudioSource audioSource;

    [Header("AudioSource (Music)")]
    public AudioSource musicSource;
    public float musicVolume = 0.05f;

    private AudioClip[] musicClips;
    public int currentTrackIndex = 0;

    [Header("Card events")]
    public AudioClip drawCardSound;
    public AudioClip initialDrawCards;
    public AudioClip selectCardSound;     
    public AudioClip playCardSound;       
    public AudioClip endTurnSound;        
    public AudioClip cardDeathSound;      
    public AudioClip weightChangeSound;  

    [Header("Game outcome")]
    public AudioClip endgameSound;                 

    public void PlayDrawCard() => PlaySFX(drawCardSound);
    public void PlayInitialDraw() => PlaySFX(initialDrawCards);
    public void PlaySelectCard() => PlaySFX(selectCardSound, volumeScale: 0.1f);
    public void PlayPlayCard() => PlaySFX(playCardSound);
    public void PlayEndTurn() => PlaySFX(endTurnSound, volumeScale: 0.01f);
    public void PlayCardDeath() => PlaySFX(cardDeathSound);
    public void PlayWeightChange() => PlaySFX(weightChangeSound);
    public void PlayEndGame() => PlaySFX(endgameSound);

    private void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (audioSource == null || clip == null) return;
        audioSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }


    void Start()
    {
        // wczytujemy soundtracki z Resources/Sounds/soundtracks
        musicClips = Resources.LoadAll<AudioClip>("Sounds/soundtracks");

        if (musicSource != null && musicClips.Length > 0)
        {
            musicSource.volume = musicVolume;
            PlayNextTrack();
        }
    }

    void Update()
    {
        if (musicSource != null && !musicSource.isPlaying)
        {
            PlayNextTrack();
        }
    }

    void PlayNextTrack()
    {
        if (musicClips.Length == 0) return;

        musicSource.clip = musicClips[currentTrackIndex];
        musicSource.Play();
    }
}
