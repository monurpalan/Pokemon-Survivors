using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public Vector3 enemyMoveDirection;
    public int damageAmount;

    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float moveSpeed;
    [SerializeField] private Effect destroyEffect;
    [SerializeField] public int health;
    [SerializeField] private int giveExperience;
    [SerializeField] private float pushtime;

    private SpriteRenderer spriteRenderer;
    private float pushcounter;
    private int defaultHealth = 10;

    private void Awake()
    {
        InitializeComponents();
        InitializeHealth();
    }

    private void Update()
    {
        UpdateMoveDirection();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        HandlePlayerCollision(collision);
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        ShowDamageNumber(damage);
        ApplyKnockback();

        if (health <= 0)
        {
            Die();
        }
    }

    private void InitializeComponents()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void InitializeHealth()
    {
        if (IsEnemySpawnerValid())
        {
            health = EnemySpawner.instance.spawners[EnemySpawner.instance.waveNumber].enemyHealth;
        }
        else
        {
            health = defaultHealth;
        }
    }

    private bool IsEnemySpawnerValid()
    {
        return EnemySpawner.instance != null &&
               EnemySpawner.instance.spawners != null &&
               EnemySpawner.instance.waveNumber < EnemySpawner.instance.spawners.Count;
    }

    private bool IsValidPlayerCollision(Collision2D collision)
    {
        return collision.gameObject.CompareTag("Player") &&
               PlayerController.instance != null &&
               EnemySpawner.instance != null &&
               !PlayerController.instance.isImmune;
    }

    private void UpdateMoveDirection()
    {
        if (PlayerController.instance != null)
        {
            enemyMoveDirection = (PlayerController.instance.transform.position - transform.position).normalized;
        }
        else
        {
            enemyMoveDirection = Vector3.zero;
        }
    }

    private void HandleMovement()
    {
        if (PlayerController.instance == null)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        HandleKnockback();
        UpdateSpriteDirection();
        ApplyMovement();
    }

    private void HandleKnockback()
    {
        if (pushcounter > 0)
        {
            pushcounter -= Time.fixedDeltaTime;

            if (moveSpeed > 0)
            {
                moveSpeed = -moveSpeed;
            }

            if (pushcounter <= 0)
            {
                moveSpeed = Mathf.Abs(moveSpeed);
            }
        }
    }

    private void UpdateSpriteDirection()
    {
        spriteRenderer.flipX = PlayerController.instance.transform.position.x - transform.position.x >= 0;
    }

    private void ApplyMovement()
    {
        rb.velocity = enemyMoveDirection != Vector3.zero
            ? new Vector2(enemyMoveDirection.x * moveSpeed, enemyMoveDirection.y * moveSpeed)
            : Vector2.zero;
    }

    private void ApplyKnockback()
    {
        pushcounter = pushtime;
    }

    private void HandlePlayerCollision(Collision2D collision)
    {
        if (!IsValidPlayerCollision(collision))
            return;

        DamagePlayer();
        PlayerController.instance.ActivateImmune();
        UpdatePlayerHealthUI();
        CheckPlayerDeath();
    }

    private void DamagePlayer()
    {
        if (IsEnemySpawnerValid())
        {
            int damage = EnemySpawner.instance.spawners[EnemySpawner.instance.waveNumber].enemyDamage;
            PlayerController.instance.playerHealth -= damage;
        }
    }

    private void UpdatePlayerHealthUI()
    {
        if (PlayerController.instance.healthBar != null)
        {
            PlayerController.instance.healthBar.value = PlayerController.instance.playerHealth;
        }
    }

    private void CheckPlayerDeath()
    {
        if (PlayerController.instance.playerHealth <= 0)
        {
            PlayerController.instance.playerHealth = 0;

            if (GameManager.instance != null)
            {
                GameManager.instance.Pause(true);
            }

            PlayerController.instance.gameObject.SetActive(false);
        }
    }

    private void ShowDamageNumber(int damage)
    {
        if (DamageNumberController.instance != null)
        {
            DamageNumberController.instance.CreateNumber(damage, transform.position);
        }
    }

    private void SpawnDestroyEffect()
    {
        if (destroyEffect != null)
        {
            Instantiate(destroyEffect, transform.position, Quaternion.identity);
        }
    }

    private void Die()
    {
        GiveExperienceToPlayer();
        SpawnDestroyEffect();
        Destroy(gameObject);
    }

    private void GiveExperienceToPlayer()
    {
        if (PlayerController.instance != null)
        {
            PlayerController.instance.Experience(giveExperience);
        }
    }
}