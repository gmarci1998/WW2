using UnityEngine;
using UnityEngine.SceneManagement;

public class OnClickHandler : MonoBehaviour
{
    public void Menu()
    {
        SceneManager.LoadScene("Menu");
    }
}