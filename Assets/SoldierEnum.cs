using UnityEngine;
using System;

public enum HungarianSoldier { Soldier1, Soldier2, Soldier3 }
public enum RussianSoldier { Soldier1, Soldier2, Soldier3 }

[System.Serializable]
public class SoldierData  // ✅ Semmi öröklődés!
{
    public string Name;
    public Sprite Image;
    public int Age;
    public string Description;
    public AudioClip englishAudio;
    public AudioClip hungarianAudio;
    public bool picked = false;
    public bool isOpened;
    // Feliratfájlok az Assets/Subtitles mappából.
    // Két független tengely: melyik hanghoz van időzítve, és milyen nyelvű a szöveg.
    [Header("Feliratok - magyar narrációhoz időzítve")]
    public TextAsset huAudioHuSubtitles;   // magyar hang, magyar felirat
    public TextAsset huAudioEnSubtitles;   // magyar hang, angol felirat

    [Header("Feliratok - angol narrációhoz időzítve")]
    public TextAsset enAudioEnSubtitles;   // angol hang, angol felirat
    public TextAsset enAudioHuSubtitles;   // angol hang, magyar felirat

    // Tartalék: kézzel, Inspectorban felvitt sorok. Csak akkor használjuk,
    // ha a fenti TextAsset nincs beállítva.
    public SubtitleEntry[] englishEntries;
    public SubtitleEntry[] hungarianEntries;

}
