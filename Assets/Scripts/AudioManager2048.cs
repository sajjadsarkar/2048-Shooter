using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class AudioManager2048 : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource effectsSource;
    [SerializeField] private AudioSource musicSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip tilePlacedSound;
    [SerializeField] private AudioClip tileMergeSound;
    [SerializeField] private List<AudioClip> specialMergeClips; // For special values (2048, etc.)

    [Header("Settings")]
    [SerializeField] private float mergeVolumeMultiplier = 0.7f;
    [SerializeField] private float pitchIncrementPerCombo = 0.05f; // Increment pitch by this amount per combo
    [SerializeField] private int maxComboPitch = 10; // Maximum combo count for pitch increase
    [SerializeField] private bool useSpecialMergeSounds = true;
    [SerializeField][Range(0f, 1f)] private float masterVolume = 1.0f;

    // Internal variables
    private float basePitch = 1.0f;
    private int currentComboCount = 0;

    // Singleton instance
    public static AudioManager2048 Instance { get; private set; }

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Create audio sources if they don't exist
        if (effectsSource == null)
        {
            effectsSource = gameObject.AddComponent<AudioSource>();
            effectsSource.playOnAwake = false;
        }

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = true;
        }
    }

    private void Start()
    {
        UpdateVolume();
        ResetPitch();
    }

    // Play sound when a tile is placed
    public void PlayTilePlacedSound()
    {
        if (tilePlacedSound != null && effectsSource != null)
        {
            // Always play placed sound at base pitch
            float originalPitch = effectsSource.pitch;
            effectsSource.pitch = basePitch;
            effectsSource.PlayOneShot(tilePlacedSound, masterVolume);
            effectsSource.pitch = originalPitch; // Restore original pitch
        }
    }

    // Play sound when tiles merge
    public void PlayMergeSound(int mergedValue = 0, int comboCount = 0)
    {
        if (effectsSource == null) return;

        // Update combo count for pitch calculation
        UpdateComboCount(comboCount);

        // Handle special value merges
        if (useSpecialMergeSounds && mergedValue >= 2048 && specialMergeClips.Count > 0)
        {
            // Choose special clip based on the value
            int clipIndex = Mathf.Min(
                Mathf.FloorToInt(Mathf.Log(mergedValue / 2048, 2)),
                specialMergeClips.Count - 1
            );

            // Special sounds also get pitch adjustment
            effectsSource.pitch = CalculatePitch();
            effectsSource.PlayOneShot(specialMergeClips[clipIndex], masterVolume);
            return;
        }

        // Regular merge sound with pitch adjustment
        if (tileMergeSound != null)
        {
            effectsSource.pitch = CalculatePitch();
            effectsSource.PlayOneShot(tileMergeSound, masterVolume * mergeVolumeMultiplier);
        }
    }

    // Calculate pitch based on combo count
    private float CalculatePitch()
    {
        // Limit the combo count used for pitch calculation
        int effectiveComboCount = Mathf.Min(currentComboCount, maxComboPitch);

        // Only increase pitch if there's a combo (count > 0)
        if (currentComboCount > 0)
        {
            return basePitch + (pitchIncrementPerCombo * effectiveComboCount);
        }

        return basePitch;
    }

    // Update the combo count for pitch calculation
    private void UpdateComboCount(int comboCount)
    {
        // Only update if this is an actual combo
        if (comboCount > 0)
        {
            currentComboCount = comboCount;
        }
        else
        {
            // If combo is broken, reset pitch
            ResetPitch();
        }
    }

    // Reset the pitch to base value
    public void ResetPitch()
    {
        currentComboCount = 0;
        effectsSource.pitch = basePitch;
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateVolume();
    }

    private void UpdateVolume()
    {
        if (effectsSource != null)
        {
            effectsSource.volume = masterVolume;
        }

        if (musicSource != null)
        {
            musicSource.volume = masterVolume * 0.7f; // Slightly lower for music
        }
    }

    // Optional method to play background music
    public void PlayMusic(AudioClip musicClip)
    {
        if (musicSource != null && musicClip != null)
        {
            musicSource.clip = musicClip;
            musicSource.Play();
        }
    }

    // Toggle sound effects on/off
    public void ToggleSoundEffects(bool enabled)
    {
        effectsSource.mute = !enabled;
    }

    // Toggle music on/off
    public void ToggleMusic(bool enabled)
    {
        musicSource.mute = !enabled;
    }
}
