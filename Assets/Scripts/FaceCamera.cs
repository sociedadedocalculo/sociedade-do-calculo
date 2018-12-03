// Useful for Text Meshes that should face the camera.
//
// In some cases there seems to be a Unity bug where the text meshes end up in
// weird positions if it's not positioned at (0,0,0). In that case simply put it
// into an empty GameObject and use that empty GameObject for positioning.
using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    // LateUpdate so that all camera updates are finished.
    void LateUpdate()
    {
        transform.forward = Camera.main.transform.forward;
    }

    // copying transform.forward is relatively expensive and slows things down
    // for large amounts of entities, so we only want to do it while the mesh
    // is actually visible
    void Awake() { enabled = false; } // disabled by default until visible
    void OnBecameVisible() { enabled = true; }
    void OnBecameInvisible() { enabled = false; }
}
