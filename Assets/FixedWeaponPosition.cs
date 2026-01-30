using UnityEngine;

public class FixedWeaponPosition : MonoBehaviour {
    [SerializeField] private Camera cam;
    [SerializeField] private Vector3 viewportOffset = new Vector3(0.5f, 0.2f, 10f); 

    void LateUpdate() {
        if (cam == null) cam = Camera.main;
        transform.position = cam.ViewportToWorldPoint(viewportOffset);
        transform.LookAt(cam.transform); 
    }
}
