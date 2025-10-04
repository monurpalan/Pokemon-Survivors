using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner instance { get; private set; }

    public static bool IsInstanceValid => instance != null;

    // Spawner sınıfı, her bir düşman dalgasının özelliklerini tanımlar.
    [System.Serializable]
    public class Spawner
    {
        [Header("Enemy Configuration")]
        public GameObject enemyPrefab;
        public int enemyHealth;
        public int enemyDamage;

        [Header("Spawn Settings")]
        public float spawnInterval = 2f;
        public int enemiesPerWave;

        [Header("Runtime Data")]
        public float timer;
        public int spawnedEnemies;
    }

    [SerializeField] public List<Spawner> spawners = new List<Spawner>();
    [SerializeField] private Camera mainCamera;

    public int waveNumber = 1;

    private float spawnAreaWidth = 32f;
    private float spawnOffsetY = 10f;
    private const float MIN_SPAWN_INTERVAL = 0.5f;
    private float difficultyMultiplier = 0.9f;

    private void Awake()
    {
        InitializeSingleton();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Update()
    {
        ProcessSpawning();
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

    private void ProcessSpawning()
    {
        if (!IsValidSpawnerConfiguration())
            return;

        Spawner currentSpawner = GetCurrentSpawner();
        if (currentSpawner == null)
            return;

        UpdateSpawnTimer(currentSpawner);
        CheckForSpawn(currentSpawner);
        CheckForWaveCompletion(currentSpawner);
    }

    private bool IsValidSpawnerConfiguration()
    {
        return spawners != null &&
               spawners.Count > 0 &&
               waveNumber < spawners.Count;
    }

    private bool CanSpawnEnemy(Spawner spawner)
    {
        return spawner != null &&
               spawner.enemyPrefab != null &&
               mainCamera != null;
    }

    private Spawner GetCurrentSpawner()
    {
        return spawners[waveNumber];
    }

    private void UpdateSpawnTimer(Spawner spawner)
    {
        spawner.timer += Time.deltaTime;
    }

    private void ResetSpawnTimer(Spawner spawner)
    {
        spawner.timer = 0f;
    }

    // Zamanlayıcı, spawn aralığına ulaştığında yeni bir düşman oluşturur.
    private void CheckForSpawn(Spawner spawner)
    {
        if (spawner.timer >= spawner.spawnInterval)
        {
            SpawnEnemy(spawner);
            ResetSpawnTimer(spawner);
        }
    }

    // Mevcut dalga için gereken düşman sayısı oluşturulduğunda bir sonraki dalgaya geçer.
    private void CheckForWaveCompletion(Spawner spawner)
    {
        if (spawner.spawnedEnemies >= spawner.enemiesPerWave)
        {
            AdvanceToNextWave();
        }
    }

    private void SpawnEnemy(Spawner spawner)
    {
        if (!CanSpawnEnemy(spawner))
            return;

        Vector3 spawnPosition = GenerateSpawnPosition();
        CreateEnemyInstance(spawner, spawnPosition);
        IncrementSpawnCount(spawner);
    }

    // Düşmanların ekranın dışında, rastgele bir konumda oluşmasını sağlar.
    private Vector3 GenerateSpawnPosition()
    {
        float randomX = Random.Range(-spawnAreaWidth * 0.5f, spawnAreaWidth * 0.5f);
        // Ekranın üstünde veya altında rastgele bir y konumu seçer.
        float yOffset = Random.Range(0, 2) == 0 ? -spawnOffsetY : spawnOffsetY;

        return new Vector3(
            randomX,
            mainCamera.transform.position.y + yOffset,
            0f
        );
    }

    private void CreateEnemyInstance(Spawner spawner, Vector3 position)
    {
        Instantiate(spawner.enemyPrefab, position, Quaternion.identity);
    }

    private void IncrementSpawnCount(Spawner spawner)
    {
        spawner.spawnedEnemies++;
    }

    // Bir sonraki dalgaya geçişi yönetir.
    private void AdvanceToNextWave()
    {
        if (!IsValidSpawnerConfiguration())
            return;

        IncrementWaveNumber();
        HandleWaveRollover();
        ResetNewWaveSpawnCount();
    }

    private void IncrementWaveNumber()
    {
        waveNumber++;
    }

    // Eğer son dalga tamamlandıysa, dalgaları başa sarar ve zorluğu artırır.
    private void HandleWaveRollover()
    {
        if (waveNumber >= spawners.Count)
        {
            waveNumber = 0;
            IncreaseDifficulty();
        }
    }

    private void ResetNewWaveSpawnCount()
    {
        if (waveNumber < spawners.Count && spawners[waveNumber] != null)
        {
            spawners[waveNumber].spawnedEnemies = 0;
        }
    }

    // Tüm dalgalar tamamlandığında oyunun zorluğunu artırır.
    // Bu örnekte, düşmanların spawn olma aralığını kısaltarak zorluk artırılıyor.
    private void IncreaseDifficulty()
    {
        if (!IsValidSpawnerConfiguration())
            return;

        foreach (var spawner in spawners)
        {
            if (spawner != null)
            {
                AdjustSpawnerDifficulty(spawner);
            }
        }
    }

    private void AdjustSpawnerDifficulty(Spawner spawner)
    {
        spawner.spawnInterval = Mathf.Max(MIN_SPAWN_INTERVAL, spawner.spawnInterval * difficultyMultiplier);
    }
}