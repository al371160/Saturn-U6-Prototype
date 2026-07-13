using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SurvivorMinigameController : MonoBehaviour
{
    public static SurvivorMinigameController Instance { get; private set; }

    [Header("Scene References")]
    public Camera minigameCamera;
    public Transform arenaRoot;
    public Transform playerRoot;
    public Transform enemyRoot;
    public SurvivorMinigamePlayer minigamePlayer;
    public SurvivorOrbitWeapon orbitWeapon;
    public SurvivorMinigameSpawner spawner;

    [Header("Optional UI")]
    public Canvas hudCanvas;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI timerText;
    public Slider healthBar;

    [Header("Templates")]
    public GameObject enemyTemplate;

    [Header("Fade")]
    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 0.35f;

    public bool IsRunning { get; private set; }

    private SurvivorMinigameConfig activeConfig;
    private PlayerController worldPlayer;
    private Camera worldCamera;
    private Transform worldPlayerTransform;
    private Vector3 returnPosition;
    private Quaternion returnRotation;
    private int killCount;
    private float elapsedTime;
    private Action onComplete;
    private Action onFail;
    private bool builtRuntimeArena;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureRuntimeSetup();
        gameObject.SetActive(false);
    }

    public void StartMinigame(SurvivorMinigameConfig config, Transform player, Action completeCallback, Action failCallback)
    {
        if (IsRunning || config == null || player == null)
            return;

        activeConfig = config;
        onComplete = completeCallback;
        onFail = failCallback;
        worldPlayerTransform = player;
        worldPlayer = player.GetComponent<PlayerController>();

        returnPosition = player.position;
        returnRotation = player.rotation;

        killCount = 0;
        elapsedTime = 0f;

        StartCoroutine(BeginMinigameRoutine());
    }

    public void RegisterKill()
    {
        killCount++;
        RefreshHud();

        if (activeConfig.killsToWin > 0 && killCount >= activeConfig.killsToWin)
            EndMinigame(true);
    }

    public void EndMinigame(bool won)
    {
        if (!IsRunning)
            return;

        StartCoroutine(FinishMinigameRoutine(won));
    }

    public Vector3 ClampToArena(Vector3 position)
    {
        if (activeConfig == null || arenaRoot == null)
            return position;

        Vector3 local = arenaRoot.InverseTransformPoint(position);
        float limit = activeConfig.arenaHalfSize - 0.75f;
        local.x = Mathf.Clamp(local.x, -limit, limit);
        local.z = Mathf.Clamp(local.z, -limit, limit);
        local.y = 0f;
        return arenaRoot.TransformPoint(local);
    }

    public void RefreshHud()
    {
        if (statusText != null)
        {
            if (activeConfig.killsToWin > 0)
                statusText.text = $"Defeated: {killCount}/{activeConfig.killsToWin}";
            else
                statusText.text = $"Defeated: {killCount}";
        }

        if (healthBar != null && minigamePlayer != null)
            healthBar.value = minigamePlayer.CurrentHealth / minigamePlayer.MaxHealth;
    }

    private IEnumerator BeginMinigameRoutine()
    {
        EnsureRuntimeSetup();
        yield return FadeTo(1f);

        CacheWorldCamera();
        HideWorldPlayer(true);

        if (worldPlayer != null)
        {
            worldPlayer.canMove = false;
            worldPlayer.canLook = false;
        }

        ResetArena();
        gameObject.SetActive(true);
        IsRunning = true;

        if (minigameCamera != null)
            minigameCamera.gameObject.SetActive(true);

        if (worldCamera != null)
            worldCamera.gameObject.SetActive(false);

        if (hudCanvas != null)
            hudCanvas.gameObject.SetActive(true);

        minigamePlayer.Initialize(this, activeConfig);
        orbitWeapon.Initialize(
            this,
            activeConfig.orbitCount,
            activeConfig.orbitRadius,
            activeConfig.orbitSpeed,
            activeConfig.orbitDamage,
            activeConfig.orbitHitRadius);

        spawner.Initialize(this, activeConfig, minigamePlayer.transform, enemyRoot, enemyTemplate);
        RefreshHud();

        yield return FadeTo(0f);
    }

    private IEnumerator FinishMinigameRoutine(bool won)
    {
        IsRunning = false;
        yield return FadeTo(1f);

        ClearEnemies();

        if (minigameCamera != null)
            minigameCamera.gameObject.SetActive(false);

        if (worldCamera != null)
            worldCamera.gameObject.SetActive(true);

        if (hudCanvas != null)
            hudCanvas.gameObject.SetActive(false);

        HideWorldPlayer(false);

        if (worldPlayerTransform != null)
        {
            CharacterController cc = worldPlayerTransform.GetComponent<CharacterController>();
            if (cc != null)
                cc.enabled = false;

            worldPlayerTransform.SetPositionAndRotation(returnPosition, returnRotation);

            if (cc != null)
                cc.enabled = true;
        }

        if (worldPlayer != null)
        {
            worldPlayer.canMove = true;
            worldPlayer.canLook = true;
        }

        gameObject.SetActive(false);

        if (won)
            onComplete?.Invoke();
        else
            onFail?.Invoke();

        onComplete = null;
        onFail = null;

        yield return FadeTo(0f);
    }

    private void Update()
    {
        if (!IsRunning || activeConfig == null)
            return;

        elapsedTime += Time.deltaTime;

        if (timerText != null)
        {
            if (activeConfig.killsToWin <= 0)
            {
                float remaining = Mathf.Max(0f, activeConfig.surviveSeconds - elapsedTime);
                timerText.text = $"Time: {remaining:0.0}s";
            }
            else
            {
                timerText.text = $"Time: {elapsedTime:0.0}s";
            }
        }

        if (activeConfig.killsToWin <= 0 && elapsedTime >= activeConfig.surviveSeconds)
            EndMinigame(true);

        if (minigameCamera != null && minigamePlayer != null)
        {
            Vector3 cameraPosition = minigamePlayer.transform.position + new Vector3(0f, 20f, 0f);
            minigameCamera.transform.position = cameraPosition;
        }
    }

    private void ResetArena()
    {
        ClearEnemies();

        if (playerRoot != null)
            playerRoot.localPosition = Vector3.zero;

        if (minigamePlayer != null)
            minigamePlayer.transform.localPosition = Vector3.zero;
    }

    private void ClearEnemies()
    {
        if (enemyRoot == null)
            return;

        for (int i = enemyRoot.childCount - 1; i >= 0; i--)
            Destroy(enemyRoot.GetChild(i).gameObject);
    }

    private void HideWorldPlayer(bool hidden)
    {
        if (worldPlayerTransform == null)
            return;

        Renderer[] renderers = worldPlayerTransform.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = !hidden;
    }

    private void CacheWorldCamera()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        if (fadeCanvasGroup == null)
            yield break;

        float startAlpha = fadeCanvasGroup.alpha;
        float timer = 0f;

        fadeCanvasGroup.blocksRaycasts = targetAlpha > 0.5f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
    }

    private void EnsureRuntimeSetup()
    {
        if (arenaRoot != null && minigamePlayer != null && enemyTemplate != null)
            return;

        if (builtRuntimeArena)
            return;

        builtRuntimeArena = true;

        if (arenaRoot == null)
        {
            GameObject arena = new GameObject("SurvivorArena");
            arena.transform.SetParent(transform, false);
            arena.transform.localPosition = new Vector3(0f, 500f, 0f);
            arenaRoot = arena.transform;

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "ArenaFloor";
            floor.transform.SetParent(arenaRoot, false);
            floor.transform.localScale = new Vector3(3.6f, 1f, 3.6f);
            floor.GetComponent<Renderer>().material.color = new Color(0.12f, 0.16f, 0.2f);

            Collider floorCollider = floor.GetComponent<Collider>();
            if (floorCollider != null)
                floorCollider.isTrigger = false;
        }

        if (playerRoot == null)
        {
            GameObject playerContainer = new GameObject("PlayerRoot");
            playerContainer.transform.SetParent(arenaRoot, false);
            playerRoot = playerContainer.transform;
        }

        if (enemyRoot == null)
        {
            GameObject enemyContainer = new GameObject("EnemyRoot");
            enemyContainer.transform.SetParent(arenaRoot, false);
            enemyRoot = enemyContainer.transform;
        }

        if (minigamePlayer == null)
        {
            GameObject playerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerObject.name = "SurvivorPlayer";
            playerObject.transform.SetParent(playerRoot, false);
            playerObject.transform.localPosition = Vector3.up;

            Collider playerCollider = playerObject.GetComponent<Collider>();
            if (playerCollider != null)
                playerCollider.isTrigger = false;

            playerObject.GetComponent<Renderer>().material.color = new Color(0.3f, 0.9f, 0.45f);
            minigamePlayer = playerObject.AddComponent<SurvivorMinigamePlayer>();
        }

        if (orbitWeapon == null)
        {
            GameObject weaponObject = new GameObject("OrbitWeapon");
            weaponObject.transform.SetParent(minigamePlayer.transform, false);
            orbitWeapon = weaponObject.AddComponent<SurvivorOrbitWeapon>();
        }

        if (spawner == null)
            spawner = gameObject.AddComponent<SurvivorMinigameSpawner>();

        if (enemyTemplate == null)
        {
            enemyTemplate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            enemyTemplate.name = "EnemyTemplate";
            enemyTemplate.transform.SetParent(transform, false);
            enemyTemplate.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
            enemyTemplate.GetComponent<Renderer>().material.color = new Color(0.9f, 0.25f, 0.25f);

            Collider enemyCollider = enemyTemplate.GetComponent<Collider>();
            if (enemyCollider != null)
                enemyCollider.isTrigger = false;

            Rigidbody enemyBody = enemyTemplate.AddComponent<Rigidbody>();
            enemyBody.isKinematic = true;
            enemyBody.useGravity = false;

            enemyTemplate.AddComponent<SurvivorMinigameEnemy>();
            enemyTemplate.SetActive(false);
        }

        if (minigameCamera == null)
        {
            GameObject cameraObject = new GameObject("SurvivorMinigameCamera");
            cameraObject.transform.SetParent(transform, false);
            minigameCamera = cameraObject.AddComponent<Camera>();
            minigameCamera.orthographic = true;
            minigameCamera.orthographicSize = 12f;
            minigameCamera.transform.position = arenaRoot.position + new Vector3(0f, 20f, 0f);
            minigameCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            minigameCamera.clearFlags = CameraClearFlags.SolidColor;
            minigameCamera.backgroundColor = new Color(0.04f, 0.05f, 0.08f);
            minigameCamera.gameObject.SetActive(false);
        }

        BuildHudIfMissing();
    }

    private void BuildHudIfMissing()
    {
        if (hudCanvas != null)
            return;

        GameObject canvasObject = new GameObject("SurvivorHud");
        canvasObject.transform.SetParent(transform, false);
        hudCanvas = canvasObject.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject statusObject = new GameObject("StatusText");
        statusObject.transform.SetParent(canvasObject.transform, false);
        statusText = statusObject.AddComponent<TextMeshProUGUI>();
        statusText.fontSize = 28;
        statusText.alignment = TextAlignmentOptions.TopLeft;
        statusText.rectTransform.anchorMin = new Vector2(0f, 1f);
        statusText.rectTransform.anchorMax = new Vector2(0f, 1f);
        statusText.rectTransform.pivot = new Vector2(0f, 1f);
        statusText.rectTransform.anchoredPosition = new Vector2(24f, -24f);

        GameObject timerObject = new GameObject("TimerText");
        timerObject.transform.SetParent(canvasObject.transform, false);
        timerText = timerObject.AddComponent<TextMeshProUGUI>();
        timerText.fontSize = 28;
        timerText.alignment = TextAlignmentOptions.TopRight;
        timerText.rectTransform.anchorMin = new Vector2(1f, 1f);
        timerText.rectTransform.anchorMax = new Vector2(1f, 1f);
        timerText.rectTransform.pivot = new Vector2(1f, 1f);
        timerText.rectTransform.anchoredPosition = new Vector2(-24f, -24f);

        GameObject healthObject = new GameObject("HealthBar");
        healthObject.transform.SetParent(canvasObject.transform, false);
        healthBar = healthObject.AddComponent<Slider>();
        healthBar.minValue = 0f;
        healthBar.maxValue = 1f;
        healthBar.value = 1f;
        RectTransform healthBarRect = healthObject.GetComponent<RectTransform>();
        healthBarRect.anchorMin = new Vector2(0.5f, 0f);
        healthBarRect.anchorMax = new Vector2(0.5f, 0f);
        healthBarRect.pivot = new Vector2(0.5f, 0f);
        healthBarRect.sizeDelta = new Vector2(320f, 18f);
        healthBarRect.anchoredPosition = new Vector2(0f, 24f);

        GameObject background = new GameObject("Background");
        background.transform.SetParent(healthObject.transform, false);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0f, 0f, 0f, 0.45f);
        background.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        background.GetComponent<RectTransform>().anchorMax = Vector2.one;
        background.GetComponent<RectTransform>().offsetMin = Vector2.zero;
        background.GetComponent<RectTransform>().offsetMax = Vector2.zero;

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(healthObject.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(4f, 4f);
        fillAreaRect.offsetMax = new Vector2(-4f, -4f);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.35f, 0.95f, 0.45f);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        healthBar.fillRect = fillRect;

        hudCanvas.gameObject.SetActive(false);
    }
}
