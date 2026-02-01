using UnityEngine;

public enum HungarianSoldier { Soldier1, Soldier2, Soldier3 }
public enum RussianSoldier { Soldier1, Soldier2, Soldier3 }

[System.Serializable]
public class SoldierData  // ✅ Semmi öröklődés!
{
    public string Name;
    public Sprite Image;
    public int Age;
    public string Description;
    public AudioClip Audio;
    public bool picked = false;
}
