using UnityEngine;

/// <summary>
/// Holds every one-shot sound effect in the game and plays them through a single
/// shared AudioSource, so the rest of the code never has to know about clips.
/// </summary>
public class SfxLibrary : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Clips")]
    [SerializeField] private AudioClip eatPellet;
    [SerializeField] private AudioClip eatGhost;
    [SerializeField] private AudioClip eatCherry;
    [SerializeField] private AudioClip wallCollision;
    [SerializeField] private AudioClip pacStudentDeath;

    private void Awake()
    {
        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
        }
    }

    public void PlayEatPellet()
    {
        Play(eatPellet);
    }

    public void PlayEatGhost()
    {
        Play(eatGhost);
    }

    public void PlayEatCherry()
    {
        Play(eatCherry);
    }

    public void PlayWallCollision()
    {
        Play(wallCollision);
    }

    public void PlayDeath()
    {
        Play(pacStudentDeath);
    }

    private void Play(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }
}
