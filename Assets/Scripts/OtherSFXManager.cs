using UnityEngine;

public class OtherSFXManager : MonoBehaviour
{
    public static OtherSFXManager Instance;

    [SerializeField]
    private AudioClip earthquakeSound;
    
    private AudioSource _audioSource;
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
    }

    public void PlayEarthQuakeEffect()
    {
        _audioSource.PlayOneShot(earthquakeSound);
    }
}
