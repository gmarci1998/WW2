using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyPupeteer : MonoBehaviour
{
    [SerializeField] int activeEnemyCount = 3;
    [SerializeField] float respawnMinDuration = 2f;
    [SerializeField] float respawnMaxDuration = 8f;

    // short delays / timings used by the state machine
    [SerializeField] float initialIdleDuration = 2f;
    [SerializeField] float idlingDuration = 2f;
    [SerializeField] float attackLoopInterval = 0.6f;
    [SerializeField] float rushTriggerDelay = 1.0f;

    EnemyHead[] Enemies => FindObjectsByType<EnemyHead>(FindObjectsSortMode.None);

    enum States
    {
        Paused,
        Idling,
        Attacking,
        Rushing
    }

    States state = States.Paused;

    // track scheduled respawn coroutines so we don't schedule duplicates
    readonly Dictionary<EnemyHead, Coroutine> respawnCoroutines = new();

    Coroutine stateMachineRoutine;

    void Start()
    {
        stateMachineRoutine = StartCoroutine(StateMachine());
    }

    void OnDisable()
    {
        if (stateMachineRoutine != null)
            StopCoroutine(stateMachineRoutine);
        foreach (var kv in respawnCoroutines.Values.ToArray())
            if (kv != null) StopCoroutine(kv);
        respawnCoroutines.Clear();
    }

    // Centralized state setter so transitions are logged in one place.
    void SetState(States newState)
    {
        if (state == newState) return;
        Debug.Log($"EnemyPupeteer: state change {state} -> {newState} (alive: {Enemies.Count(e => e != null && e.gameObject.activeSelf)}, total: {Enemies.Length})");
        state = newState;
    }

    IEnumerator StateMachine()
    {
        // Start paused: ensure everyone hiding
        SetState(States.Paused);
        SetAll(EnemyHead.States.Hiding);

        // transition to idling after a short pause
        yield return new WaitForSeconds(initialIdleDuration);

        SetState(States.Idling);

        while (true)
        {
            if (state == States.Idling)
            {
                // Choose up to activeEnemyCount active (not-dead) enemies to peak
                PeakRandomActiveEnemies(activeEnemyCount);
                yield return new WaitForSeconds(idlingDuration);

                // After idling, go attack
                SetState(States.Attacking);
            }
            else if (state == States.Attacking)
            {
                // Main attacking loop: ensure up to N enemies are firing/peaking,
                // schedule respawns for dead enemies, and detect rush condition.
                while (state == States.Attacking)
                {
                    var all = Enemies.Where(e => e != null).ToArray();
                    var alive = all.Where(e => e.gameObject.activeSelf).ToArray();
                    var dead = all.Except(alive).ToArray();

                    // schedule respawns for dead enemies
                    foreach (var d in dead)
                    {
                        if (!respawnCoroutines.ContainsKey(d))
                        {
                            float delay = Random.Range(respawnMinDuration, respawnMaxDuration);
                            respawnCoroutines[d] = StartCoroutine(RespawnAfter(d, delay, rushMode: false));
                        }
                    }

                    // ensure up to activeEnemyCount of the alive enemies are Firing
                    ActivateFiringOnRandom(alive, activeEnemyCount);

                    // if no alive enemies, trigger rush after short delay
                    if (alive.Length == 0)
                    {
                        yield return new WaitForSeconds(rushTriggerDelay);
                        // re-evaluate: if still none alive, enter rushing
                        if (Enemies.Where(e => e != null && e.gameObject.activeSelf).Count() == 0)
                        {
                            SetState(States.Rushing);
                            break;
                        }
                    }

                    yield return new WaitForSeconds(attackLoopInterval);
                }
            }
            else if (state == States.Rushing)
            {
                // Rush: respawn dead enemies twice as fast and force all to firing/peaking.
                var all = Enemies.Where(e => e != null).ToArray();
                var alive = all.Where(e => e.gameObject.activeSelf).ToArray();
                var dead = all.Except(alive).ToArray();

                // schedule respawns for dead enemies at twice the speed
                foreach (var d in dead)
                {
                    if (!respawnCoroutines.ContainsKey(d))
                    {
                        float delay = Random.Range(respawnMinDuration, respawnMaxDuration) * 0.5f;
                        respawnCoroutines[d] = StartCoroutine(RespawnAfter(d, delay, rushMode: true));
                    }
                }

                // force any currently alive enemies to firing (or peaking if transition prevents firing)
                foreach (var e in all.Where(x => x != null && x.gameObject.activeSelf))
                {
                    e.DoAction(EnemyHead.States.Firing);
                }

                // Stay in rushing until at least one enemy becomes alive; then return to Attacking
                float checkTimeout = 0f;
                const float maxRushDuration = 10f; // safety cap to avoid infinite rush state
                while (state == States.Rushing)
                {
                    var nowAlive = Enemies.Where(e => e != null && e.gameObject.activeSelf).ToArray();
                    if (nowAlive.Length > 0)
                    {
                        // back to attacking
                        SetState(States.Attacking);
                        break;
                    }

                    yield return new WaitForSeconds(0.25f);
                    checkTimeout += 0.25f;
                    if (checkTimeout >= maxRushDuration)
                    {
                        // safety fallback: return to attacking and let regular respawns continue
                        SetState(States.Attacking);
                        break;
                    }
                }
            }
            else
            {
                yield return null;
            }
        }
    }

    void SetAll(EnemyHead.States s)
    {
        foreach (var e in Enemies.Where(x => x != null && x.gameObject.activeSelf))
            e.DoAction(s);
    }

    void PeakRandomActiveEnemies(int count)
    {
        var alive = Enemies.Where(e => e != null && e.gameObject.activeSelf).ToArray();
        var chosen = PickRandomSubset(alive, count);
        foreach (var e in chosen)
            e.DoAction(EnemyHead.States.Peaking);
    }

    void ActivateFiringOnRandom(EnemyHead[] alive, int count)
    {
        var chosen = PickRandomSubset(alive, count);
        // chosen ones fire, the rest can peak
        foreach (var e in alive)
        {
            if (chosen.Contains(e))
                e.DoAction(EnemyHead.States.Firing);
            else
                e.DoAction(EnemyHead.States.Peaking);
        }
    }

    EnemyHead[] PickRandomSubset(EnemyHead[] source, int count)
    {
        if (source == null || source.Length == 0 || count <= 0)
            return new EnemyHead[0];

        var list = source.ToList();
        // Fisher-Yates shuffle partial
        for (int i = 0; i < list.Count; i++)
        {
            int j = Random.Range(i, list.Count);
            var tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
        return list.Take(Mathf.Min(count, list.Count)).ToArray();
    }

    IEnumerator RespawnAfter(EnemyHead enemy, float delay, bool rushMode)
    {
        if (enemy == null)
        {
            respawnCoroutines.Remove(enemy);
            yield break;
        }

        yield return new WaitForSeconds(delay);

        // Remove the tracking entry early so re-scheduling can occur if needed
        respawnCoroutines.Remove(enemy);

        if (enemy == null)
            yield break;

        // Reactivate and reset via public API on EnemyHead (no reflection).
        try
        {
            enemy.ResetForRespawn(rushMode);
        }
        catch
        {
            // ignore non-fatal errors
        }
    }
}
