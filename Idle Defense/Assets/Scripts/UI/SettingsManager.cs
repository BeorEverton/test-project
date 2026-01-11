using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Audio Settings")]
    [Range(-45f, 20f)] public float MusicVolume = 1f;
    [Range(-45f, 20f)] public float SFXVolume = 1f;
    private float savedMusicVolume, savedSFXVolume;
    [Header("Mixer Reference")]
    [SerializeField] private AudioMixer _masterMixer;

    [Header("UI Settings")]
    public bool AllowPopups = true;
    public bool AllowTooltips = true;
    public bool Mute = false;

    [Header("External Links")]
    [SerializeField] private List<ExternalLink> externalLinks;

    private Dictionary<string, string> _linkLookup;

    // Used only for loading
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Toggle muteToggle;
    [SerializeField] private Toggle popupToggle;
    [SerializeField] private Toggle tooltipToggle;
    [SerializeField] private ToggleVisibility settingPanelVisibility;


    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        // Cache links for fast lookup
        _linkLookup = new Dictionary<string, string>();
        foreach (var link in externalLinks)
        {
            if (!_linkLookup.ContainsKey(link.key))
                _linkLookup.Add(link.key, link.url);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UpdateSettingsUI();
            settingPanelVisibility.Toggle();
        }
    }

    public void ShouldShowPopups(bool option)
    {
        AllowPopups = option;
    }

    public void ShouldShowTooltips(bool option)
    {
        AllowTooltips = option;
    }

    public void MuteAll(bool option)
    {
        Mute = option;
        if (option)
        {
            _masterMixer.SetFloat("MusicVolume", -80f);
            _masterMixer.SetFloat("SFXVolume", -80f);
        }
        else
        {
            SetMusicVolume(savedMusicVolume);
            SetSFXVolume(savedSFXVolume);
        }
    }

    public void SetMusicVolume(float sliderValue)
    {
        _masterMixer.SetFloat("MusicVolume", sliderValue);
        MusicVolume = sliderValue;
        savedMusicVolume = sliderValue;
    }

    public void SetSFXVolume(float sliderValue)
    {
        _masterMixer.SetFloat("SFXVolume", sliderValue);
        SFXVolume = sliderValue;
        savedSFXVolume = sliderValue;
    }

    public void OpenExternalLink(string key)
    {
        if (_linkLookup.TryGetValue(key, out string url))
        {
            Application.OpenURL(url);
        }
        else
        {
            Debug.LogWarning($"No link registered with key: {key}");
        }
    }

    public void UpdateSettingsUI()
    {
        musicSlider.SetValueWithoutNotify(MusicVolume);
        sfxSlider.SetValueWithoutNotify(SFXVolume);

        muteToggle.SetIsOnWithoutNotify(Mute);
        popupToggle.SetIsOnWithoutNotify(AllowPopups);
        tooltipToggle.SetIsOnWithoutNotify(AllowTooltips);
    }


    [System.Serializable]
    public class ExternalLink
    {
        public string key;
        public string url;
    }
}
