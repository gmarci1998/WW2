using System.Collections;
using UnityEngine;

public class ShotAnimationController : MonoBehaviour
{
    [SerializeField] float sparkDuration = 0.2f;
    [SerializeField] float shakeDuration = 0.5f;
    [SerializeField] float shakeMagnitude = 2f;
    [SerializeField] GameObject gun;
    [SerializeField] GameObject map;
    [SerializeField] GameObject spark;

    Coroutine gunShakeCoroutine;
    Coroutine mapShakeCoroutine;
    Coroutine sparkCoroutine;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Shake()
    {
        // Shake the map and shake the gun
        if (gun != null)
        {
            if (gunShakeCoroutine != null) StopCoroutine(gunShakeCoroutine);
            gunShakeCoroutine = StartCoroutine(ShakeTransform(gun.transform, shakeDuration, shakeMagnitude));
        }

        if (map != null)
        {
            if (mapShakeCoroutine != null) StopCoroutine(mapShakeCoroutine);
            // often a larger object looks better with a slightly different magnitude
            mapShakeCoroutine = StartCoroutine(ShakeTransform(map.transform, shakeDuration, shakeMagnitude * 0.6f));
        }

        if (spark != null)
        {
            // briefly enable the spark object if it's a scene object; if it's a prefab you'd Instantiate instead
            spark.SetActive(true);
            if (sparkCoroutine != null) StopCoroutine(sparkCoroutine);
            sparkCoroutine = StartCoroutine(DisableSparkAfter(shakeDuration));
        }
    }

    IEnumerator ShakeTransform(Transform target, float duration, float magnitude)
    {
        Vector3 originalLocalPos = target.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            target.localPosition = originalLocalPos + new Vector3(x, y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        target.localPosition = originalLocalPos;

        // clear coroutine handle so subsequent calls know it's finished
        if (target == gun?.transform) gunShakeCoroutine = null;
        if (target == map?.transform) mapShakeCoroutine = null;
    }

    IEnumerator DisableSparkAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (spark != null) spark.SetActive(false);
        sparkCoroutine = null;
    }
}
