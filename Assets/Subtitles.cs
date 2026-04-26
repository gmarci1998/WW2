using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Subtitles : MonoBehaviour
{
    public TextMeshProUGUI subtitleText;
    public SubtitleEntry[] subtitleEntries; // Array of subtitle entries with text and timing

    private int currentIndex;
    private float elapsedTime;


    void Start()
    {
        subtitleText.text = "";
        currentIndex = 0;
        elapsedTime = 0f;
    }

    public void SetSubtitles(SubtitleEntry[] entries)
    {
        bool subtitlesEnabled = PlayerPrefs.GetInt("SubtitlesEnabled", 1) == 1; // Ellenőrizzük, hogy a feliratok engedélyezve vannak-e

        if (!subtitlesEnabled)
        {
            subtitleText.text = ""; // Ha ki van kapcsolva, ne írjon ki semmit
            return;
        }

        subtitleEntries = entries;
        currentIndex = 0;
        elapsedTime = 0f;
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;

        if (currentIndex < subtitleEntries.Length && elapsedTime >= subtitleEntries[currentIndex].time)
        {
            ShowSubtitle(subtitleEntries[currentIndex].text);
            currentIndex++;
        }
    }

    void ShowSubtitle(string text)
    {
        subtitleText.text = text;
    }
}
