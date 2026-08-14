using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SpawnPointCoverInfo : MonoBehaviour
{
    [Tooltip("Ennyit emelkedjen ki innen az ellenfél a fedezék fölé, a spawn pont Y pozíciójához képest (világegységben).")]
    public float riseAmount = 1.65f;

    [Tooltip("A gizmo vízszintes vonalának hossza a Scene nézetben - csak vizuális segédlet, nincs hatással a játékmenetre.")]
    public float gizmoWidth = 2f;

    void OnDrawGizmos()
    {
        Vector3 hiddenPos = transform.position;
        Vector3 peekPos = hiddenPos + Vector3.up * riseAmount;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(hiddenPos, 0.08f);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(peekPos, 0.08f);
        Gizmos.DrawLine(hiddenPos, peekPos);
        Gizmos.DrawLine(peekPos - Vector3.right * (gizmoWidth / 2f), peekPos + Vector3.right * (gizmoWidth / 2f));

#if UNITY_EDITOR
        Handles.color = Color.red;
        Handles.Label(peekPos + Vector3.up * 0.15f, $"kibukkanás teteje (+{riseAmount:0.00})");
#endif
    }
}
