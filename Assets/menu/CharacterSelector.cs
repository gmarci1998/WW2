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
        public string description;
        public AudioClip narrationClip;
    }

    public CharacterData[] characters;
    private int currentCharacterIndex = -1;

    public void SelectCharacter(int index)
    {
        currentCharacterIndex = index;
        characterImage.sprite = characters[index].image;
        characterDescription.text = characters[index].description;
        characterInfoPanel.SetActive(true);
    }

    public void PlayNarration()
    {
        if (currentCharacterIndex >= 0 && characters[currentCharacterIndex].narrationClip != null)
        {
            narrationSource.clip = characters[currentCharacterIndex].narrationClip;
            narrationSource.Play();
        }
    }

    public void ClosePanel()
    {
        characterInfoPanel.SetActive(false);
        narrationSource.Stop();
    }
}
