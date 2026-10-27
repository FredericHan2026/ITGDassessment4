using UnityEngine;

/// <summary>
/// Drives the level's background music.
///
/// On Play the intro music starts. As soon as that clip finishes, or three
/// seconds have gone by - whichever happens first - the normal ghost-state music
/// takes over and loops.
///
/// The timing is done with a plain counter in Update rather than a coroutine, and
/// the AudioSource is left holding the looping track with Play On Awake ticked,
/// so the level still has music even if this component never runs.
/// </summary>
public class AudioManager : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private AudioSource musicSource;

    [Header("Clips")]
    [SerializeField] private AudioClip introMusic;
    [SerializeField] private AudioClip ghostNormalMusic;
    [SerializeField] private AudioClip ghostScaredMusic;
    [SerializeField] private AudioClip ghostDeadMusic;

    [Tooltip("Off when the round drives the music, which is the case in a level: "
             + "the intro then starts and stops with the countdown instead of "
             + "handing over on a timer of its own.")]
    [SerializeField] private bool playOnStart = true;

    [Header("Timing")]
    [Tooltip("Hand over to the looping music after at most this many seconds.")]
    [SerializeField] private float maxIntroSeconds = 3f;

    private float _introElapsed;
    private bool _waitingForHandover;

    private void Awake()
    {
        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();
        }
    }

    private void Start()
    {
        if (musicSource == null || !playOnStart)
        {
            return;
        }

        if (introMusic == null)
        {
            // No intro to play, so go straight to the looping track.
            PlayGhostNormal();
            return;
        }

        musicSource.Stop();
        musicSource.clip = introMusic;
        musicSource.loop = false;
        musicSource.Play();

        _introElapsed = 0f;
        _waitingForHandover = true;
    }

    private void Update()
    {
        if (!_waitingForHandover)
        {
            return;
        }

        _introElapsed += Time.deltaTime;

        // Whichever comes first: the intro finishing, or the time limit.
        bool introFinished = musicSource != null && !musicSource.isPlaying;
        if (_introElapsed < maxIntroSeconds && !introFinished)
        {
            return;
        }

        _waitingForHandover = false;
        PlayGhostNormal();
    }

    /// <summary>
    /// Starts the intro, once, without looping. The round calls this as the
    /// countdown begins and calls PlayGhostNormal when it ends.
    /// </summary>
    public void PlayIntro()
    {
        if (musicSource == null || introMusic == null)
        {
            return;
        }

        _waitingForHandover = false;
        musicSource.Stop();
        musicSource.clip = introMusic;
        musicSource.loop = false;
        musicSource.Play();
    }

    /// <summary>Normal seal-state music, looping. Used from Assessment 2 onwards too.</summary>
    public void PlayGhostNormal()
    {
        // Falling back to the intro keeps the level from going silent if the
        // looping track is ever missing.
        PlayLooping(ghostNormalMusic != null ? ghostNormalMusic : introMusic);
    }

    public void PlayGhostScared()
    {
        PlayLooping(ghostScaredMusic);
    }

    public void PlayGhostDead()
    {
        PlayLooping(ghostDeadMusic);
    }

    private void PlayLooping(AudioClip clip)
    {
        if (musicSource == null || clip == null)
        {
            return;
        }

        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }
}
