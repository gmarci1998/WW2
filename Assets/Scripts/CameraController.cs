using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] GameObject layers;
    [SerializeField] float sensitivity = 1f;

    // maximum allowed offset from the initial position (half-extent)
    [SerializeField] Vector2 bounds;

    // how many screen pixels one unit of Input.GetAxis("Mouse X"/"Mouse Y") represents
    [SerializeField] float axisPixelScale = 200f;

    // if true, confine the OS cursor to the game window while the app has focus
    [SerializeField] bool confineCursorToWindow = true;

    // stores the last virtual mouse position and the physical mouse position
    Vector3 _virtualMousePosition;
    Vector3 _lastVirtualMousePosition;
    Vector3 _physicalMousePosition;

    // keep the initial position of the layers so bounds are relative to that
    Vector3 _startPosition;

    Camera cam;

    void Start()
    {
        // apply cursor confinement if requested
        if (confineCursorToWindow)
            Cursor.lockState = CursorLockMode.Confined;

        _physicalMousePosition = Input.mousePosition;
        _virtualMousePosition = _physicalMousePosition;
        _lastVirtualMousePosition = _virtualMousePosition;

        if (layers != null)
            _startPosition = layers.transform.position;

        cam = Camera.main;
    }

    void Update()
    {
        if (layers == null)
            return;

        // update virtual mouse position:
        // - include actual movement of the physical cursor (will be zero at window edges)
        // - include relative axis input so movement continues when cursor is at screen border
        Vector3 physicalDelta = (Vector3)Input.mousePosition - _physicalMousePosition;
        _physicalMousePosition = Input.mousePosition;

        float axisX = Input.GetAxis("Mouse X");
        float axisY = Input.GetAxis("Mouse Y");

        // axis input scaled to pixels; Time.deltaTime makes it frame-rate stable
        Vector3 axisDeltaPixels = new Vector3(axisX, axisY, 0f) * axisPixelScale * Time.deltaTime;

        _virtualMousePosition += physicalDelta + axisDeltaPixels;

        if (cam == null)
        {
            // fallback to pixel-based translation using virtual mouse positions
            Vector3 pixelDelta = _virtualMousePosition - _lastVirtualMousePosition;
            Vector3 proposed = layers.transform.position + (-pixelDelta * sensitivity) * Time.deltaTime;
            layers.transform.position = ClampToBounds(proposed);

            _lastVirtualMousePosition = _virtualMousePosition;
            return;
        }

        // compute a world-space delta using the distance from camera to the layers object
        float distance = Mathf.Abs(cam.transform.position.z - layers.transform.position.z);
        Vector3 currentWorld = cam.ScreenToWorldPoint(new Vector3(_virtualMousePosition.x, _virtualMousePosition.y, distance));
        Vector3 lastWorld = cam.ScreenToWorldPoint(new Vector3(_lastVirtualMousePosition.x, _lastVirtualMousePosition.y, distance));
        Vector3 worldDelta = currentWorld - lastWorld;

        // compute proposed new position (move in opposite direction of the mouse world delta)
        Vector3 proposedPosition = layers.transform.position + -worldDelta * sensitivity;

        // apply but clamp so movement beyond bounds is not allowed
        layers.transform.position = ClampToBounds(proposedPosition);

        _lastVirtualMousePosition = _virtualMousePosition;
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!confineCursorToWindow)
            return;

        // Confine cursor while app has focus, release when it loses focus.
        Cursor.lockState = hasFocus ? CursorLockMode.Confined : CursorLockMode.None;
    }

    void OnDisable()
    {
        // Ensure cursor lock state is restored if this component is disabled
        if (confineCursorToWindow)
            Cursor.lockState = CursorLockMode.None;
    }

    void OnApplicationQuit()
    {
        // Restore cursor behavior on exit
        if (confineCursorToWindow)
            Cursor.lockState = CursorLockMode.None;
    }

    // Clamps a position to remain within _startPosition +/- bounds on X and Y. Z is preserved.
    Vector3 ClampToBounds(Vector3 proposed)
    {
        Vector3 clamped = proposed;
        clamped.x = Mathf.Clamp(proposed.x, _startPosition.x - bounds.x, _startPosition.x + bounds.x);
        clamped.y = Mathf.Clamp(proposed.y, _startPosition.y - bounds.y, _startPosition.y + bounds.y);
        // keep Z unchanged (layers depth)
        clamped.z = proposed.z;
        return clamped;
    }
}
