using System.Collections;
using UnityEngine;

public class SpellEffectVisual : MonoBehaviour
{
    public float duration = 0.15f;

    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    public void PlayBetweenPoints(Vector3 startPos, Vector3 endPos)
    {
        transform.position = (startPos + endPos) * 0.5f;

        Vector3 direction = endPos - startPos;
        float length = direction.magnitude;

        transform.right = direction.normalized;
        transform.localScale = new Vector3(length, originalScale.y, originalScale.z);

        StartCoroutine(DestroyAfterTime());
    }

    public void PlayAtPoint(Vector3 position, float scaleMultiplier = 1f)
    {
        transform.position = position;
        transform.localScale = originalScale * scaleMultiplier;

        StartCoroutine(DestroyAfterTime());
    }

    private IEnumerator DestroyAfterTime()
    {
        yield return new WaitForSeconds(duration);
        Destroy(gameObject);
    }
}