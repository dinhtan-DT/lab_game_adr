using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    private AudioSource bgmSource;
    private AudioSource sfxSource;
    private AudioSource warningSource;

    private bool isMusicMuted = false;
    private bool isSoundMuted = false;

    private AudioClip shootClip;
    private AudioClip warningClip;
    private AudioClip explosionClip;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
            GenerateSimulatedSounds();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAudioSources()
    {
        AudioSource[] existingSources = GetComponents<AudioSource>();
        if (existingSources != null && existingSources.Length >= 3)
        {
            bgmSource = existingSources[0];
            sfxSource = existingSources[1];
            warningSource = existingSources[2];
            return;
        }

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.volume = 0.5f;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.volume = 0.8f;

        warningSource = gameObject.AddComponent<AudioSource>();
        warningSource.loop = false;
        warningSource.volume = 1f;
    }

    private void GenerateSimulatedSounds()
    {
        shootClip = AudioClip.Create("Shoot", 44100 / 4, 1, 44100, false);
        float[] shootData = new float[44100 / 4];
        for (int i = 0; i < shootData.Length; i++)
        {
            shootData[i] = Mathf.Sin(2 * Mathf.PI * 800 * i / 44100f) * Mathf.Exp(-i * 0.001f);
        }
        shootClip.SetData(shootData, 0);

        warningClip = AudioClip.Create("Warning", 44100 / 4, 1, 44100, false);
        float[] warnData = new float[44100 / 4];
        for (int i = 0; i < warnData.Length; i++)
        {
            warnData[i] = Mathf.Sin(2 * Mathf.PI * 1200 * i / 44100f) * Mathf.Exp(-i * 0.0005f);
        }
        warningClip.SetData(warnData, 0);

        explosionClip = AudioClip.Create("Explode", 44100 / 2, 1, 44100, false);
        float[] expData = new float[44100 / 2];
        for (int i = 0; i < expData.Length; i++)
        {
            expData[i] = (Random.value * 2f - 1f) * Mathf.Exp(-i * 0.0002f);
        }
        explosionClip.SetData(expData, 0);

        // Tạo nhạc nền (BGM) điện tử 8-bit lặp lại (dài 4 giây)
        AudioClip bgmClip = AudioClip.Create("BGM", 44100 * 4, 1, 44100, false);
        float[] bgmData = new float[44100 * 4];
        float[] notes = { 220f, 261.63f, 329.63f, 392.00f }; // A3, C4, E4, G4
        for (int i = 0; i < bgmData.Length; i++)
        {
            float time = i / 44100f;
            // Thay đổi nốt nhạc mỗi 0.25 giây
            int noteIndex = Mathf.FloorToInt(time * 4) % notes.Length;
            float freq = notes[noteIndex];
            
            // Tạo sóng vuông (square wave) đặc trưng của retro 8-bit
            float wave = Mathf.Sign(Mathf.Sin(2 * Mathf.PI * freq * time));
            
            // Thêm phách trống kick drum ảo mỗi 0.5 giây
            float kickEnv = Mathf.Exp(-(time % 0.5f) * 10f);
            float kick = Mathf.Sin(2 * Mathf.PI * 100f * (time % 0.5f)) * kickEnv;

            bgmData[i] = (wave * 0.15f + kick * 0.3f) * 0.5f; // Giảm âm lượng 50%
        }
        bgmClip.SetData(bgmData, 0);

        if (bgmSource != null)
        {
            bgmSource.clip = bgmClip;
            if (!isMusicMuted) bgmSource.Play();
        }
    }

    public void PlayBGM(AudioClip clip = null)
    {
        if (clip != null) bgmSource.clip = clip;
        if (!isMusicMuted && !bgmSource.isPlaying && bgmSource.clip != null)
        {
            bgmSource.Play();
        }
    }

    public void ToggleMusic()
    {
        isMusicMuted = !isMusicMuted;
        bgmSource.mute = isMusicMuted;
    }
    
    public void SetMusicMute(bool mute)
    {
        isMusicMuted = mute;
        bgmSource.mute = mute;
    }

    public void ToggleSound()
    {
        isSoundMuted = !isSoundMuted;
        sfxSource.mute = isSoundMuted;
        warningSource.mute = isSoundMuted;
        Debug.Log("[AudioManager] ToggleSound. isSoundMuted = " + isSoundMuted);
    }

    public void SetSoundMute(bool mute)
    {
        isSoundMuted = mute;
        sfxSource.mute = mute;
        warningSource.mute = mute;
    }

    public bool IsMusicMuted => isMusicMuted;
    public bool IsSoundMuted => isSoundMuted;

    public void PlayShoot()
    {
        if (isSoundMuted) return;
        if (shootClip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(shootClip);
        }
    }

    public void PlayJump()
    {
        if (isSoundMuted) return;
        if (shootClip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(shootClip, 0.7f);
        }
    }

    public void PlayExplosion()
    {
        if (!isSoundMuted && explosionClip != null)
        {
            sfxSource.PlayOneShot(explosionClip);
        }
    }

    private bool isWarningPlaying = false;
    public void PlayWarningBeeps()
    {
        if (!isWarningPlaying && !isSoundMuted)
        {
            StartCoroutine(WarningRoutine());
        }
    }

    private IEnumerator WarningRoutine()
    {
        isWarningPlaying = true;
        int beeps = Random.Range(3, 7); // 3 to 6
        for (int i = 0; i < beeps; i++)
        {
            if (!isSoundMuted && warningClip != null)
            {
                warningSource.PlayOneShot(warningClip);
            }
            yield return new WaitForSeconds(0.4f);
        }
        isWarningPlaying = false;
    }
}
