using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Assets.Scripts.Systems.Audio
{
    /// <summary>
    /// Used to play all sounds, musics excluded because this is just a prototype with a single music.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        // Holds all sounds in an array
        public Sound[] sounds;
        public Sound[] musics;
        string musicPlaying;
        private float lastClipLenght;
        private float lastPlayed;
        private string lastClip;
        public AudioMixer audioMixer;
        // Created these variables to save them
        public float musicVolume = -20f;
        public float SFXVolume;
        private bool mute = true; // Used only to not play all sounds when the game is loaded. It is set to false on the game load.

        #region Simple Singleton
        public static AudioManager Instance { get; private set; }
        void Awake()
        {
            if (!Instance)
                Instance = this;
            else
                Destroy(this);
            #endregion

            // On start add all sounds to the array with it's respective values
            foreach (Sound s in sounds)
            {
                s.source = gameObject.AddComponent<AudioSource>();
                s.source.outputAudioMixerGroup = audioMixer.FindMatchingGroups("SFX")[0];
                s.source.clip = s.clip;
                s.source.volume = s.volume;
                s.source.pitch = s.pitch;
                s.source.loop = s.loop;
                s.source.playOnAwake = false;
            }
            foreach (Sound s in musics)
            {
                s.source = gameObject.AddComponent<AudioSource>();
                s.source.outputAudioMixerGroup = audioMixer.FindMatchingGroups("Music")[0];
                s.source.clip = s.clip;
                s.source.volume = s.volume;
                s.source.pitch = s.pitch;
                s.source.loop = s.loop;
                s.source.playOnAwake = false;
            }

            PlayMusic("Main Theme");

            //Debug.Log(AudioListener.volume);
        }

        private void Start()
        {
            PlayMusic("Main");
            mute = false;
        }

        // Play can be called from any script
        public void Play(string name)
        {
            if (mute)
                return;
            //Debug.Log("calling sound " + name);
            Sound s = Array.Find(sounds, sound => sound.name == name); // Search the sound   
            if (s != null)
            {
                s.source.PlayOneShot(s.clip); // If found play it
                lastClip = name;
                lastClipLenght = s.clip.length;
                lastPlayed = Time.time;
            }
            else
                Debug.LogWarning("Sound: " + s + " not found");
        }

        /// <summary>
        /// Plays a sound with slight pitch and volume variation to prevent audio fatigue.
        /// </summary>
        /// <param name="name">The name of the sound to play.</param>
        /// <param name="pitchRange">Range of pitch variation (e.g., 0.05f = ±5%).</param>
        /// <param name="volumeRange">Range of volume variation (e.g., 0.1f = ±10%).</param>
        public void PlayWithVariation(string name, float pitchRange = 0.05f, float volumeRange = 0.1f)
        {
            if (mute)
                return;

            Sound s = Array.Find(sounds, sound => sound.name == name);
            if (s != null)
            {
                float originalPitch = s.source.pitch;
                float originalVolume = s.source.volume;

                s.source.pitch = originalPitch * UnityEngine.Random.Range(1f - pitchRange, 1f + pitchRange);
                s.source.volume = Mathf.Clamp(originalVolume * UnityEngine.Random.Range(1f - volumeRange, 1f + volumeRange), 0f, 1f);

                s.source.PlayOneShot(s.clip);

                s.source.pitch = originalPitch; // Reset to original values
                s.source.volume = originalVolume;
            }
            else
            {
                Debug.LogWarning("Sound: " + name + " not found");
            }
        }


        public void PlayMusic(string name)
        {
            if (!string.IsNullOrEmpty(musicPlaying))
                StopMusic(musicPlaying); // First stop the music curently playing

            //Debug.Log("calling music " + name);
            Sound s = Array.Find(musics, sound => sound.name == name); // Search the sound   
            if (s != null)
            {
                s.source.Play(); // If found play it
                musicPlaying = name;
            }
            else
                Debug.LogWarning("Sound: " + s + " not found");
        }

        public void StopMusic(string name)
        {
            Sound s = Array.Find(musics, sound => sound.name == name);
            if (s == null)
            {
                Debug.LogWarning("Sound: " + s + " not found");
                return;
            }
            s.source.Stop();
        }

        public void Stop(string name)
        {
            Sound s = Array.Find(sounds, sound => sound.name == name);
            if (s == null)
            {
                Debug.LogWarning("Sound: " + s + " not found");
                return;
            }
            s.source.Stop();
        }

        public void StopAllMusics()
        {
            for (int i = 0; i < musics.Length; i++)
            {
                musics[i].source.Stop();
            }
        }

        // Just for editor scripts
        public void PlayClick()
        {
            Play("Click");
        }
    }
}