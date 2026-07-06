using UnityEngine;
using UnityEngine.Audio;

namespace SpeedRush
{
    public class SR_BGMManager : MonoBehaviour
    {
        public static SR_BGMManager Instance { get; private set; }

        [Header("Audio Settings")]
        public AudioClip bgmClip;
        public AudioMixerGroup musicMixerGroup;
        [Range(0f, 1f)]
        public float defaultVolume = 0.5f;

        private AudioSource audioSource;
        private const string MusicVolumeParam = "MusicVolume";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            SetupAudioSource();
            GenerateBGMIfNeeded();
        }

        private void Start()
        {
            if (!audioSource.isPlaying && audioSource.clip != null)
            {
                audioSource.Play();
            }
        }

        private void SetupAudioSource()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.clip = bgmClip;
            audioSource.outputAudioMixerGroup = musicMixerGroup;
            audioSource.volume = defaultVolume;
            audioSource.priority = 0;
        }

        private void GenerateBGMIfNeeded()
        {
            if (bgmClip == null)
            {
                Debug.LogWarning("[BGMManager] No BGM clip assigned, generating procedural BGM...");
                GenerateProceduralBGM();
            }
        }

        public void SetVolume(float normalizedVolume)
        {
            if (audioSource != null)
            {
                audioSource.volume = normalizedVolume;
            }
        }

        public void SetMixerVolume(float normalizedVolume)
        {
            if (musicMixerGroup != null && musicMixerGroup.audioMixer != null)
            {
                float dbVolume = normalizedVolume > 0.001f ? Mathf.Log10(normalizedVolume) * 20f : -80f;
                musicMixerGroup.audioMixer.SetFloat(MusicVolumeParam, dbVolume);
            }
            else
            {
                SetVolume(normalizedVolume);
            }
        }

        public float GetVolume()
        {
            return audioSource != null ? audioSource.volume : defaultVolume;
        }

        private void GenerateProceduralBGM()
        {
            int sampleRate = 44100;
            float duration = 8f;
            int sampleCount = sampleRate * (int)duration;
            float[] samples = new float[sampleCount];

            float bpm = 140f;
            float beatDuration = 60f / bpm;
            float bassFreq = 55f;
            float hihatInterval = beatDuration / 2f;
            float bassInterval = beatDuration;

            for (int i = 0; i < sampleCount; i++)
            {
                float time = (float)i / sampleRate;
                float sample = 0f;

                float beatPhase = time % bassInterval;
                if (beatPhase < 0.08f)
                {
                    float env = 1f - (beatPhase / 0.08f);
                    sample += Mathf.Sin(2f * Mathf.PI * bassFreq * time) * env * 0.4f;
                    float subFreq = bassFreq * 0.5f;
                    sample += Mathf.Sin(2f * Mathf.PI * subFreq * time) * env * 0.3f;
                }

                float hihatPhase = time % hihatInterval;
                if (hihatPhase < 0.015f)
                {
                    float hihatEnv = 1f - (hihatPhase / 0.015f);
                    float noise = (Random.value * 2f - 1f) * hihatEnv * 0.15f;
                    sample += noise;
                }

                float kickPattern = bassInterval * 4f;
                float kickPhase = time % kickPattern;
                if (kickPhase < 0.12f)
                {
                    float kickEnv = 1f - (kickPhase / 0.12f);
                    float kickFreq = Mathf.Lerp(150f, bassFreq, kickPhase / 0.12f);
                    sample += Mathf.Sin(2f * Mathf.PI * kickFreq * time) * kickEnv * 0.35f;
                }

                float synthInterval = beatDuration * 2f;
                float synthPhase = time % synthInterval;
                if (synthPhase < beatDuration * 0.8f)
                {
                    float synthEnv = 1f - (synthPhase / (beatDuration * 0.8f));
                    float freq1 = 220f;
                    float freq2 = 330f;
                    float freq3 = 440f;
                    sample += Mathf.Sin(2f * Mathf.PI * freq1 * time) * synthEnv * 0.08f;
                    sample += Mathf.Sin(2f * Mathf.PI * freq2 * time) * synthEnv * 0.06f;
                    sample += Mathf.Sin(2f * Mathf.PI * freq3 * time) * synthEnv * 0.04f;
                }

                samples[i] = Mathf.Clamp(sample, -1f, 1f);
            }

            bgmClip = AudioClip.Create("SpeedRush_BGM_Procedural", sampleCount, 1, sampleRate, false);
            bgmClip.SetData(samples, 0);
            audioSource.clip = bgmClip;

            Debug.Log("[BGMManager] Procedural BGM generated: " + duration + "s loop.");
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
