using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshPro))]
public class TextMeshFadeAlpha : MonoBehaviour
{
    public TextMeshPro textMesh;
    public float delay = 0;
    public float duration = 1;
    float perSecond;
    float startTime;

    void Start()
    {
        // calculate by how much to fade per second
        perSecond = textMesh.color.a / duration;

        // calculate start time
        startTime = Time.time + delay;
    }

    void Update()
    {
        if (Time.time >= startTime)
        {
            // fade all text meshes (in children too in case of shadows etc.)
            Color color = textMesh.color;
            color.a -= perSecond * Time.deltaTime;
            textMesh.color = color;
        }
    }
}
