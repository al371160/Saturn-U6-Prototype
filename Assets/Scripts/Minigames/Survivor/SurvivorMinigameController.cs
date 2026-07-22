using System.Collections;
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
    [Tooltip("Deprecated — Survivor uses Power Core StaminaUI dots instead of a bar.")]
    public Slider staminaBar;
    public Slider xpBar;
    public StaminaUI staminaDotsUI;
    public GameObject staminaUnitPrefab;

    [Header("Templates")]
    public GameObject enemyTemplate;

    public bool IsRunning { get; private set; }
    public bool IsPaused { get; private set; }
    /// <summary>True while starter-weapon / level-up UI is open. World keeps running under a pause shield.</summary>
    public bool IsUpgradeMenuOpen { get; private set; }
    public bool IsBossActive => activeBosses.Count > 0;
    public SurvivorRoundResult? FinalResult { get; private set; }
    private readonly List<SurvivorBossEnemy> activeBosses = new List<SurvivorBossEnemy>();

    private SurvivorRoundRules roundRules;

    /// <summary>Opt-in mode plug-in — leaving this unset preserves the original fully-endless
    /// behavior exactly (no auto win/loss).</summary>
    public void SetRoundRules(SurvivorRoundRules rules)
    {
        roundRules = rules;
        roundRules?.Initialize(this);
    }

    /// <summary>Ends the round for good (unlike FreezeGameplay, which is for transient menus).</summary>
    public void EndRound(SurvivorRoundResult result)
    {
        if (!IsRunning)
            return;

        IsRunning = false;
        FinalResult = result;
        IsPaused = true;
        Time.timeScale = 0f;
        SetMenuMouseUnlocked(true);

        inventorySync?.ClearSynced();
        SurvivorAudio.StopCombatAmbience();

        if (forestRoot != null)
        {
            Destroy(forestRoot.gameObject);
            forestRoot = null;
        }

        if (statusText != null)
            statusText.text = $"{result.outcome}: {result.reason}";
    }

    [Header("Live Tuning (design/debug)")]
    [Tooltip("Multiplies the enemy count cap — a pure 'how many enemies' knob.")]
    [Range(0.1f, 5f)]
    public float enemyIntensity = 1f;
    [Tooltip("Fraction of newly spawned enemies that spawn as tougher, bigger 'elite' variants (0 = none, 1 = all).")]
    [Range(0f, 1f)]
    public float enemyEliteness = 0f;

    public SurvivorMinigamePlayer MinigamePlayer => minigamePlayer;
    public SurvivorWeaponManager WeaponManager => weaponManager;
    public SurvivorPlayerProgression Progression => progression;
    public SurvivorMinigameSpawner EnemySpawner => spawner;
    public Transform enemyRoot;

    /// <summary>Every buff acquired this run and its current stack count. Mirrored into
    /// InventoryCanvas via SurvivorInventorySync for display; gameplay effects are applied by
    /// SurvivorBuffDataSO.Apply() when picked.</summary>
    public IReadOnlyDictionary<SurvivorBuffDataSO, int> AcquiredBuffs => acquiredBuffs;
    private readonly Dictionary<SurvivorBuffDataSO, int> acquiredBuffs = new Dictionary<SurvivorBuffDataSO, int>();

    public void RecordBuffAcquired(SurvivorBuffDataSO buff)
    {
        if (buff == null)
            return;

        acquiredBuffs.TryGetValue(buff, out int count);
        acquiredBuffs[buff] = count + 1;
        NotifyLoadoutChanged();
    }

    /// <summary>Pushes the current weapon/buff loadout into InventoryCanvas ItemSlots.</summary>
    public void NotifyLoadoutChanged()
    {
        inventorySync?.Refresh();
    }

    private SurvivorMinigamePlayer minigamePlayer;
    private PlayerController worldPlayer;
    private Transform gemRoot;
    private SurvivorWeaponManager weaponManager;
    private SurvivorPlayerProgression progression;
    private SurvivorLevelUpUI levelUpUI;
    private SurvivorInventorySync inventorySync;
    private SurvivorMinigameSpawner spawner;
    private GameObject bossTemplate;
    private GameObject xpGemTemplate;
    private int pendingLevelUps;
    private int killCount;
    private SurvivorPauseShield pauseShield;

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

        if (config != null)
            SurvivorDamagePopup.SharedFont = config.levelUpFont;

        worldPlayer = FindFirstObjectByType<PlayerController>();
        if (worldPlayer == null)
        {
            Debug.LogError("SurvivorMinigameController: no PlayerController found in the scene — combat cannot start.");
            return;
        }

        if (config.useBattleBusIntro)
            StartCoroutine(BattleBusIntroRoutine());
        else
            BeginCombat();
    }

    private bool playerSystemsReady;
    private bool weaponChoiceHandled;

    /// <summary>Idempotent — safe to call both early (bus ride) and again from BeginCombat() (non-bus path).</summary>
    private void EnsurePlayerSystemsReady()
    {
        if (playerSystemsReady)
            return;
        playerSystemsReady = true;

        // Drop any leftover run-synced entries before equipping this run's loadout.
        inventorySync?.ClearSynced();

        AttachToPlayer(worldPlayer);
        progression.Initialize(config);

        if (staminaDotsUI != null)
            staminaDotsUI.player = worldPlayer;
        else if (hudCanvas != null)
            EnsureStaminaDotsHud(hudCanvas.transform);
        else
        {
            StaminaUI existing = FindFirstObjectByType<StaminaUI>();
            if (existing != null)
            {
                staminaDotsUI = existing;
                staminaDotsUI.player = worldPlayer;
            }
        }

        if (staminaBar != null)
            staminaBar.gameObject.SetActive(false);
    }

    /// <summary>Idempotent — the bus path handles this while riding; the non-bus path handles it
    /// from BeginCombat() same as before.</summary>
    private void HandleWeaponChoiceIfNeeded()
    {
        if (weaponChoiceHandled)
            return;
        weaponChoiceHandled = true;

        if (config.startingWeapons != null && config.startingWeapons.Length > 0)
            EquipStartingWeapons();
        else
            ShowInitialWeaponChoice();
    }

    private bool worldPropsSpawned;

    private IEnumerator BattleBusIntroRoutine()
    {
        GameObject busObject = new GameObject("SurvivorBattleBus");
        SurvivorBattleBus bus = busObject.AddComponent<SurvivorBattleBus>();
        bus.pathHalfExtent = config.busPathHalfExtent;
        bus.flightAltitude = config.busFlightAltitude;
        bus.flightDuration = config.busFlightDuration;
        bus.centerPassDistance = config.busCenterPassDistance;

        bool jumped = false;
        bus.Launch(worldPlayer.transform, () => jumped = true);

        // Populate the map as soon as the player boards the bus (visible during flight/skydive).
        EnsurePlayerSystemsReady();
        SpawnWorldPropsIfNeeded();
        ApplySessionOrDefaultLoadout();

        yield return new WaitUntil(() => jumped);
        yield return new WaitUntil(() => !worldPlayer.isSkydiving && worldPlayer.isGrounded);

        Destroy(busObject);
        BeginCombat();

        // Hand off from the freefall/shoulder view to the tactical top-down view once grounded.
        SurvivorCameraViewToggle viewToggle = FindFirstObjectByType<SurvivorCameraViewToggle>();
        viewToggle?.ForceTopDownView();
    }

    private void BeginCombat()
    {
        EnsurePlayerSystemsReady();

        spawner.Initialize(this, config, minigamePlayer.transform, enemyRoot, enemyTemplate);
        SpawnWorldPropsIfNeeded();

        IsRunning = true;
        SurvivorAudio.StartCombatAmbience();
        RefreshHud();

        ApplySessionOrDefaultLoadout();
    }

    /// <summary>Trees / crates / ground weapons — once per run. Called on bus board (or BeginCombat if no bus).</summary>
    private void SpawnWorldPropsIfNeeded()
    {
        if (worldPropsSpawned)
            return;
        worldPropsSpawned = true;

        SpawnForestTrees();
        SpawnLootCrates();
        SpawnGroundWeapons();
    }

    /// <summary>Uses MatchSessionState loadout from the menu when present; otherwise config defaults.
    /// Never opens the bus/start weapon-choice modal.</summary>
    private void ApplySessionOrDefaultLoadout()
    {
        if (weaponChoiceHandled)
            return;
        weaponChoiceHandled = true;

        MatchSessionState session = MatchSessionState.Instance;
        if (session != null && session.HasLoadout)
        {
            if (session.SelectedWeapon != null)
                weaponManager.EquipOrUpgrade(session.SelectedWeapon);

            for (int i = 0; i < session.SelectedBuffs.Count; i++)
            {
                SurvivorBuffDataSO buff = session.SelectedBuffs[i];
                if (buff == null)
                    continue;
                buff.Apply(this);
                RecordBuffAcquired(buff);
            }

            return;
        }

        if (config.startingWeapons != null && config.startingWeapons.Length > 0)
        {
            EquipStartingWeapons();
            return;
        }

        // Editor / direct Level 1 play: auto-equip first starting choice or available weapon.
        SurvivorWeaponDataSO[] pool = config.startingWeaponChoices != null && config.startingWeaponChoices.Length > 0
            ? config.startingWeaponChoices
            : config.availableWeapons;
        if (pool != null)
        {
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i] == null)
                    continue;
                weaponManager.EquipOrUpgrade(pool[i]);
                break;
            }
        }
    }

    private Transform forestRoot;

    /// <summary>Scatter forest tree prefabs across the combat pad.</summary>
    private void SpawnForestTrees()
    {
        if (config == null || config.forestTreePrefabs == null || config.forestTreePrefabs.Length == 0)
            return;

        if (forestRoot != null)
            Destroy(forestRoot.gameObject);

        forestRoot = new GameObject("SurvivorTrees").transform;
        int targetCount = Mathf.Max(0, config.forestTreeCount);
        float half = config.forestMapHalfExtent;
        float minSpacing = Mathf.Max(1f, config.forestMinSpacing);
        float minSpacingSqr = minSpacing * minSpacing;

        List<Vector3> placed = new List<Vector3>(targetCount);
        int attempts = 0;
        int maxAttempts = targetCount * 20;

        while (placed.Count < targetCount && attempts < maxAttempts)
        {
            attempts++;
            Vector3 candidate = new Vector3(
                Random.Range(-half, half),
                0f,
                Random.Range(-half, half));

            bool tooClose = false;
            for (int i = 0; i < placed.Count; i++)
            {
                if ((placed[i] - candidate).sqrMagnitude < minSpacingSqr)
                {
                    tooClose = true;
                    break;
                }
            }

            if (tooClose)
                continue;

            if (Physics.CheckSphere(candidate + Vector3.up, 2.5f, ~0, QueryTriggerInteraction.Ignore))
            {
                // Allow ground hits; reject if a non-trigger collider sits near the point above ground.
                Collider[] hits = Physics.OverlapSphere(candidate + Vector3.up * 1.2f, 1.8f, ~0, QueryTriggerInteraction.Ignore);
                bool blocked = false;
                for (int h = 0; h < hits.Length; h++)
                {
                    if (hits[h] == null)
                        continue;
                    // Skip large ground/terrain meshes by requiring a reasonably tall local bounds.
                    if (hits[h] is TerrainCollider)
                        continue;
                    Bounds b = hits[h].bounds;
                    if (b.size.y > 1.2f && b.extents.x + b.extents.z > 2f)
                    {
                        blocked = true;
                        break;
                    }
                }

                if (blocked)
                    continue;
            }

            GameObject prefab = config.forestTreePrefabs[Random.Range(0, config.forestTreePrefabs.Length)];
            if (prefab == null)
                continue;

            Vector3 spawnPosition = SurvivorGroundUtility.SnapToGround(
                candidate, config.groundMask, config.groundSnapRayHeight, 0f);

            GameObject tree = Instantiate(prefab, spawnPosition, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), forestRoot);
            float scale = Random.Range(0.85f, 1.2f);
            tree.transform.localScale = prefab.transform.localScale * scale;
            placed.Add(spawnPosition);
        }
    }

    /// <summary>Spawns breakable loot crates at random picks from map anchors (not player-relative).
    /// Crates drop weapons or buffs when shattered.</summary>
    private void SpawnLootCrates()
    {
        List<SurvivorBuffDataSO> buffPool = new List<SurvivorBuffDataSO>();
        if (config.availableBuffs != null)
        {
            foreach (SurvivorBuffDataSO buff in config.availableBuffs)
            {
                if (buff != null)
                    buffPool.Add(buff);
            }
        }

        SurvivorWeaponDataSO[] weapons = config.availableWeapons;
        SurvivorBuffDataSO[] buffs = buffPool.ToArray();
        if ((weapons == null || weapons.Length == 0) && buffs.Length == 0)
            return;

        Vector3[] shuffled = ShuffleAreas(GetPredeterminedCrateAreas());
        const int structureCount = 28;
        int spawnCount = Mathf.Min(structureCount, shuffled.Length);

        Transform pickupTarget = worldPlayer != null ? worldPlayer.transform : minigamePlayer != null ? minigamePlayer.transform : null;
        float halfHeight = SurvivorLootCrate.CrateWorldSize * 0.5f;

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 area = shuffled[i];
            Vector2 jitter = Random.insideUnitCircle * 6f;
            Vector3 spawnPosition = area + new Vector3(jitter.x, 0f, jitter.y);
            spawnPosition = SurvivorGroundUtility.SnapToGround(spawnPosition, config.groundMask, config.groundSnapRayHeight, halfHeight);

            GameObject structureObject = new GameObject("SurvivorLootCrate");
            structureObject.transform.position = spawnPosition;

            BoxCollider structureCollider = structureObject.AddComponent<BoxCollider>();
            structureCollider.size = Vector3.one * SurvivorLootCrate.CrateWorldSize;

            SurvivorLootCrate crate = structureObject.AddComponent<SurvivorLootCrate>();
            crate.Initialize(this, pickupTarget, weapons, buffs, 50f, 0.65f);
        }
    }

    /// <summary>Raw weapon pickups on the ground (no crate) at a different random subset of map anchors.</summary>
    private void SpawnGroundWeapons()
    {
        SurvivorWeaponDataSO[] weapons = config.availableWeapons;
        if (weapons == null || weapons.Length == 0)
            return;

        Vector3[] shuffled = ShuffleAreas(GetPredeterminedCrateAreas());
        const int groundCount = 10;
        int spawnCount = Mathf.Min(groundCount, shuffled.Length);

        Transform pickupTarget = worldPlayer != null ? worldPlayer.transform : minigamePlayer != null ? minigamePlayer.transform : null;

        for (int i = 0; i < spawnCount; i++)
        {
            SurvivorWeaponDataSO weapon = SurvivorLootRarity.PickWeightedWeapon(weapons);
            if (weapon == null)
                continue;

            int startStar = SurvivorLootRarity.RollStartStar(weapon);

            // Offset from crate cluster so they don't stack on the same points.
            Vector3 area = shuffled[i];
            Vector2 jitter = Random.insideUnitCircle * 10f + new Vector2(8f, -6f);
            Vector3 spawnPosition = area + new Vector3(jitter.x, 0f, jitter.y);
            spawnPosition = SurvivorGroundUtility.SnapToGround(spawnPosition, config.groundMask, config.groundSnapRayHeight, 0.45f);

            GameObject pickupObject = new GameObject("SurvivorWeaponPickup");
            pickupObject.transform.position = spawnPosition;
            SphereCollider col = pickupObject.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.45f;
            pickupObject.AddComponent<SurvivorWeaponPickup>().Initialize(this, pickupTarget, weapon, startStar);
        }
    }

    private static Vector3[] ShuffleAreas(Vector3[] source)
    {
        Vector3[] shuffled = (Vector3[])source.Clone();
        for (int i = shuffled.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Vector3 tmp = shuffled[i];
            shuffled[i] = shuffled[j];
            shuffled[j] = tmp;
        }

        return shuffled;
    }

    /// <summary>Dense grid of world anchors across the Level 1 terrain (±200). Each run
    /// randomly activates a subset — positions themselves do not depend on the player.</summary>
    private static Vector3[] GetPredeterminedCrateAreas()
    {
        List<Vector3> areas = new List<Vector3>(64);
        const float half = 200f;
        const float step = 60f;
        for (float x = -half; x <= half + 0.01f; x += step)
        {
            for (float z = -half; z <= half + 0.01f; z += step)
                areas.Add(new Vector3(x, 0f, z));
        }

        return areas.ToArray();
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
                string levelOneDescription = weapon.GetLevelDescription(1);
                choices.Add(new SurvivorUpgradeChoice(weapon.displayName, levelOneDescription, weapon.weaponColor, weapon.icon, c => c.WeaponManager.EquipOrUpgrade(captured)));
            }
        }

        levelUpUI.ShowChoices(choices, "Choose your weapon");
    }

    /// <summary>Soft menu pause: world keeps running under an invincible forcefield (no timeScale freeze).</summary>
    private void FreezeGameplay()
    {
        IsUpgradeMenuOpen = true;
        Time.timeScale = 1f;

        if (worldPlayer != null)
        {
            worldPlayer.canMove = false;
            worldPlayer.canLook = false;
        }

        if (minigamePlayer != null)
            minigamePlayer.IsInvulnerable = true;

        Transform follow = worldPlayer != null ? worldPlayer.transform : minigamePlayer != null ? minigamePlayer.transform : null;
        if (pauseShield == null && follow != null)
            pauseShield = SurvivorPauseShield.Attach(follow, 6.5f);

        SetMenuMouseUnlocked(true);
    }

    private void UnfreezeGameplay()
    {
        IsUpgradeMenuOpen = false;
        Time.timeScale = 1f;

        if (worldPlayer != null)
        {
            worldPlayer.canMove = true;
            worldPlayer.canLook = true;
        }

        if (minigamePlayer != null)
            minigamePlayer.IsInvulnerable = false;

        if (pauseShield != null)
        {
            pauseShield.Teardown();
            pauseShield = null;
        }

        SetMenuMouseUnlocked(false);
    }

    /// <summary>Always unlocks the cursor while an upgrade/weapon-choice menu is open, regardless
    /// of which camera view is active, so its buttons are clickable.</summary>
    private void SetMenuMouseUnlocked(bool menuOpen)
    {
        SurvivorMouseAimRig aimRig = FindFirstObjectByType<SurvivorMouseAimRig>();
        aimRig?.SetMenuOpen(menuOpen);
    }

    private void Update()
    {
        if (!IsRunning || worldPlayer == null)
            return;

        if (roundRules != null)
        {
            SurvivorRoundResult? result = roundRules.OnTick(Time.deltaTime);
            if (result.HasValue)
            {
                EndRound(result.Value);
                return;
            }
        }

        // Stamina is rendered by StaminaUI dots (Power Core units), not a slider bar.
        if (staminaBar != null && staminaBar.gameObject.activeSelf)
            staminaBar.gameObject.SetActive(false);

        if (xpBar != null && progression != null)
            xpBar.value = progression.XPToNextLevel > 0 ? (float)progression.CurrentXP / progression.XPToNextLevel : 0f;
    }

    public int GetCurrentMaxEnemies()
    {
        int level = progression != null ? progression.Level : 1;
        int levelsPast = level - 1;
        float scaled = config.baseMaxEnemies
            + config.maxEnemiesPerLevel * levelsPast
            + config.maxEnemiesPerLevelSquared * levelsPast * levelsPast;
        return Mathf.Min(config.maxEnemiesCap, Mathf.RoundToInt(scaled * enemyIntensity));
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

        if (roundRules != null)
        {
            SurvivorRoundResult? result = roundRules.OnEnemyKilled();
            if (result.HasValue)
            {
                EndRound(result.Value);
                return;
            }
        }

        if (config.killsPerBossWave > 0 && !IsBossActive && killCount % config.killsPerBossWave == 0)
            SpawnBoss();
    }

    public void RegisterBossDefeated(SurvivorBossEnemy boss, int xpReward)
    {
        activeBosses.Remove(boss);
        progression.AddXP(xpReward);
    }

    public void HandlePlayerDefeated()
    {
        if (minigamePlayer == null)
            return;

        if (roundRules != null)
        {
            SurvivorRoundResult? result = roundRules.OnPlayerDefeated();
            if (result.HasValue)
            {
                EndRound(result.Value);
                return;
            }
        }

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

    /// <summary>Instantly collects every XP gem currently on the field — the "Loot Magnet" instant item.</summary>
    public void CollectAllGems()
    {
        if (gemRoot == null)
            return;

        for (int i = gemRoot.childCount - 1; i >= 0; i--)
        {
            SurvivorXPGem gem = gemRoot.GetChild(i).GetComponent<SurvivorXPGem>();
            gem?.ForceCollect();
        }
    }

    /// <summary>Damages every current enemy (not the boss) at once — the "Nuke" instant item.</summary>
    public void NukeAllEnemies(float damage)
    {
        if (enemyRoot == null)
            return;

        for (int i = enemyRoot.childCount - 1; i >= 0; i--)
        {
            GameObject enemyObject = enemyRoot.GetChild(i).gameObject;
            if (enemyObject.GetComponent<ISurvivorDamageable>() == null)
                continue;

            SurvivorCombatFX.ApplyHit(enemyObject, damage, SurvivorElementType.None, Vector3.zero, 0f);
        }

        SurvivorCombatFX.Shake(1f);

        if (minigamePlayer != null)
            SurvivorExplosionFX.Play(minigamePlayer.transform.position, 12f, new Color(1f, 0.55f, 0.2f));
    }

    private void SpawnBoss()
    {
        if (config.bossPool == null || config.bossPool.Length == 0)
            return;

        SurvivorBossDataSO bossData = config.bossPool[Random.Range(0, config.bossPool.Length)];
        if (bossData == null)
            return;

        EnsureBossTemplate();

        float groundOffset = bossData.scale * 0.5f;
        Vector3 spawnOffset = -minigamePlayer.transform.forward * 8f;
        Vector3 spawnPosition = SurvivorGroundUtility.SnapToGround(
            minigamePlayer.transform.position + spawnOffset,
            config.groundMask,
            config.groundSnapRayHeight,
            groundOffset);

        GameObject bossObject = Instantiate(bossTemplate, spawnPosition, Quaternion.identity, enemyRoot);
        bossObject.SetActive(true);

        SurvivorBossEnemy boss = bossObject.GetComponent<SurvivorBossEnemy>();
        boss.Initialize(this, minigamePlayer.transform, bossData, config.groundMask, config.groundSnapRayHeight, groundOffset);
        activeBosses.Add(boss);

        if (statusText != null)
            statusText.text = $"BOSS INCOMING: {bossData.displayName}";
    }

    private void HandleLevelUp(int newLevel)
    {
        pendingLevelUps++;

        if (!IsUpgradeMenuOpen)
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

        if (inventorySync == null)
        {
            inventorySync = gameObject.GetComponent<SurvivorInventorySync>();
            if (inventorySync == null)
                inventorySync = gameObject.AddComponent<SurvivorInventorySync>();
            inventorySync.Initialize(this);
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

        healthBar = BuildBar(canvasObject.transform, "HealthBar", new Vector2(0f, 92f), new Color(0.35f, 0.95f, 0.45f));
        // No yellow/brown stamina bar — Power Core dots sit in the bottom HUD stack.
        staminaBar = null;
        HideLegacyStaminaBars();
        EnsureStaminaDotsHud(canvasObject.transform);
        xpBar = BuildBar(canvasObject.transform, "XPBar", new Vector2(0f, 46f), new Color(0.55f, 0.65f, 0.95f));
        levelBarText = BuildLabeledBar(canvasObject.transform, "LevelBar", new Vector2(0f, 24f), new Color(0.7f, 0.5f, 0.9f), "Lv. 1");
    }

    private void HideLegacyStaminaBars()
    {
        if (staminaBar != null)
            staminaBar.gameObject.SetActive(false);

        // Hide leftover overworld stamina sliders that sit in the middle-bottom HUD.
        Slider[] sliders = FindObjectsByType<Slider>(FindObjectsSortMode.None);
        for (int i = 0; i < sliders.Length; i++)
        {
            Slider s = sliders[i];
            if (s == null || s == healthBar || s == xpBar)
                continue;

            string n = s.gameObject.name.ToLowerInvariant();
            if (n.Contains("stamina") || n.Contains("energy"))
                s.gameObject.SetActive(false);
        }
    }

    private void EnsureStaminaDotsHud(Transform canvasTransform)
    {
        if (staminaUnitPrefab == null)
        {
#if UNITY_EDITOR
            staminaUnitPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/StaminaUnit.prefab");
#endif
        }

        // Reuse existing StaminaUI if present, but reparent into the middle-bottom HUD stack.
        if (staminaDotsUI == null)
            staminaDotsUI = FindFirstObjectByType<StaminaUI>();

        RectTransform dotsRect;
        if (staminaDotsUI != null)
        {
            dotsRect = staminaDotsUI.GetComponent<RectTransform>();
            if (dotsRect == null)
                dotsRect = staminaDotsUI.gameObject.AddComponent<RectTransform>();
            staminaDotsUI.transform.SetParent(canvasTransform, false);
        }
        else
        {
            GameObject dotsRoot = new GameObject("StaminaDots", typeof(RectTransform));
            dotsRoot.transform.SetParent(canvasTransform, false);
            dotsRect = dotsRoot.GetComponent<RectTransform>();

            HorizontalLayoutGroup layout = dotsRoot.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 6f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            staminaDotsUI = dotsRoot.AddComponent<StaminaUI>();
            staminaDotsUI.staminaUnitPrefab = staminaUnitPrefab;
            staminaDotsUI.staminaPerUnit = 10;
        }

        // Middle-bottom, above the HP / XP / level bars.
        dotsRect.anchorMin = new Vector2(0.5f, 0f);
        dotsRect.anchorMax = new Vector2(0.5f, 0f);
        dotsRect.pivot = new Vector2(0.5f, 0f);
        dotsRect.anchoredPosition = new Vector2(0f, 118f);
        dotsRect.sizeDelta = new Vector2(360f, 48f);

        // Prefer horizontal layout in the bottom stack. DestroyImmediate so we can add
        // HorizontalLayoutGroup in the same frame (Unity forbids both layout groups).
        VerticalLayoutGroup vertical = staminaDotsUI.GetComponent<VerticalLayoutGroup>();
        if (vertical != null)
            DestroyImmediate(vertical);

        HorizontalLayoutGroup horizontal = staminaDotsUI.GetComponent<HorizontalLayoutGroup>();
        if (horizontal == null)
            horizontal = staminaDotsUI.gameObject.AddComponent<HorizontalLayoutGroup>();

        if (horizontal != null)
        {
            horizontal.childAlignment = TextAnchor.MiddleCenter;
            horizontal.spacing = 6f;
            horizontal.childControlWidth = false;
            horizontal.childControlHeight = false;
            horizontal.childForceExpandWidth = false;
            horizontal.childForceExpandHeight = false;
        }

        if (staminaDotsUI.staminaUnitPrefab == null)
            staminaDotsUI.staminaUnitPrefab = staminaUnitPrefab;

        PlayerController player = worldPlayer != null ? worldPlayer : FindFirstObjectByType<PlayerController>();
        staminaDotsUI.player = player;
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
