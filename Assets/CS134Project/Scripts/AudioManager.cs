using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public AudioMixer audioMixer;

    // Audio slider logic
    public void SetVolume(float value)
    {
        // Log scaling for audio
        float volume = Mathf.Log10(value) * 20;
        audioMixer.SetFloat("MasterVolume", volume);
    }
}