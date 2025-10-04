using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapons : MonoBehaviour
{
    public static Weapons instance { get; private set; }

    public static bool IsInstanceValid => instance != null;

    [System.Serializable]
    public class Weapon
    {
        [Header("Weapon Configuration")]
        public GameObject weaponPrefab;
        public Vector3 scale = new Vector3(0.5f, 0.5f, 0.5f);
        public int damage = 1;

        [Header("Timing")]
        public float duration = 10f;
        public float stayTime = 2f;
    }

    [SerializeField] public List<Weapon> weapons = new List<Weapon>();
    [SerializeField] private GameObject upgradePanel;

    public List<EnemyController> enemiesInRange = new List<EnemyController>();
    public int currentWeaponIndex = 0;

    private GameObject currentWeapon;
    private float initialDuration;
    private bool isHiding = false;
    private float damageCounter;

    private float playerControllerTimeout = 5f;
    private float pollInterval = 0.1f;
    private float damageInterval = 0.5f;
    private float scalingSpeed = 3f;
    private float scaleThreshold = 0.01f;

    private readonly Vector3 weaponOffset = new Vector3(0, 0.5f, 0);
    private readonly Vector3 minScale = new Vector3(0.05f, 0.05f, 0.05f);

    private void Awake()
    {
        InitializeSingleton();
        InitializeCollections();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Start()
    {
        InitializeWeaponSystem();
    }

    private void Update()
    {
        ProcessWeaponBehavior();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        HandleEnemyEnter(collision);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        HandleEnemyExit(collision);
    }

    public void UpgradePanel()
    {
        ShowUpgradePanel();
    }

    public void UpgradeWeapon()
    {
        ProcessWeaponUpgrade();
        HideUpgradePanel();
    }

    private void InitializeSingleton()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void InitializeCollections()
    {
        if (enemiesInRange == null)
        {
            enemiesInRange = new List<EnemyController>();
        }

        if (weapons == null)
        {
            weapons = new List<Weapon>();
        }
    }

    private void InitializeWeaponSystem()
    {
        if (!IsValidWeaponConfiguration())
        {
            DisableComponent();
            return;
        }

        if (PlayerController.instance == null)
        {
            StartCoroutine(WaitForPlayerController());
            return;
        }

        CreateInitialWeapon();
    }

    private bool IsValidWeaponConfiguration()
    {
        return weapons != null &&
               weapons.Count > 0 &&
               IsValidWeaponIndex() &&
               IsValidWeaponData();
    }

    private bool IsValidWeaponIndex()
    {
        if (currentWeaponIndex >= weapons.Count || currentWeaponIndex < 0)
        {
            currentWeaponIndex = 0;
        }
        return currentWeaponIndex < weapons.Count;
    }

    private bool IsValidWeaponData()
    {
        return weapons[currentWeaponIndex] != null &&
               weapons[currentWeaponIndex].weaponPrefab != null;
    }

    private bool IsValidUpdateState()
    {
        return weapons != null && currentWeaponIndex < weapons.Count;
    }

    private bool IsEnemyCollision(Collider2D collision)
    {
        return collision != null && collision.CompareTag("Enemy");
    }

    private bool IsValidEnemyInteraction(EnemyController enemy)
    {
        return enemy != null &&
               weapons != null &&
               currentWeaponIndex < weapons.Count;
    }

    private bool CanProcessDamage()
    {
        return weapons != null &&
               currentWeaponIndex < weapons.Count &&
               enemiesInRange != null;
    }

    private bool IsWeaponActive()
    {
        return currentWeapon != null &&
               weapons != null &&
               currentWeaponIndex < weapons.Count;
    }

    private bool ShouldHideWeapon()
    {
        return weapons[currentWeaponIndex].duration <= 0 && !isHiding;
    }

    private bool ShouldContinueScaling(Vector3 targetScale)
    {
        return currentWeapon != null &&
               Vector3.Distance(currentWeapon.transform.localScale, targetScale) > scaleThreshold;
    }

    private bool CanUpgradeWeapon()
    {
        return weapons != null && weapons.Count > 0;
    }

    private void CreateInitialWeapon()
    {
        StoreInitialDuration();
        InstantiateWeapon();
        StartWeaponBehavior();
    }

    private void StoreInitialDuration()
    {
        initialDuration = weapons[currentWeaponIndex].duration;
    }

    private void InstantiateWeapon()
    {
        currentWeapon = Instantiate(
            weapons[currentWeaponIndex].weaponPrefab,
            PlayerController.instance.transform.position,
            Quaternion.identity,
            PlayerController.instance.transform
        );

        if (currentWeapon != null)
        {
            currentWeapon.transform.localScale = weapons[currentWeaponIndex].scale;
        }
    }

    private void StartWeaponBehavior()
    {
        if (currentWeapon != null)
        {
            StartCoroutine(ManageWeaponLifecycle());
        }
    }

    private void DisableComponent()
    {
        enabled = false;
    }

    private void ProcessWeaponBehavior()
    {
        if (!IsValidUpdateState())
            return;

        ProcessDamageCounter();
        UpdateWeaponPosition();
        UpdateWeaponDuration();
    }

    private void UpdateWeaponPosition()
    {
        if (currentWeapon != null && PlayerController.instance != null)
        {
            currentWeapon.transform.position = PlayerController.instance.transform.position + weaponOffset;
        }
    }

    private void UpdateWeaponDuration()
    {
        if (weapons[currentWeaponIndex].duration > 0)
        {
            weapons[currentWeaponIndex].duration -= Time.deltaTime;
        }
    }

    private void HandleEnemyEnter(Collider2D collision)
    {
        if (!IsEnemyCollision(collision))
            return;

        EnemyController enemy = GetEnemyController(collision);
        if (IsValidEnemyInteraction(enemy))
        {
            AddEnemyToRange(enemy);
            DealDamageToEnemy(enemy);
        }
    }

    private void HandleEnemyExit(Collider2D collision)
    {
        if (!IsEnemyCollision(collision))
            return;

        EnemyController enemy = GetEnemyController(collision);
        if (enemy != null && enemiesInRange != null)
        {
            RemoveEnemyFromRange(enemy);
        }
    }

    private EnemyController GetEnemyController(Collider2D collision)
    {
        return collision.GetComponent<EnemyController>();
    }

    private void AddEnemyToRange(EnemyController enemy)
    {
        enemiesInRange.Add(enemy);
    }

    private void RemoveEnemyFromRange(EnemyController enemy)
    {
        enemiesInRange.Remove(enemy);
    }

    private void DealDamageToEnemy(EnemyController enemy)
    {
        enemy.TakeDamage(weapons[currentWeaponIndex].damage);
    }

    private void ProcessDamageCounter()
    {
        if (!CanProcessDamage())
            return;

        damageCounter -= Time.deltaTime;
        if (damageCounter <= 0)
        {
            ResetDamageCounter();
            DamageAllEnemiesInRange();
        }
    }

    private void ResetDamageCounter()
    {
        damageCounter = damageInterval;
    }

    private void DamageAllEnemiesInRange()
    {
        for (int i = enemiesInRange.Count - 1; i >= 0; i--)
        {
            if (enemiesInRange[i] != null)
            {
                DealDamageToEnemy(enemiesInRange[i]);
            }
            else
            {
                enemiesInRange.RemoveAt(i);
            }
        }
    }

    private IEnumerator WaitForPlayerController()
    {
        float elapsed = 0f;

        while (PlayerController.instance == null && elapsed < playerControllerTimeout)
        {
            yield return new WaitForSeconds(pollInterval);
            elapsed += pollInterval;
        }

        if (PlayerController.instance != null)
        {
            CreateInitialWeapon();
        }
        else
        {
            DisableComponent();
        }
    }

    // Silahın yaşam döngüsünü yöneten ana Coroutine.
    // Silahın aktif kalma süresini, gizlenmesini, bekleme süresini ve tekrar görünmesini kontrol eder.
    // Bu döngü, silah aktif olduğu sürece devam eder.
    private IEnumerator ManageWeaponLifecycle()
    {
        while (IsWeaponActive())
        {
            if (ShouldHideWeapon())
            {
                yield return StartCoroutine(HideWeapon());
                yield return StartCoroutine(WeaponCooldown());
                yield return StartCoroutine(ShowWeapon());
            }
            yield return null;
        }
    }

    // Silahı yavaşça küçülterek gizleme efekti yaratır ve ardından deaktif eder.
    private IEnumerator HideWeapon()
    {
        yield return StartCoroutine(ScaleWeaponTo(minScale));

        if (currentWeapon != null)
        {
            isHiding = true;
            currentWeapon.SetActive(false);
        }
    }

    // Silahın gizlendikten sonra ne kadar süre bekleyeceğini belirler.
    private IEnumerator WeaponCooldown()
    {
        yield return new WaitForSeconds(weapons[currentWeaponIndex].stayTime);
    }

    // Silahı tekrar aktifleştirir ve orijinal boyutuna yavaşça büyüterek görünür hale getirir.
    private IEnumerator ShowWeapon()
    {
        if (currentWeapon != null)
        {
            currentWeapon.SetActive(true);
            ResetWeaponDuration();
            yield return StartCoroutine(ScaleWeaponTo(weapons[currentWeaponIndex].scale));
            isHiding = false;
        }
    }

    // Silahı hedef bir boyuta doğru yavaşça ölçeklendirir (büyütür veya küçültür).
    private IEnumerator ScaleWeaponTo(Vector3 targetScale)
    {
        while (ShouldContinueScaling(targetScale))
        {
            ApplyScaling(targetScale);
            yield return null;
        }

        FinalizeScale(targetScale);
    }

    private void ApplyScaling(Vector3 targetScale)
    {
        currentWeapon.transform.localScale = Vector3.Lerp(
            currentWeapon.transform.localScale,
            targetScale,
            Time.deltaTime * scalingSpeed
        );
    }

    private void FinalizeScale(Vector3 targetScale)
    {
        if (currentWeapon != null)
        {
            currentWeapon.transform.localScale = targetScale;
        }
    }

    private void ResetWeaponDuration()
    {
        weapons[currentWeaponIndex].duration = initialDuration;
    }

    private void ShowUpgradePanel()
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(true);
        }

        if (GameManager.IsInstanceValid)
        {
            GameManager.instance.Pause(true);
        }
    }

    private void HideUpgradePanel()
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
        }

        if (GameManager.IsInstanceValid)
        {
            GameManager.instance.Pause(false);
        }
    }

    private void ProcessWeaponUpgrade()
    {
        if (CanUpgradeWeapon())
        {
            AdvanceWeaponIndex();
        }
    }

    private void AdvanceWeaponIndex()
    {
        currentWeaponIndex = Mathf.Min(currentWeaponIndex + 1, weapons.Count - 1);
    }
}