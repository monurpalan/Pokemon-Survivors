using System.Collections;
using UnityEngine;

public class Effect : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private float fallbackDuration = 1f;
    private float animationBuffer = 0.1f;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    private void Start()
    {
        StartCoroutine(DestroyAfterAnimation());
    }

    private IEnumerator DestroyAfterAnimation()
    {
        yield return new WaitForSeconds(GetAnimationDuration());
        DestroyEffect();
    }

    private float GetAnimationDuration()
    {
        if (animator != null)
        {
            return animator.GetCurrentAnimatorStateInfo(0).length - animationBuffer;
        }

        return fallbackDuration;
    }

    private void DestroyEffect()
    {
        Destroy(gameObject);
    }
}