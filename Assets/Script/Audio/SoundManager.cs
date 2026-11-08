using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Plays all sounds and music. Any script can just write: SoundManager.Play(Sound.Coin);
//
// You never place it in a scene: it creates itself when the game starts
// ([RuntimeInitializeOnLoadMethod]) and survives scene changes (DontDestroyOnLoad),
// so the music doesn't restart when you go from the menu to the game.
// The sound files come from the SoundLibrary asset in a Resources folder.
public class SoundManager : MonoBehaviour
{
    const int SoundSources = 10;            // up to 10 sounds can play at the same moment
    const float MinTimeBetweenSame = 0.05f; // the same sound at most 20 times a second
    const float MusicFadeTime = 1f;

    private static SoundManager instance;

    private SoundLibrary library;
    private AudioSource[] soundSources;
    private int nextSource;               // which source plays the next sound (we take turns)
    private float[] lastPlayed;           // per sound: when it was last played
    private AudioSource[] musicSources;   // two, so the old song can fade out while the new one fades in
    private int currentMusic;
    private Coroutine musicFade;

    // Runs by itself once, before the first scene loads
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void CreateOnStart()
    {
        GameObject managerObject = new GameObject("SoundManager");
        DontDestroyOnLoad(managerObject);
        instance = managerObject.AddComponent<SoundManager>();
    }

    void Awake()
    {
        library = Resources.Load<SoundLibrary>("SoundLibrary");
        if (library == null) Debug.LogWarning("No SoundLibrary found in a Resources folder, so the game is silent.");

        soundSources = new AudioSource[SoundSources];
        for (int i = 0; i < SoundSources; i++)
        {
            soundSources[i] = gameObject.AddComponent<AudioSource>();
            soundSources[i].playOnAwake = false;
        }

        musicSources = new AudioSource[2];
        for (int i = 0; i < 2; i++)
        {
            musicSources[i] = gameObject.AddComponent<AudioSource>();
            musicSources[i].playOnAwake = false;
            musicSources[i].loop = true;
            musicSources[i].volume = 0f;
        }

        // Start far in the past, so every sound may play right away
        lastPlayed = new float[System.Enum.GetValues(typeof(Sound)).Length];
        for (int i = 0; i < lastPlayed.Length; i++) lastPlayed[i] = -1f;

        // Things that happen anywhere in the game, heard from here (all static events)
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnemyBoss.Appeared += OnBossAppeared;
        EnemyBoss.Disappeared += OnBossGone;
        PlayerHealth.Died += OnPlayerDied;
        ChapterManager.ChapterCleared += OnChapterCleared;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        EnemyBoss.Appeared -= OnBossAppeared;
        EnemyBoss.Disappeared -= OnBossGone;
        PlayerHealth.Died -= OnPlayerDied;
        ChapterManager.ChapterCleared -= OnChapterCleared;
    }

    // Plays a sound once. 'randomPitch' makes it a tiny bit higher or lower each time,
    // so 50 arrows in a row don't sound like a machine gun of the same file.
    public static void Play(Sound sound, bool randomPitch = false)
    {
        if (instance == null || instance.library == null) return;
        instance.PlaySound(sound, randomPitch);
    }

    void PlaySound(Sound sound, bool randomPitch)
    {
        if (library == null) return;

        AudioClip[] clips = library.Get(sound);
        if (clips == null || clips.Length == 0) return; // no file for this sound (yet)

        // Unscaled time: still counts while the game is paused (skill cards, wheel)
        int index = (int)sound;
        if (Time.unscaledTime - lastPlayed[index] < MinTimeBetweenSame) return;
        lastPlayed[index] = Time.unscaledTime;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;

        // Take turns with the sources, so a new sound never cuts off the one before it
        AudioSource source = soundSources[nextSource];
        nextSource = (nextSource + 1) % SoundSources;

        source.pitch = randomPitch ? Random.Range(0.9f, 1.1f) : 1f;
        source.PlayOneShot(clip, library.soundVolume);
    }

    // Fades from the current song to 'clip' (null = fade to silence)
    void PlayMusic(AudioClip clip)
    {
        if (library == null) return;

        AudioSource current = musicSources[currentMusic];
        if (clip != null && current.clip == clip && current.isPlaying) return; // already playing

        if (musicFade != null) StopCoroutine(musicFade);
        musicFade = StartCoroutine(FadeMusic(clip));
    }

    IEnumerator FadeMusic(AudioClip clip)
    {
        AudioSource oldMusic = musicSources[currentMusic];
        currentMusic = 1 - currentMusic; // 0 -> 1, 1 -> 0
        AudioSource newMusic = musicSources[currentMusic];

        newMusic.clip = clip;
        newMusic.volume = 0f;
        if (clip != null) newMusic.Play();

        float oldStartVolume = oldMusic.volume;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / MusicFadeTime;
            oldMusic.volume = Mathf.Lerp(oldStartVolume, 0f, t);
            newMusic.volume = Mathf.Lerp(0f, library.musicVolume, t);
            yield return null;
        }

        oldMusic.Stop();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (library == null) return;
        PlayMusic(scene.name == "MainMenu" ? library.menuMusic : library.gameMusic);
    }

    void OnBossAppeared(EnemyBoss boss)
    {
        PlaySound(Sound.BossAppear, false);
        if (library != null && library.bossMusic != null) PlayMusic(library.bossMusic);
    }

    void OnBossGone()
    {
        if (library != null) PlayMusic(library.gameMusic);
    }

    void OnPlayerDied()
    {
        PlaySound(Sound.GameOver, false);
        PlayMusic(null); // silence, so the game over jingle stands out
    }

    void OnChapterCleared()
    {
        PlaySound(Sound.ChapterClear, false);
        PlayMusic(null);
    }
}
