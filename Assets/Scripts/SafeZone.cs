using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class SafeZone : MonoBehaviour
{
    public Color gizmoColor = new Color(0, 1, 1, 0.25f);
    public Color gizmoWireColor = new Color(1, 1, 1, 0.8f);

    // draw the zone all the time, otherwise it's not too obvious where the
    // zones are
    void OnDrawGizmos()
    {
        BoxCollider collider = GetComponent<BoxCollider>();

        // we need to set the gizmo matrix for proper scale & rotation
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(collider.center, collider.size);
        Gizmos.color = gizmoWireColor;
        Gizmos.DrawWireCube(collider.center, collider.size);
        Gizmos.matrix = Matrix4x4.identity;
    }
}
