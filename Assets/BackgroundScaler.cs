using UnityEngine;

public class BackgroundScaler : MonoBehaviour {
    /*void Start() {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr.sprite == null) {
            Debug.LogError("No Sprite on " + gameObject.name);
            return;
        }

        float unitsPerPixel = 1f / sr.sprite.pixelsPerUnit;
        float spriteWorldHeight = sr.sprite.rect.height * unitsPerPixel;
        float spriteWorldWidth = sr.sprite.rect.width * unitsPerPixel;

        float camHeight = Camera.main.orthographicSize * 2f;
        float camWidth = camHeight * Screen.width / Screen.height;

        transform.localScale = new Vector3(camWidth / spriteWorldWidth, camHeight / spriteWorldHeight, 1f);
        Debug.Log($"Scale: {transform.localScale}, Sprite size: {spriteWorldWidth}x{spriteWorldHeight}");
    }*/
}
