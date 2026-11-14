using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] private AudioClip musicClip;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.4f;

    [Header("SFX")]
    [SerializeField] private int sfxVoices = 16;                         // max simultaneous sound effects
    [SerializeField, Range(0f, 0.5f)] private float pitchVariation = 0.1f; // +/- around normal pitch (0.1 = roughly +/- 1.5 semitones)
    [SerializeField] private float minRepeatInterval = 0.05f;            // same clip can't retrigger faster than this (stops volume stacking)
    [SerializeField, Range(0f, 1f)] private float sfxMasterVolume = 1f;

    private AudioSource musicSource;
    private AudioSource[] voices;
    private int nextVoice;
    private readonly Dictionary<AudioClip, float> lastPlayed = new Dictionary<AudioClip, float>();

    private void Awake()
    {
        // Singleton: keep the first instance, destroy any duplicate
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f; // 2D
        musicSource.volume = musicVolume;

        voices = new AudioSource[sfxVoices];
        for (int i = 0; i < sfxVoices; i++)
        {
            voices[i] = gameObject.AddComponent<AudioSource>();
            voices[i].playOnAwake = false;
            voices[i].spatialBlend = 0f;
        }
    }

    private void Start()
    {
        PlayMusic(musicClip);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ---------- Static helper: safe to call from anywhere, even with no AudioManager or no clip ----------

    /// <param name="pitchVar">Override the default variation. Pass 0 for sounds that must always sound identical (e.g. a jingle). Negative = use default.</param>
    public static void Play(AudioClip clip, float volume = 1f, float pitchVar = -1f)
    {
        if (Instance != null) Instance.PlaySFX(clip, volume, pitchVar);
    }

    // ---------- SFX ----------

    public void PlaySFX(AudioClip clip, float volume = 1f, float pitchVar = -1f)
    {
        if (clip == null || voices == null) return;

        // Unscaled time, so the limit still works while the game is paused
        float now = Time.unscaledTime;
        if (lastPlayed.TryGetValue(clip, out float last) && now - last < minRepeatInterval) return;
        lastPlayed[clip] = now;

        AudioSource src = GetVoice();
        float v = pitchVar < 0f ? pitchVariation : pitchVar;

        src.pitch = 1f + Random.Range(-v, v);
        src.volume = volume * sfxMasterVolume;
        src.clip = clip;
        src.Play();
    }

    // Prefer an idle voice; if all are busy, steal the next one round-robin
    private AudioSource GetVoice()
    {
        for (int i = 0; i < voices.Length; i++)
        {
            int idx = (nextVoice + i) % voices.Length;
            if (!voices[idx].isPlaying)
            {
                nextVoice = (idx + 1) % voices.Length;
                return voices[idx];
            }
        }

        AudioSource stolen = voices[nextVoice];
        nextVoice = (nextVoice + 1) % voices.Length;
        return stolen;
    }

    // ---------- Music ----------

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void StopMusic() => musicSource.Stop();
}
