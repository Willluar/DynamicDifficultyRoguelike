using System;
using System.Collections;
using UnityEngine;

public class SpellProjectileVisual : MonoBehaviour
{
    public void Launch(Vector3 startPos, Vector3 endPos, float travelTime, Action onHit)
    {
        StartCoroutine(TravelRoutine(startPos, endPos, travelTime, onHit));
    }

    private IEnumerator TravelRoutine(Vector3 startPos, Vector3 endPos, float travelTime, Action onHit)
    {
        float elapsed = 0f;
        transform.position = startPos;

        while (elapsed < travelTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / travelTime);
            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        transform.position = endPos;

        onHit?.Invoke();

        Destroy(gameObject);
    }
}