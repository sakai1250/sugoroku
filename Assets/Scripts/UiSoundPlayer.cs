using UnityEngine;

public class UiSoundPlayer : MonoBehaviour
{
    public AudioClip clickClip;
    public AudioClip confirmClip;
    public AudioClip errorClip;

    private AudioSource audioSource;

    public static UiSoundPlayer Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
    }

    public void PlayClick()
    {
        Play(clickClip);
    }

    public void PlayConfirm()
    {
        Play(confirmClip);
    }

    public void PlayError()
    {
        Play(errorClip);
    }

    private void Play(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
