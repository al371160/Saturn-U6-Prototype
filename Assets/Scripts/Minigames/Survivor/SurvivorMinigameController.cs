using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SurvivorMinigameController : MonoBehaviour
{
    public static SurvivorMinigameController Instance { get; private set; }

    [Header("Config")]
    public SurvivorMinigameConfig config;

    [Header("Optional UI")]
    public Canvas hudCanvas;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI levelBarText;
    public Slider healthBar;
    public Slider staminaBar;
    public Slider xpBar;

    [Header("Templates")]
    public GameObject enemyTemplate;

    public bool IsRunning { get; private set; }
    public bool IsPaused { get; private set; }
    public bool IsBossActive { get; private set; }

    public SurvivorMinigamePlayer MinigamePlayer => minigamePlayer;
    public SurvivorWeaponManager WeaponManager => weaponManager;
    public SurvivorPlayerProgression Progression => progression;
    public Transform enemyRoot;

    private SurvivorMinigamePlayer minigamePlayer;
    private PlayerController worldPlayer;
    private Transform gemRoot;
    private SurvivorWeaponManager weaponManager;
    private SurvivorPlayerProgression progression;
    private SurvivorLevelUpUI levelUpUI;
    private SurvivorMinigameSpawner spawner;
    private GameObject bossTemplate;
    private GameObject xpGemTemplate;
    private int pendingLevelUps;
    private int killCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        EnsureRuntimeSetup();

        worldPlayer = FindFirstObjectByType<PlayerController>();
        if (worldPlayer == null)
        {
            Debug.LogError("SurvivorMinigameController: no PlayerController found in the scene — combat cannot start.");
            return;
        }

        AttachToPlayer(worldPlayer);

        progression.Initialize(config);
        spawner.Initialize(this, config, minigamePlayer.transform, enemyRoot, enemyTemplate);

        IsRunning = true;
        RefreshHud();

        if (config.startingWeapons != null && config.startingWeapons.Length > 0)
            EquipStartingWeapons();
        else
            ShowInitialWeaponChoice();
    }

    private void ShowInitialWeaponChoice()
    {
        FreezeGameplay();

        List<SurvivorUpgradeChoice> choices = new List<SurvivorUpgradeChoice>();
        SurvivorWeaponDataSO[] startingChoicePool = config.startingWeaponChoices != null && config.startingWeaponChoices.Length > 0
            ? config.startingWeaponChoices
            : config.availableWeapons;

        if (startingChoicePool != null)
        {
            foreach (SurvivorWeaponDataSO weapon in startingChoicePool)
            {
                if (weapon == null)
                    continue;

                SurvivorWeaponDataSO captured = weapon;
                choices.Add(new SurvivorUpgradeChoice(weapon.displayName, weapon.description, weapon.weaponColor, c => c.WeaponManager.EquipOrUpgrade(captured)));
            }
        }

        levelUpUI.ShowChoices(choices, "Choose your weapon");
    }

    private void FreezeGameplay()
    {
        IsPaused = true;
        Time.timeScale = 0f;

        if (worldPlayer != null)
        {
            worldPlayer.canMove = false;
            worldPlayer.canLook = false;
        }
    }

    private void UnfreezeGameplay()
    {
        IsPaused = false;
        Time.timeScale = 1f;

        if (worldPlayer != null)
        {
            worldPlayer.canMove = true;
            worldPlayer.canLook = true;
        }
    }

    private void Update()
    {
        if (!IsRunning || worldPlayer == null)
            return;

        if (staminaBar != null)
            staminaBar.value = worldPlayer.maxStamina > 0f ? worldPlayer.currentStamina / worldPlayer.maxStamina : 0f;

        if (xpBar != null && progression != null)
            xpBar.value = progression.XPToNextLevel > 0 ? (float)progression.CurrentXP / progression.XPToNextLevel : 0f;
    }

    public int GetCurrentMaxEnemies()
    {
        int level = progression != null ? progression.Level : 1;
        int scaled = config.baseMaxEnemies + config.maxEnemiesPerLevel * (level - 1);
        return Mathf.Min(config.maxEnemiesCap, scaled);
    }

    private void AttachToPlayer(PlayerController player)
    {
        minigamePlayer = player.GetComponent<SurvivorMinigamePlayer>();
        if (minigamePlayer == null)
            minigamePlayer = player.gameObject.AddComponent<SurvivorMinigamePlayer>();

        minigamePlayer.Initialize(this, config);

        // Not parented to the player — SurvivorWeaponManager tracks the player's position
        // only (see Initialize), so auto weapons don't inherit the player's look rotation.
        GameObject weaponManagerObject = new GameObject("SurvivorWeaponManager");
        weaponManagerObject.transform.SetParent(transform, false);
        weaponManager = weaponManagerObject.AddComponent<SurvivorWeaponManager>();
        weaponManager.Initialize(this, player.transform);
    }

    public void RegisterKill()
    {
        killCount++;
        RefreshHud();

        if (config.killsPerBossWave > 0 && !IsBossActive && killCount % config.killsPerBossWave == 0)
            SpawnBoss();
    }

    public void RegisterBossDefeated()
    {
        IsBossActive = false;
        progression.AddXP(config.bossXPReward);
    }

    public void HandlePlayerDefeated()
    {
        if (minigamePlayer == null)
            return;

        minigamePlayer.Heal(minigamePlayer.MaxHealth);
        minigamePlayer.GrantInvulnerability(2f);
        RefreshHud();
    }

    public void SpawnXPGem(Vector3 position, int amount)
    {
        if (amount <= 0 || minigamePlayer == null)
            return;

        EnsureXPGemTemplate();

        GameObject gemObject = Instantiate(xpGemTemplate, position, Quaternion.identity, gemRoot);
        gemObject.SetActive(true);

        SurvivorXPGem gem = gemObject.GetComponent<SurvivorXPGem>();
        float magnetRadius = config.xpGemMagnetRadius + minigamePlayer.MagnetRadiusBonus;
        gem.Initialize(this, minigamePlayer.transform, amount, magnetRadius);
    }

    private void SpawnBoss()
    {
        IsBossActive = true;
        EnsureBossTemplate();

        float groundOffset = bossTemplate.transform.localScale.y * 0.5f;
        Vector3 spawnOffset = -minigamePlayer.transform.forward * 8f;
        Vector3 spawnPosition = SurvivorGroundUtility.SnapToGround(
            minigamePlayer.transform.position + spawnOffset,
            config.groundMask,
            config.groundSnapRayHeight,
            groundOffset);

        GameObject bossObject = Instantiate(bossTemplate, spawnPosition, Quaternion.identity, enemyRoot);
        bossObject.SetActive(true);

        SurvivorBossEnemy boss = bossObject.GetComponent<SurvivorBossEnemy>();
        boss.Initialize(
            this,
            minigamePlayer.transform,
            config.bossHealth,
            config.bossMoveSpeed,
            config.bossAttackRange,
            config.bossAttackDamage,
            config.bossAttackRadius,
            config.bossTelegraphSeconds,
            config.bossAttackSeconds,
            config.bossRecoverSeconds,
            config.groundMask,
            config.groundSnapRayHeight,
            groundOffset);

        if (statusText != null)
            statusText.text = "BOSS INCOMING";
    }

    private void HandleLevelUp(int newLevel)
    {
        pendingLevelUps++;

        if (!IsPaused)
            ShowNextLevelUpChoice();
    }

    private void ShowNextLevelUpChoice()
    {
        if (pendingLevelUps <= 0)
            return;

        FreezeGameplay();

        List<SurvivorUpgradeChoice> choices = SurvivorUpgradePool.BuildChoices(this, config, config.upgradeChoicesPerLevelUp);
        levelUpUI.ShowChoices(choices);
    }

    public void ResumeAfterUpgrade()
    {
        pendingLevelUps = Mathf.Max(0, pendingLevelUps - 1);

        if (pendingLevelUps > 0)
        {
            ShowNextLevelUpChoice();
            return;
        }

        UnfreezeGameplay();
    }

    private void EquipStartingWeapons()
    {
        if (config.startingWeapons == null)
            return;

        foreach (SurvivorWeaponDataSO weapon in config.startingWeapons)
        {
            if (weapon != null)
                weaponManager.EquipOrUpgrade(weapon);
        }
    }

    public void RefreshHud()
    {
        if (statusText != null)
            statusText.text = $"Kills: {killCount}";

        if (levelBarText != null && progression != null)
            levelBarText.text = $"Lv. {progression.Level}";

        if (healthBar != null && minigamePlayer != null)
            healthBar.value = minigamePlayer.CurrentHealth / minigamePlayer.MaxHealth;
    }

    private void EnsureXPGemTemplate()
    {
        if (xpGemTemplate != null)
            return;

        xpGemTemplate = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        xpGemTemplate.name = "XPGemTemplate";
        xpGemTemplate.transform.SetParent(transform, false);
        xpGemTemplate.transform.localScale = Vector3.one * 0.35f;
        xpGemTemplate.GetComponent<Renderer>().material.color = new Color(0.4f, 0.9f, 1f);

        Collider gemCollider = xpGemTemplate.GetComponent<Collider>();
        if (gemCollider != null)
            gemCollider.isTrigger = true;

        Rigidbody gemBody = xpGemTemplate.AddComponent<Rigidbody>();
        gemBody.isKinematic = true;
        gemBody.useGravity = false;

        xpGemTemplate.AddComponent<SurvivorXPGem>();
        xpGemTemplate.SetActive(false);
    }

    private void EnsureBossTemplate()
    {
        if (bossTemplate != null)
            return;

        bossTemplate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bossTemplate.name = "BossTemplate";
        bossTemplate.transform.SetParent(transform, false);
        bossTemplate.transform.localScale = new Vector3(2.2f, 2.2f, 2.2f);
        bossTemplate.GetComponent<Renderer>().material.color = new Color(0.55f, 0.15f, 0.65f);

        Collider bossCollider = bossTemplate.GetComponent<Collider>();
        if (bossCollider != null)
            bossCollider.isTrigger = false;

        Rigidbody bossBody = bossTemplate.AddComponent<Rigidbody>();
        bossBody.isKinematic = true;
        bossBody.useGravity = false;

        bossTemplate.AddComponent<SurvivorBossEnemy>();
        bossTemplate.SetActive(false);
    }

    private void EnsureRuntimeSetup()
    {
        if (enemyRoot == null)
        {
            GameObject enemyContainer = new GameObject("EnemyRoot");
            enemyContainer.transform.SetParent(transform, false);
            enemyRoot = enemyContainer.transform;
        }

        if (gemRoot == null)
        {
            GameObject gemContainer = new GameObject("GemRoot");
            gemContainer.transform.SetParent(transform, false);
            gemRoot = gemContainer.transform;
        }

        if (progression == null)
        {
            progression = gameObject.AddComponent<SurvivorPlayerProgression>();
            progression.OnLevelUp += HandleLevelUp;
        }

        if (levelUpUI == null)
        {
            GameObject levelUpObject = new GameObject("LevelUpUI");
            levelUpObject.transform.SetParent(transform, false);
            levelUpUI = levelUpObject.AddComponent<SurvivorLevelUpUI>();
            levelUpUI.Initialize(this);
        }

        if (spawner == null)
            spawner = gameObject.AddComponent<SurvivorMinigameSpawner>();

        if (enemyTemplate == null)
        {
            enemyTemplate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            enemyTemplate.name = "EnemyTemplate";
            enemyTemplate.transform.SetParent(transform, false);
            enemyTemplate.transform.localScale = new Vector3(0.9f, 1.8f, 0.9f);
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

        healthBar = BuildBar(canvasObject.transform, "HealthBar", new Vector2(0f, 68f), new Color(0.35f, 0.95f, 0.45f));
        staminaBar = BuildBar(canvasObject.transform, "StaminaBar", new Vector2(0f, 46f), new Color(0.95f, 0.85f, 0.3f));
        xpBar = BuildBar(canvasObject.transform, "XPBar", new Vector2(0f, 24f), new Color(0.55f, 0.65f, 0.95f));
        levelBarText = BuildLabeledBar(canvasObject.transform, "LevelBar", new Vector2(0f, 2f), new Color(0.7f, 0.5f, 0.9f), "Lv. 1");
    }

    private Slider BuildBar(Transform canvasTransform, string name, Vector2 anchoredPosition, Color fillColor)
    {
        GameObject barObject = new GameObject(name);
        barObject.transform.SetParent(canvasTransform, false);
        Slider bar = barObject.AddComponent<Slider>();
        bar.minValue = 0f;
        bar.maxValue = 1f;
        bar.value = 1f;
        RectTransform barRect = barObject.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.5f, 0f);
        barRect.anchorMax = new Vector2(0.5f, 0f);
        barRect.pivot = new Vector2(0.5f, 0f);
        barRect.sizeDelta = new Vector2(320f, 18f);
        barRect.anchoredPosition = anchoredPosition;

        GameObject background = new GameObject("Background");
        background.transform.SetParent(barObject.transform, false);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0f, 0f, 0f, 0.45f);
        background.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        background.GetComponent<RectTransform>().anchorMax = Vector2.one;
        background.GetComponent<RectTransform>().offsetMin = Vector2.zero;
        background.GetComponent<RectTransform>().offsetMax = Vector2.zero;

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(barObject.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(4f, 4f);
        fillAreaRect.offsetMax = new Vector2(-4f, -4f);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = fillColor;
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        bar.fillRect = fillRect;

        return bar;
    }

    private TextMeshProUGUI BuildLabeledBar(Transform canvasTransform, string name, Vector2 anchoredPosition, Color fillColor, string initialText)
    {
        Slider bar = BuildBar(canvasTransform, name, anchoredPosition, fillColor);
        bar.value = 1f;
        bar.interactable = false;

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(bar.transform, false);
        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = initialText;
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 14;
        label.color = Color.black;

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return label;
    }
}
