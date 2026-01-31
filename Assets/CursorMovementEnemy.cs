using UnityEngine;

public class CursorMovementEnemy : MonoBehaviour
{

    [SerializeField] private float enemyParallaxX = 4f;  
    [SerializeField] private float enemyParallaxY = 0.2f;  

    private float normalizedX;
    private float normalizedY;
    void Update()
    {
        // A háttér mozgásához igazított mozgás
        /*transform.position = new Vector3(
            normalizedX, // Az X tengelyen történő mozgás a GameManager által számított érték alapján
            transform.position.y, // Az Y tengely fix értéken marad
            transform.position.z
        );*/
    }

    public void SetNormalized(float x, float y)
    {
        // A GameManager által számított értékeket használjuk
        normalizedX = x * enemyParallaxX; // Parallax tényező alkalmazása az X tengelyen
        normalizedY = y * enemyParallaxY; // Parallax tényező alkalmazása az Y tengelyen
    }
}
