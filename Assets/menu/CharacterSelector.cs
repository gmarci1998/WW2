using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelector : MonoBehaviour
{
    [SerializeField] private GameObject characterInfoPanel;
    [SerializeField] private Image characterImage;
    [SerializeField] private TMP_Text characterDescription;

    [Serializable]
    public class CharacterData
    {
        public Sprite image;
        public string description;
    }

    public CharacterData[] characters;

    public void SelectCharacter(int index)
    {
        characterImage.sprite = characters[index].image;
        characterDescription.text = characters[index].description;
        characterInfoPanel.SetActive(true);
    }
}
