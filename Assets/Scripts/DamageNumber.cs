using System;
using TMPro;
using UnityEngine;

public class DamageNumber : MonoBehaviour
{
    [SerializeField] private TMP_Text damageText;

    private float lifetime = 1f;
    private const float MIN_FLOAT_SPEED = 0.1f;
    private const float MAX_FLOAT_SPEED = 1f;

    private float floatSpeed;
    private float currentLifetime;
    private Action<DamageNumber> returnToPoolCallback;

    private void Update()
    {
        FloatUpward();
        UpdateLifetime();
    }

    public void SetDamage(int value)
    {
        if (damageText != null)
        {
            damageText.text = value.ToString();
        }
    }

    public void SetReturnToPoolCallback(Action<DamageNumber> callback)
    {
        returnToPoolCallback = callback;
    }

    // Hasar sayısı yeniden kullanılacağı zaman çağrılır. Ömrünü ve hızını sıfırlar.
    public void Initialize()
    {
        floatSpeed = UnityEngine.Random.Range(MIN_FLOAT_SPEED, MAX_FLOAT_SPEED);
        currentLifetime = lifetime;
    }

    private void FloatUpward()
    {
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;
    }

    // Nesnenin ekranda kalma süresini yönetir. Süre dolduğunda havuza geri döner.
    private void UpdateLifetime()
    {
        currentLifetime -= Time.deltaTime;
        if (currentLifetime <= 0f)
        {
            ReturnToPool();
        }
    }

    // Nesneyi havuza geri döndürür.
    // Eğer bir callback ayarlanmışsa onu çağırır, yoksa nesneyi yok eder.
    private void ReturnToPool()
    {
        if (returnToPoolCallback != null)
        {
            returnToPoolCallback(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}