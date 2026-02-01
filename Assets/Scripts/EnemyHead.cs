using System.Collections;
using UnityEngine;

public class EnemyHead : MonoBehaviour
{
    [SerializeField] Vector3 hidingPos;
    [SerializeField] Vector3 peakingPos;
    [SerializeField] Vector3 firingPos;

    [Header("Motion")]
    [SerializeField] float moveDuration = 0.35f;
    [SerializeField] AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] float fireDuration = 2f;
    [SerializeField] float peakDuration = 2f;
    [SerializeField] float hitDuration = 1f;
    [SerializeField] GameObject spark;
    [SerializeField] float sparkDuration = 0.2f;

    private float fireTimer = 0f;
    private float peakTimer = 0f;

    public enum States
    {
        Hiding,
        Peaking,
        Firing,
        Dead
    }

    // internal state
    [SerializeField] States state = States.Hiding;

    // internal motion tracking
    Vector3 startPos;
    Vector3 targetPos;
    float moveTimer = 0f;
    bool isMoving = false;

    void Start()
    {
        // ensure starting position is hidingPos
        transform.localPosition = hidingPos;
    }

    void Update()
    {
        // Nothing required here for the pupeteer-driven behavior.
    }

    // Centralized state setter that logs transitions including the GameObject name.
    void SetState(States newState)
    {
        if (state == newState) return;
        Debug.Log($"EnemyHead[{gameObject.name}]: state {state} -> {newState}");
        state = newState;
    }

    // Start an action and interpolate the head between hiding and the requested state.
    // Valid transitions: Hiding <-> Peaking, Hiding <-> Firing.
    public void DoAction(States newState)
    {
        if (state == States.Dead)
        {
            Debug.Log($"EnemyHead[{gameObject.name}]: DoAction({newState}) ignored because state is Dead.");
            return;
        }

        // If already in requested state and not moving, ignore
        if (state == newState && !isMoving)
        {
            Debug.Log($"EnemyHead[{gameObject.name}]: DoAction({newState}) ignored (already in state).");
            return;
        }

        Debug.Log($"EnemyHead[{gameObject.name}]: DoAction requested {newState} (current {state})");

        StopAllCoroutines();

        if (newState == States.Peaking)
        {
            StartCoroutine(MoveRoutine(peakingPos, newState));
        }
        else if (newState == States.Firing)
        {
            // ensure we move to firing pos then run firing behaviour
            StartCoroutine(FireRoutine());
        }
        else if (newState == States.Hiding)
        {
            StartCoroutine(MoveRoutine(hidingPos, newState));
        }
    }

    IEnumerator MoveRoutine(Vector3 destination, States resultingState)
    {
        isMoving = true;
        startPos = transform.localPosition;
        targetPos = destination;
        moveTimer = 0f;

        while (moveTimer < moveDuration)
        {
            moveTimer += Time.deltaTime;
            float t = Mathf.Clamp01(moveTimer / moveDuration);
            t = ease.Evaluate(t);
            transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        transform.localPosition = targetPos;
        SetState(resultingState);
        isMoving = false;

        // if peaking, stay for peakDuration then return to hiding (unless another command intervenes)
        if (resultingState == States.Peaking)
        {
            yield return new WaitForSeconds(peakDuration);
            if (state == States.Peaking) // still in peaking
                DoAction(States.Hiding);
        }
    }

    IEnumerator FireRoutine()
    {
        // move to firing position
        yield return StartCoroutine(MoveRoutine(firingPos, States.Firing));

        // show spark if configured
        if (spark != null)
        {
            spark.SetActive(true);
        }

        yield return new WaitForSeconds(sparkDuration);

        if (spark != null)
            spark.SetActive(false);

        float elapsed = 0f;
        while (elapsed < fireDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        FindFirstObjectByType<PlayerHead>().Hit();

        // after firing, return to hiding
        if (state != States.Dead)
            DoAction(States.Hiding);
    }

    public void Hit()
    {
        // Simple reaction to being hit: mark dead and stop activity
        StopAllCoroutines();
        SetState(States.Dead);
        if (spark != null)
            spark.SetActive(false);
        // Optionally animate hit/hide; here we immediately hide (could be replaced with an animation)
        transform.localPosition = hidingPos;
        gameObject.SetActive(false);

        Debug.Log($"EnemyHead[{gameObject.name}]: Hit() processed, set to Dead and deactivated.");
    }

    // Public API used by EnemyPupeteer to respawn/reset without reflection.
    // Stops current coroutines, ensures GameObject is active, resets position/state and
    // optionally starts an initial action depending on rushMode.
    public void ResetForRespawn(bool rushMode)
    {
        StopAllCoroutines();
        SetState(States.Hiding);
        transform.localPosition = hidingPos;
        gameObject.SetActive(true);

        Debug.Log($"EnemyHead[{gameObject.name}]: ResetForRespawn(rushMode={rushMode}) called.");

        // Immediately place the enemy into an appropriate action depending on the caller's intent.
        if (rushMode)
            DoAction(States.Firing);
        else
            DoAction(States.Peaking);
    }

    // Expose current state if needed
    public States CurrentState => state;
}
