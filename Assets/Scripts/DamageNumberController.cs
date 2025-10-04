using System.Collections.Generic;
using UnityEngine;

public class DamageNumberController : MonoBehaviour
{
    public static DamageNumberController instance { get; private set; }

    public static bool IsInstanceValid => instance != null;

    [SerializeField] private DamageNumber prefab;
    [SerializeField] private int poolSize = 20;


    private Queue<DamageNumber> damageNumberPool;
    private List<DamageNumber> activeDamageNumbers;

    private void Awake()
    {
        InitializeSingleton();
        InitializePool();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    // Belirtilen konumda bir hasar sayısı oluşturur.
    // Havuzdan bir nesne alır ve onu ayarlar.
    public void CreateNumber(float value, Vector3 location)
    {
        if (!IsValidSetup())
            return;

        DamageNumber damageNumber = GetPooledDamageNumber();
        if (damageNumber != null)
        {
            SetupDamageNumber(damageNumber, value, location);
        }
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

    // Başlangıçta belirlenen `poolSize` kadar nesneyi oluşturup havuza ekler.
    // Bu nesneler başlangıçta deaktif durumdadır ve kullanılmayı bekler.
    private void InitializePool()
    {
        damageNumberPool = new Queue<DamageNumber>();
        activeDamageNumbers = new List<DamageNumber>();

        for (int i = 0; i < poolSize; i++)
        {
            CreatePooledDamageNumber();
        }
    }

    private void CreatePooledDamageNumber()
    {
        DamageNumber damageNumber = Instantiate(prefab, transform);
        damageNumber.gameObject.SetActive(false);
        damageNumber.SetReturnToPoolCallback(ReturnToPool);
        damageNumberPool.Enqueue(damageNumber);
    }

    // Havuzdan bir DamageNumber nesnesi alır.
    // Eğer havuz boşsa, havuza yeni bir nesne ekler ve onu kullanır.
    // Bu, havuzun dinamik olarak büyümesini sağlar.
    private DamageNumber GetPooledDamageNumber()
    {
        if (damageNumberPool.Count == 0)
        {
            CreatePooledDamageNumber();
        }

        DamageNumber damageNumber = damageNumberPool.Dequeue();
        activeDamageNumbers.Add(damageNumber);
        return damageNumber;
    }

    // Kullanımı biten bir DamageNumber nesnesini havuza geri döndürür.
    // Nesneyi deaktif hale getirir ve tekrar kullanılmak üzere kuyruğa ekler.
    public void ReturnToPool(DamageNumber damageNumber)
    {
        if (damageNumber != null && activeDamageNumbers.Contains(damageNumber))
        {
            activeDamageNumbers.Remove(damageNumber);
            damageNumber.gameObject.SetActive(false);
            damageNumber.transform.SetParent(transform);
            damageNumberPool.Enqueue(damageNumber);
        }
    }

    private bool IsValidSetup()
    {
        return prefab != null && damageNumberPool != null;
    }

    // Havuzdan alınan DamageNumber nesnesini ayarlar. Konumunu, hasar değerini belirler ve aktifleştirir.
    private void SetupDamageNumber(DamageNumber damageNumber, float value, Vector3 location)
    {
        if (damageNumber != null)
        {
            damageNumber.transform.position = location;
            damageNumber.transform.rotation = Quaternion.identity;
            damageNumber.gameObject.SetActive(true);
            damageNumber.SetDamage(Mathf.RoundToInt(value));
            damageNumber.Initialize();
        }
    }
}