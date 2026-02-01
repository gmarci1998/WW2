using UnityEngine;

public class ShotController : MonoBehaviour
{
    [SerializeField] GameObject crossHair;
    [SerializeField] float uiHitRadius = 20f; // pixels threshold for "hitting" a UI point

    [SerializeField] float maxDistance = 100f;
    [SerializeField] LayerMask hitMask = ~0;
    [SerializeField] Camera mainCamera;

    // New: radius for a sphere cast (adds "thickness" to the ray)
    [SerializeField] float shotRadius = 0.5f;

    // New: when true, will return all hits along the sphere cast (useful for area effects)
    [SerializeField] bool useSphereCastAll = false;
    [SerializeField] float shotCoolDown = 1f;

    private float shotCoolDownTimer = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (crossHair == null)
            Debug.LogWarning("ShotController: crossHair is not assigned.");
    }

    // Update is called once per frame
    void Update()
    {
        if(shotCoolDownTimer > 0f)
        {
            shotCoolDownTimer -= Time.deltaTime;
        }
        else if (Input.GetMouseButtonDown(0))
        {
            FindFirstObjectByType<ShotAnimationController>().Shake();
            ShootFromCrosshair();
            shotCoolDownTimer = shotCoolDown;
        }
    }

    void ShootFromCrosshair()
    {
        if (mainCamera == null || crossHair == null)
            return;

        // --- 2D detection: cast forward from the crosshair and check for a SpriteRenderer hit (CapsuleCollider2D) ---
        // Uses Physics2D.* which works with 2D colliders (e.g., CapsuleCollider2D).
        Vector3 origin3 = crossHair.transform.position;
        Vector3 forward3 = crossHair.transform.forward; // "forward" from the crosshair GameObject
        Vector2 origin2 = new Vector2(origin3.x, origin3.y);

        // Convert 3D forward to 2D direction (XY). If forward has almost zero XY, try using up as fallback.
        Vector2 dir2 = new Vector2(forward3.x, forward3.y);
        if (dir2.sqrMagnitude < 1e-6f)
        {
            // fallback: use local up (useful if your crosshair is oriented in Z axis)
            Vector3 up3 = crossHair.transform.up;
            dir2 = new Vector2(up3.x, up3.y);
            if (dir2.sqrMagnitude < 1e-6f)
            {
                // final fallback: to the right
                dir2 = Vector2.right;
            }
        }

        // Visualize 2D cast in world space (draw using the 3D forward so it appears in Scene view)
        Debug.DrawRay(origin3, forward3 * maxDistance, Color.yellow, 1.0f);

        int layerMask2D = hitMask; // LayerMask implicitly converts to int for Physics2D

        if (shotRadius <= 0f)
        {
            // Raycast in 2D
            RaycastHit2D hit2D = Physics2D.Raycast(origin2, dir2.normalized, maxDistance, layerMask2D);
            if (hit2D.collider != null)
            {
                var sr = hit2D.collider.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Debug.Log($"2D Raycast hit SpriteRenderer: {hit2D.collider.name} at {hit2D.point}");
                    Debug.DrawLine(origin3, new Vector3(hit2D.point.x, hit2D.point.y, origin3.z), Color.green, 1.0f);

                    // TODO: apply damage/effects to the 2D target here
                    sr.GetComponent<EnemyHead>()?.Hit();
                    return;
                }
                else
                {
                    Debug.Log($"2D Raycast hit collider (no SpriteRenderer): {hit2D.collider.name}");
                    // fall-through to 3D checks only if you want to; here we return because 2D collider was found
                    return;
                }
            }
            // no 2D hit -> fall through to 3D physics below
        }
        else
        {
            // CircleCast in 2D to emulate thickness
            RaycastHit2D hit2D = Physics2D.CircleCast(origin2, shotRadius, dir2.normalized, maxDistance, layerMask2D);
            if (hit2D.collider != null)
            {
                var sr = hit2D.collider.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Debug.Log($"2D CircleCast hit SpriteRenderer: {hit2D.collider.name} at {hit2D.point} (distance: {hit2D.distance})");
                    Debug.DrawLine(origin3, new Vector3(hit2D.point.x, hit2D.point.y, origin3.z), Color.green, 1.0f);

                    // TODO: apply damage/effects to the 2D target here
                    sr.GetComponent<EnemyHead>()?.Hit();
                    return;
                }
                else
                {
                    Debug.Log($"2D CircleCast hit collider (no SpriteRenderer): {hit2D.collider.name}");
                    return;
                }
            }
            // no 2D hit -> fall through to 3D physics below
        }
    }
}