// Instantiates a tooltip while the cursor is over this UI element.
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIShowToolTip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject tooltipPrefab;
    [TextArea(1, 30)] public string text = "";

    // instantiated tooltip
    GameObject current;

    void CreateToolTip()
    {
        // instantiate
        current = Instantiate(tooltipPrefab, transform.position, Quaternion.identity);

        // put to foreground
        current.transform.SetParent(transform.root, true); // canvas
        current.transform.SetAsLastSibling(); // last one means foreground
    }

    void ShowToolTip(float delay)
    {
        Invoke("CreateToolTip", delay);
    }

    void DestroyToolTip()
    {
        // stop any running attempts to show it
        CancelInvoke("CreateToolTip");

        // destroy it
        Destroy(current);
    }

    public void OnPointerEnter(PointerEventData d)
    {
        ShowToolTip(0.5f);
    }

    public void OnPointerExit(PointerEventData d)
    {
        DestroyToolTip();
    }

    void Update()
    {
        // always copy text to tooltip. it might change dynamically when
        // swapping items etc., so setting it once is not enough.
        if (current) current.GetComponentInChildren<Text>().text = text;
    }

    void OnDisable()
    {
        DestroyToolTip();
    }

    void OnDestroy()
    {
        DestroyToolTip();
    }
}
