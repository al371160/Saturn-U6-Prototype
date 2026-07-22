using UnityEngine;

/// <summary>
/// Central one-shot / loop SFX hub. Prefer this over scattering AudioSources on every action.
/// Auto-creates on first play if missing from the scene.
/// Footstep loop, combat ambient, and bed ambient each use their own source.
/// </summary>
public class PlayerAudioHub : MonoBehaviour
{
    public static PlayerAudioHub Instance { get; private set; }

    [SerializeField] private PlayerAudioLibrary library;
    [SerializeField] private AudioSource oneShotSource;
    [SerializeField] private AudioSource loopSource;
    [SerializeField] private AudioSource ambientCombatSource;
    [SerializeField] private AudioSource ambientBedSource;
    [SerializeField] private AudioSource windSource;
    [SerializeField] private AudioSource glideSource;

    public PlayerAudioLibrary Library => library;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists()
    {
        if (Instance != null)
            return;

        PlayerAudioHub existing = FindFirstObjectByType<PlayerAudioHub>();
        if (existing != null)
        {
            Instance = existing;
            return;
        }

        GameObject go = new GameObject("PlayerAudioHub");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<PlayerAudioHub>();
        Instance.Bootstrap();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Bootstrap();
    }

    private void Bootstrap()
    {
        if (oneShotSource == null)
            oneShotSource = CreateSource("OneShot", loop: false);

        if (loopSource == null)
            loopSource = CreateSource("MovementLoop", loop: true);

        if (ambientCombatSource == null)
            ambientCombatSource = CreateSource("AmbientCombat", loop: true);

        if (ambientBedSource == null)
            ambientBedSource = CreateSource("AmbientBed", loop: true);

        if (windSource == null)
            windSource = CreateSource("WindLoop", loop: true);

        if (glideSource == null)
            glideSource = CreateSource("GlideLoop", loop: true);

        if (library == null)
        {
#if UNITY_EDITOR
            library = UnityEditor.AssetDatabase.LoadAssetAtPath<PlayerAudioLibrary>("Assets/ScriptableObject/Audio/Player Audio Library.asset");
#endif
        }
    }

    private AudioSource CreateSource(string childName, bool loop)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(transform, false);
        AudioSource source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        return source;
    }

    public void SetLibrary(PlayerAudioLibrary audioLibrary)
    {
        library = audioLibrary;
    }

    public void PlayOneShot(AudioClip clip, float volume = 1f)
    {
        if (clip == null || oneShotSource == null)
            return;

        oneShotSource.PlayOneShot(clip, volume);
    }

    public void PlayLibrary(System.Func<PlayerAudioLibrary, AudioClip> selector, float volume = 1f)
    {
        if (library == null || selector == null)
            return;

        PlayOneShot(selector(library), volume);
    }

    public void StartLoop(AudioClip clip, float volume = 1f)
    {
        if (loopSource == null || clip == null)
            return;

        if (loopSource.isPlaying && loopSource.clip == clip)
        {
            loopSource.volume = volume;
            return;
        }

        loopSource.clip = clip;
        loopSource.volume = volume;
        loopSource.Play();
    }

    public void StopLoop()
    {
        if (loopSource != null && loopSource.isPlaying)
            loopSource.Stop();
    }

    public void StartAmbientCombat(AudioClip clip, float volume)
    {
        StartAmbientOn(ambientCombatSource, clip, volume);
    }

    public void StartAmbientBed(AudioClip clip, float volume)
    {
        StartAmbientOn(ambientBedSource, clip, volume);
    }

    public void StopAmbient()
    {
        StopAmbientSource(ambientCombatSource);
        StopAmbientSource(ambientBedSource);
    }

    public void StartWind(AudioClip clip, float volume = 0.35f)
    {
        StartAmbientOn(windSource, clip, volume);
    }

    public void StopWind()
    {
        StopAmbientSource(windSource);
    }

    public void StartGlide(AudioClip clip, float volume = 0.4f)
    {
        StartAmbientOn(glideSource, clip, volume);
    }

    public void StopGlide()
    {
        StopAmbientSource(glideSource);
    }

    private static void StartAmbientOn(AudioSource source, AudioClip clip, float volume)
    {
        if (source == null || clip == null)
            return;

        if (source.isPlaying && source.clip == clip)
        {
            source.volume = volume;
            return;
        }

        source.clip = clip;
        source.volume = volume;
        source.Play();
    }

    private static void StopAmbientSource(AudioSource source)
    {
        if (source != null && source.isPlaying)
            source.Stop();
    }
}
