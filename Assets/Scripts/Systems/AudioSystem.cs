using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Ajedrez.Systems
{
    public class AudioSystem : MonoBehaviour
    {
        public enum SoundType
        {
            Move,
            Capture,
            Castle,
            Promotion,
            InCheck
        }

        public enum MusicType
        {
            Menu
        }

        public static AudioSystem Instance { get; private set; }

        // Constants
        private const float MIN_PITCH = 0.9f;
        private const float MAX_PITCH = 1.1f;

        // Audio sources
        private AudioSource soundSource;
        private AudioSource randomSoundSource;
        private AudioSource musicSource;

        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer audioMixer;

        [Header("Mixer Groups")]
        [SerializeField] private AudioMixerGroup effectsMixerGroup;
        [SerializeField] private AudioMixerGroup musicMixerGroup;

        // Audio clips
        [Header("Sounds")]
        [SerializeField] private AudioClip moveSound;
        [SerializeField] private AudioClip captureSound;
        [SerializeField] private AudioClip castleSound;
        [SerializeField] private AudioClip promotionSound;
        [SerializeField] private AudioClip checkSound;

        [Header("Music")]
        [SerializeField] private AudioClip menuMusic;

        private Dictionary<SoundType, AudioClip> sounds;
        private Dictionary<MusicType, AudioClip> musics;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                // Create and configure the AudioSource instances in code
                soundSource = gameObject.AddComponent<AudioSource>();
                randomSoundSource = gameObject.AddComponent<AudioSource>();
                musicSource = gameObject.AddComponent<AudioSource>();
                soundSource.playOnAwake = false;
                randomSoundSource.playOnAwake = false;
                musicSource.playOnAwake = false;
                musicSource.loop = true;

                // Assign Mixer Groups
                soundSource.outputAudioMixerGroup = effectsMixerGroup;
                randomSoundSource.outputAudioMixerGroup = effectsMixerGroup;
                musicSource.outputAudioMixerGroup = musicMixerGroup;

                InitializeSounds();
                InitializeMusic();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeSounds()
        {
            sounds = new Dictionary<SoundType, AudioClip>
            {
                { SoundType.Move, moveSound },
                { SoundType.Capture, captureSound },
                { SoundType.Castle, castleSound },
                { SoundType.Promotion, promotionSound },
                { SoundType.InCheck, checkSound }
            };
        }

        private void InitializeMusic()
        {
            musics = new Dictionary<MusicType, AudioClip>
            {
                { MusicType.Menu, menuMusic }
            };
        }

        public void PlaySound(SoundType sound)
        {
            soundSource.PlayOneShot(sounds[sound]);
        }

        public void PlaySoundWithRandomPitch(SoundType sound, float min = MIN_PITCH, float max = MAX_PITCH)
        {
            randomSoundSource.pitch = Random.Range(min, max);
            randomSoundSource.PlayOneShot(sounds[sound]);
        }

        public void PlayMusic(MusicType music)
        {
            musicSource.clip = musics[music];
            musicSource.Play();
        }

        public void SetGeneralVolume(float volume)
        {
            audioMixer.SetFloat("volumenGeneral", Mathf.Log10(volume) * 20f);
        }

        public void SetEffectsVolume(float volume)
        {
            audioMixer.SetFloat("volumenEfectos", Mathf.Log10(volume) * 20f);
        }

        public void SetMusicVolume(float volume)
        {
            audioMixer.SetFloat("volumenMusica", Mathf.Log10(volume) * 20f);
        }

        public float GetGeneralVolume()
        {
            audioMixer.GetFloat("volumenGeneral", out float valueDb);
            return Mathf.Pow(10f, valueDb / 20f);
        }

        public float GetEffectsVolume()
        {
            audioMixer.GetFloat("volumenEfectos", out float valueDb);
            return Mathf.Pow(10f, valueDb / 20f);
        }

        public float GetMusicVolume()
        {
            audioMixer.GetFloat("volumenMusica", out float valueDb);
            return Mathf.Pow(10f, valueDb / 20f);
        }
    }
}