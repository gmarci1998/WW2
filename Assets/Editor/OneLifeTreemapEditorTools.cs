#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Creates (or fills in missing) treemapRectSlots on a OneLifeSequence component so their
// size/position can be hand-tuned in the editor with the Rect Tool instead of being computed
// from fractions in code. Re-running is safe: existing slots are left untouched, only missing
// ones are (re)created using OneLifeSequence.TreemapRects as the starting layout.
public static class OneLifeTreemapEditorTools
{
    [MenuItem("CONTEXT/OneLifeSequence/Generate Treemap Rect Slots")]
    private static void GenerateTreemapRectSlots(MenuCommand command)
    {
        var sequence = (OneLifeSequence)command.context;
        var so = new SerializedObject(sequence);
        SerializedProperty bigSquareProp = so.FindProperty("bigSquare");
        SerializedProperty slotsProp = so.FindProperty("treemapRectSlots");

        var bigSquare = bigSquareProp.objectReferenceValue as RectTransform;
        if (bigSquare == null)
        {
            Debug.LogError("OneLifeTreemapEditorTools: assign Big Square before generating treemap rect slots.", sequence);
            return;
        }

        Rect[] defaults = OneLifeSequence.TreemapRects;
        slotsProp.arraySize = defaults.Length;

        for (int i = 0; i < defaults.Length; i++)
        {
            SerializedProperty slot = slotsProp.GetArrayElementAtIndex(i);
            var rt = slot.objectReferenceValue as RectTransform;

            if (rt == null)
            {
                var go = new GameObject("TreemapRect" + (i + 1), typeof(RectTransform), typeof(Image));
                Undo.RegisterCreatedObjectUndo(go, "Create Treemap Rect Slot");
                rt = (RectTransform)go.transform;
                rt.SetParent(bigSquare, false);

                Image img = go.GetComponent<Image>();
                img.raycastTarget = false;
                img.color = new Color(0.85f, 0.85f, 0.85f, 0.30f);

                rt.anchorMin = new Vector2(defaults[i].xMin, defaults[i].yMin);
                rt.anchorMax = new Vector2(defaults[i].xMax, defaults[i].yMax);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                go.SetActive(false);
                slot.objectReferenceValue = rt;
            }
        }

        so.ApplyModifiedProperties();
        Debug.Log("OneLifeTreemapEditorTools: treemap rect slots ready - resize/reposition them in the Scene view, they're regular RectTransforms under Big Square.");
    }

    // Freehand-dragging a rect's edges in the Scene view writes fixed pixel margins
    // (offsetMin/offsetMax) on top of its anchors. Those margins don't scale when Big
    // Square's own size changes at runtime (GrowBigSquare), so a layout that looks right in
    // the editor visibly jumps once the square grows. This bakes each slot's CURRENT visual
    // position/size into pure anchor fractions (relative to Big Square's rect right now) and
    // zeroes the pixel margins, so the layout stays put at any Big Square size afterwards.
    // Run it right after hand-placing the rects, before resizing Big Square.
    [MenuItem("CONTEXT/OneLifeSequence/Normalize Treemap Rect Anchors To Current Layout")]
    private static void NormalizeTreemapRectAnchors(MenuCommand command)
    {
        var sequence = (OneLifeSequence)command.context;
        var so = new SerializedObject(sequence);
        SerializedProperty slotsProp = so.FindProperty("treemapRectSlots");

        int normalized = 0;

        for (int i = 0; i < slotsProp.arraySize; i++)
        {
            var rt = slotsProp.GetArrayElementAtIndex(i).objectReferenceValue as RectTransform;
            if (rt == null)
            {
                continue;
            }

            var parentRT = rt.parent as RectTransform;
            if (parentRT == null)
            {
                continue;
            }

            Vector2 parentSize = parentRT.rect.size;
            if (parentSize.x <= 0f || parentSize.y <= 0f)
            {
                Debug.LogWarning("OneLifeTreemapEditorTools: Big Square has zero size right now, skipping " + rt.name + ".", rt);
                continue;
            }

            Undo.RecordObject(rt, "Normalize Treemap Rect Anchors");

            Vector2 newAnchorMin = rt.anchorMin + new Vector2(rt.offsetMin.x / parentSize.x, rt.offsetMin.y / parentSize.y);
            Vector2 newAnchorMax = rt.anchorMax + new Vector2(rt.offsetMax.x / parentSize.x, rt.offsetMax.y / parentSize.y);

            rt.anchorMin = newAnchorMin;
            rt.anchorMax = newAnchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            EditorUtility.SetDirty(rt);
            normalized++;
        }

        Debug.Log("OneLifeTreemapEditorTools: normalized " + normalized + " treemap rect(s) to pure anchors - they should now hold their layout at any Big Square size.");
    }
}
#endif
