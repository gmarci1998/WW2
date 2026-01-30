using UnityEngine;

public class ParallaxBackground : MonoBehaviour {
    [SerializeField] private float parallaxFactorX = 8f;  
    [SerializeField] private float parallaxFactorY = 0.4f; 
    [SerializeField] private float maxOffsetY = 2f;      
    private Vector3 startPos;
    public GameObject backgroundSprite;  
    public GameObject middlegroundSprite;  

    void Start() {
        startPos = transform.position;
        Cursor.lockState = CursorLockMode.Confined;  
    }

    void Update() {
        float normalizedX = (Input.mousePosition.x / Screen.width - 0.5f) * 2f * parallaxFactorX;
        float normalizedY = (Input.mousePosition.y / Screen.height - 0.5f) * 2f * parallaxFactorY;

        Vector3 targetPos = startPos + new Vector3(normalizedX, normalizedY, 0);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 10f);

        if (backgroundSprite != null) {
            backgroundSprite.transform.position = new Vector3(
                normalizedX,  
                normalizedY - maxOffsetY, 
                backgroundSprite.transform.position.z
            );
        }

        if (middlegroundSprite != null) {
            middlegroundSprite.transform.position = new Vector3(
                normalizedX / 4f, 
                -4.2f, 
                middlegroundSprite.transform.position.z
            );
        }

        Debug.Log($"Norm X: {normalizedX:F2}, Norm Y: {normalizedY:F2}");
    }
}
