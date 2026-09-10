using UnityEngine;
using System.Collections.Generic;

public class SFXManager : MonoBehaviour
{

    public static SFXManager instance;
    [SerializeField] private AudioSource SFXObject;
    [SerializeField] private float FXVolume = 1f;
    [SerializeField] private float MusicVolume = 1f;

    [SerializeField] private List<AudioClip> BonkSound;
    [SerializeField] private List<AudioClip> DeathSound;
    [SerializeField] private List<AudioClip> Music;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    private void OnEnable()
    {
        EventBus.WallSideHitEvent += PlayBonkSFX;
        EventBus.DeathEvent += PlayDeathSFX;
    }

    private void OnDisable()
    {
        EventBus.WallSideHitEvent -= PlayBonkSFX;
        EventBus.DeathEvent -= PlayDeathSFX;
    }
    
    private void PlaySound(AudioClip clip, float volume)
    {
        AudioSource audioSource = Instantiate(SFXObject);
        // if (audioSource.name == "SFXObject(Clone)")
        // {
        //     audioSource.name = "SFX_" + System.DateTime.Now.ToString("mmssfff");
        // }
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.Play();
        Destroy(audioSource.gameObject, clip.length);
    }

    public void PlayBonkSFX(bool t=false)
    {
        int x = Random.Range(0, BonkSound.Count);
        // Debug.Log(string.Format("{0} the {1} audio count is {2}", x, "bonk", BonkSound.Count));
        AudioClip clip = BonkSound[x];
        PlaySound(clip, FXVolume);
    }

    public void PlayDeathSFX()
    {
        int x = Random.Range(0, DeathSound.Count);
        // Debug.Log(string.Format("{0} the {1} audio count is {2}", x, "death", DeathSound.Count));
        AudioClip clip = DeathSound[x];
        PlaySound(clip, FXVolume);
    }

    public void PlayMusic()
    {
        
    }
}
