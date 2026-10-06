using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// A Assets/Subtitles mappában lévő feliratfájlokat olvassa be.
/// Várt formátum (a Whisper kimenetével megegyező):
///   [ { "timestamp": [0, 3.72], "text": "..." }, ... ]
/// </summary>
public static class SubtitleLoader
{
    // Ha az utolsó sornak nincs érvényes vége, ennyi ideig marad kint.
    private const float FallbackLastDuration = 4f;

    [Serializable]
    private class Cue
    {
        public float[] timestamp;
        public string text;
    }

    [Serializable]
    private class CueList
    {
        public Cue[] items;
    }

    public static SubtitleEntry[] Load(TextAsset asset)
    {
        if (asset == null)
        {
            return null; // a hívó eldönti, mi legyen a tartalék
        }

        return Parse(asset.text, asset.name);
    }

    public static SubtitleEntry[] Parse(string json, string sourceName = "subtitles")
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("SubtitleLoader: üres feliratfájl (" + sourceName + ").");
            return new SubtitleEntry[0];
        }

        // A JsonUtility nem tud gyökérszintű tömböt olvasni, ezért becsomagoljuk,
        // és nem bírja a szám-tömbben lévő null-t sem, ezért azt -1-re cseréljük.
        string sanitized = Regex.Replace(json.Trim(), @"(,\s*)null(\s*\])", "$1-1$2");

        CueList list;
        try
        {
            list = JsonUtility.FromJson<CueList>("{" + @"""items"":" + sanitized + "}");
        }
        catch (Exception e)
        {
            Debug.LogError("SubtitleLoader: nem sikerült beolvasni (" + sourceName + "): " + e.Message);
            return new SubtitleEntry[0];
        }

        if (list == null || list.items == null)
        {
            Debug.LogError("SubtitleLoader: érvénytelen feliratfájl (" + sourceName + ").");
            return new SubtitleEntry[0];
        }

        List<SubtitleEntry> entries = new List<SubtitleEntry>(list.items.Length);
        foreach (Cue cue in list.items)
        {
            if (cue == null || string.IsNullOrEmpty(cue.text))
            {
                continue;
            }

            SubtitleEntry entry = new SubtitleEntry();
            entry.text = cue.text.Trim();
            entry.time = (cue.timestamp != null && cue.timestamp.Length > 0) ? cue.timestamp[0] : 0f;
            entry.endTime = (cue.timestamp != null && cue.timestamp.Length > 1) ? cue.timestamp[1] : 0f;
            entries.Add(entry);
        }

        SubtitleEntry[] result = entries.ToArray();
        Array.Sort(result, (a, b) => a.time.CompareTo(b.time));
        FillMissingEnds(result);
        return result;
    }

    /// <summary>
    /// Kitölti a hiányzó vagy hibás végidőpontokat: a sor a következő kezdetéig marad kint.
    /// Így az Inspectorban kézzel felvitt, végidő nélküli bejegyzések is működnek.
    /// </summary>
    public static void FillMissingEnds(SubtitleEntry[] entries)
    {
        if (entries == null)
        {
            return;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].endTime > entries[i].time)
            {
                continue;
            }

            entries[i].endTime = (i + 1 < entries.Length)
                ? entries[i + 1].time
                : entries[i].time + FallbackLastDuration;
        }
    }
}
