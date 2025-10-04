using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
  public static PlayerController instance { get; private set; }

  public static bool IsInstanceValid => instance != null;

  [Header("Movement")]
  [SerializeField] private Rigidbody2D rb;
  [SerializeField] private float moveSpeed = 5f;
  [SerializeField] private Animator animator;

  [Header("UI")]
  [SerializeField] public Slider healthBar;
  [SerializeField] public Slider expBar;

  [HideInInspector] public Vector3 playerMoveDirection;
  [HideInInspector] public int playerHealth;
  [HideInInspector] public bool isImmune = false;
  [HideInInspector] public int experience;
  [HideInInspector] public int currentLevel;
  [HideInInspector] public int maxLevel;
  [HideInInspector] public List<int> playersLevel;

  public int maxHealth { get; private set; }

  private Collider2D playerCollider;
  private SpriteRenderer spriteRenderer;
  private bool useButtonInput = false;

  private float immuneTime = 2f;
  private int defaultMaxHealth = 10;
  private int baseExperienceRequirement = 100;
  private float levelExperienceMultiplier = 3f;
  private int fallbackExpRequirement = 100;

  private float currentImmuneTime = 0f;

  private void Awake()
  {
    InitializeSingleton();
    InitializeComponents();
    InitializePlayerStats();
    InitializeUI();
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
    if (instance != this)
      return;

    SetupLevelProgression();
    UpdateExperienceBar();
  }

  private void Update()
  {
    HandleInput();
    ProcessImmunity();
  }

  private void FixedUpdate()
  {
    ApplyMovement();
    UpdateAnimations();
  }

  public void ActivateImmune()
  {
    SetImmuneVisualState();
    StartImmunity();
  }

  public void Experience(int exp)
  {
    AddExperience(exp);
    CheckForLevelUp();
  }

  public void ButtonUp()
  {
    SetDirectionalInput(Vector3.up, 0, 1);
  }

  public void ButtonDown()
  {
    SetDirectionalInput(Vector3.down, 0, -1);
  }

  public void ButtonLeft()
  {
    SetDirectionalInput(Vector3.left, -1, 0);
  }

  public void ButtonRight()
  {
    SetDirectionalInput(Vector3.right, 1, 0);
  }

  public void ButtonRelease()
  {
    ReleaseDirectionalInput();
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

  private void InitializeComponents()
  {
    spriteRenderer = GetComponent<SpriteRenderer>();
    playerCollider = GetComponent<Collider2D>();

    if (rb == null)
      rb = GetComponent<Rigidbody2D>();

    if (animator == null)
      animator = GetComponent<Animator>();
  }

  private void InitializePlayerStats()
  {
    InitializeLevelSystem();
    maxHealth = defaultMaxHealth;
    playerHealth = maxHealth;
    experience = 0;
    currentLevel = 0;
  }

  private void InitializeLevelSystem()
  {
    if (playersLevel == null)
    {
      playersLevel = new List<int>();
    }

    if (playersLevel.Count == 0)
    {
      playersLevel.Add(baseExperienceRequirement);
    }
  }

  private void InitializeUI()
  {
    SetupHealthBar();
    SetupExperienceBar();
  }

  private void SetupHealthBar()
  {
    if (healthBar != null)
    {
      healthBar.maxValue = maxHealth;
      healthBar.value = playerHealth;
    }
  }

  private void SetupExperienceBar()
  {
    if (expBar != null)
    {
      if (currentLevel < playersLevel.Count)
      {
        expBar.maxValue = playersLevel[currentLevel];
        expBar.value = experience;
      }
      else
      {
        expBar.maxValue = fallbackExpRequirement;
        expBar.value = 0;
      }
    }
  }

  private void SetupLevelProgression()
  {
    if (playersLevel == null)
    {
      playersLevel = new List<int> { baseExperienceRequirement };
    }

    GenerateAdditionalLevels();
  }

  private void GenerateAdditionalLevels()
  {
    int levelsToAdd = Mathf.Max(0, maxLevel - playersLevel.Count + 1);

    for (int i = 0; i < levelsToAdd; i++)
    {
      int nextLevelExp = CalculateNextLevelRequirement();
      playersLevel.Add(nextLevelExp);
    }
  }

  private int CalculateNextLevelRequirement()
  {
    if (playersLevel.Count > 0)
    {
      return Mathf.CeilToInt(playersLevel[playersLevel.Count - 1] * levelExperienceMultiplier);
    }
    return baseExperienceRequirement;
  }

  private void UpdateExperienceBar()
  {
    if (expBar != null && currentLevel < playersLevel.Count)
    {
      expBar.maxValue = playersLevel[currentLevel];
      expBar.value = experience;
    }
  }

  private void HandleInput()
  {
    if (!useButtonInput)
    {
      ProcessKeyboardInput();
    }
  }

  private void ProcessKeyboardInput()
  {
    float inputX = Input.GetAxisRaw("Horizontal");
    float inputY = Input.GetAxisRaw("Vertical");
    playerMoveDirection = new Vector3(inputX, inputY).normalized;
    UpdateAnimatorParameters(inputX, inputY);
  }

  private void UpdateAnimatorParameters(float inputX, float inputY)
  {
    if (animator != null)
    {
      animator.SetFloat("moveX", inputX);
      animator.SetFloat("moveY", inputY);
    }
  }

  private void ApplyMovement()
  {
    if (rb != null)
    {
      Vector2 velocity = CalculateMovementVelocity();
      rb.velocity = velocity;
    }
  }

  private Vector2 CalculateMovementVelocity()
  {
    if (playerMoveDirection != Vector3.zero)
    {
      return new Vector2(playerMoveDirection.x * moveSpeed, playerMoveDirection.y * moveSpeed);
    }
    return Vector2.zero;
  }

  private void UpdateAnimations()
  {
    if (animator != null && rb != null)
    {
      animator.SetBool("isMoving", rb.velocity != Vector2.zero);
    }
  }

  // Oyuncunun hasar aldıktan sonra belirli bir süre hasar almaz hale gelmesini yönetir.
  // `isImmune` true olduğunda, zamanlayıcı çalışır ve süre dolduğunda bağışıklık sona erer.
  private void ProcessImmunity()
  {
    if (isImmune)
    {
      UpdateImmunityTimer();
      CheckImmunityExpiration();
    }
  }

  private void UpdateImmunityTimer()
  {
    currentImmuneTime -= Time.deltaTime;
  }

  private void CheckImmunityExpiration()
  {
    if (currentImmuneTime <= 0f)
    {
      EndImmunity();
    }
  }

  private void StartImmunity()
  {
    isImmune = true;
    currentImmuneTime = immuneTime;
  }

  private void EndImmunity()
  {
    isImmune = false;
    RestoreColliderState();
    RestoreNormalVisualState();
  }

  private void SetImmuneVisualState()
  {
    if (spriteRenderer != null)
    {
      spriteRenderer.color = Color.red;
    }
  }

  private void RestoreNormalVisualState()
  {
    if (spriteRenderer != null)
    {
      spriteRenderer.color = Color.white;
    }
  }

  private void RestoreColliderState()
  {
    if (playerCollider != null)
    {
      playerCollider.isTrigger = false;
    }
  }

  private void AddExperience(int exp)
  {
    experience += exp;
    UpdateExperienceUI();
  }

  private void UpdateExperienceUI()
  {
    if (expBar != null)
    {
      expBar.value = experience;
    }
  }

  private void CheckForLevelUp()
  {
    if (CanLevelUp())
    {
      ProcessLevelUp();
    }
  }

  private bool CanLevelUp()
  {
    return currentLevel < playersLevel.Count && experience >= playersLevel[currentLevel];
  }

  // Seviye atlama işlemini gerçekleştirir. Seviyeyi artırır, yükseltme panelini tetikler ve EXP çubuğunu günceller.
  private void ProcessLevelUp()
  {
    currentLevel++;
    TriggerUpgradePanel();
    UpdateExperienceBarForNewLevel();
  }

  private void TriggerUpgradePanel()
  {
    if (Weapons.instance != null)
    {
      Weapons.instance.UpgradePanel();
    }
  }

  private void UpdateExperienceBarForNewLevel()
  {
    if (expBar != null && currentLevel < playersLevel.Count)
    {
      expBar.maxValue = playersLevel[currentLevel];
    }
  }

  private void SetDirectionalInput(Vector3 direction, float moveX, float moveY)
  {
    useButtonInput = true;
    playerMoveDirection = direction;
    UpdateAnimatorParameters(moveX, moveY);
  }

  private void ReleaseDirectionalInput()
  {
    playerMoveDirection = Vector3.zero;
    useButtonInput = false;

    if (animator != null)
    {
      animator.SetBool("isMoving", false);
    }
  }
}