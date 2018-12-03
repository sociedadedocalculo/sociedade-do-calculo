using UnityEngine;

public class UnityVersionCheck : MonoBehaviour
{
    public string recommended;
    public string download = "https://unity3d.com/unity/qa/lts-releases";

    void Awake()
    {
        if (Application.unityVersion != recommended)
            Debug.LogWarning("uMMORPG works best with Unity " + recommended + " LTS! Download: " + download + "\n");
    }
}
