using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelector : MonoBehaviour
{
    [SerializeField] private GameObject characterInfoPanel;
    [SerializeField] private Image characterImage;
    [SerializeField] private TMP_Text characterDescription;
    [SerializeField] private AudioSource narrationSource;

    [Serializable]
    public class CharacterData
    {
        public Sprite image;
        public string englishDescription;
        public string hungarianDescription;
        public AudioClip englishNarrationClip;
        public AudioClip hungarianNarrationClip;
    }

    public CharacterData[] characters;
    private int currentCharacterIndex = -1;

    private static string GetLocalizedDescription(CharacterData data)
    {
        if (LocalizationManager.CurrentLanguage == "Hungarian" && !string.IsNullOrEmpty(data.hungarianDescription))
        {
            return data.hungarianDescription;
        }

        return data.englishDescription;
    }

    private static AudioClip GetLocalizedNarrationClip(CharacterData data)
    {
        if (LocalizationManager.CurrentLanguage == "Hungarian" && data.hungarianNarrationClip != null)
        {
            return data.hungarianNarrationClip;
        }

        return data.englishNarrationClip;
    }

    public void SelectCharacter(int index)
    {
        currentCharacterIndex = index;
        characterImage.sprite = characters[index].image;
        characterDescription.text = GetLocalizedDescription(characters[index]);
        characterInfoPanel.SetActive(true);
    }

    public void PlayNarration()
    {
        if (currentCharacterIndex < 0)
        {
            return;
        }

        AudioClip clip = GetLocalizedNarrationClip(characters[currentCharacterIndex]);
        if (clip != null)
        {
            narrationSource.clip = clip;
            narrationSource.Play();
        }
    }

    public void ClosePanel()
    {
        characterInfoPanel.SetActive(false);
        narrationSource.Stop();
    }
}
