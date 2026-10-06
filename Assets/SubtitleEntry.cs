using System;

[Serializable]
public struct SubtitleEntry
{
    public string text;

    // A narrációs hangsáv elejétől számított kezdés másodpercben.
    public float time;

    // Mikor tűnjön el a sor. Ha kisebb vagy egyenlő, mint a time, akkor
    // a sor a következő bejegyzés kezdetéig marad kint (régi viselkedés).
    public float endTime;
}
