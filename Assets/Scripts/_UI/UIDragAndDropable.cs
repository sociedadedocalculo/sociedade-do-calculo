// Drag and Drop support for UI elements. Drag and Drop actions will be sent to
// the local player GameObject.
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIDragAndDropable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    // drag options
    public PointerEventData.InputButton button = PointerEventData.InputButton.Left;
    public GameObject drageePrefab;
    GameObject currentlyDragged;

    // status
    public bool dragable = true;
    public bool dropable = true;

    [HideInInspector] public bool draggedToSlot = false;

    public void OnBeginDrag(PointerEventData d)
    {
        // one mouse button is enough for dnd
        if (dragable && d.button == button)
        {
            // load current
            currentlyDragged = Instantiate(drageePrefab, transform.position, Quaternion.identity);
            currentlyDragged.GetComponent<Image>().sprite = GetComponent<Image>().sprite;
            currentlyDragged.transform.SetParent(transform.root, true); // canvas
            currentlyDragged.transform.SetAsLastSibling(); // move to foreground

            // disable button while dragging so onClick isn't fired if we drop a
            // slot on itself
            GetComponent<Button>().interactable = false;
        }
    }

    public void OnDrag(PointerEventData d)
    {
        // one mouse button is enough for drag and drop
        if (dragable && d.button == button)
            // move current
            currentlyDragged.transform.position = d.position;
    }

    // called after the slot's OnDrop
    public void OnEndDrag(PointerEventData d)
    {
        // delete current in any case
        Destroy(currentlyDragged);

        // one mouse button is enough for drag and drop
        if (dragable && d.button == button)
        {
            // try destroy if not dragged to a slot (flag will be set by slot)
            // message is sent to drag and drop handler for game specifics
            // (we also check d.pointerEnter because dropping a slot on itself
            //  is not detected as a drop. it shouldnt call clear either though)
            if (!draggedToSlot && d.pointerEnter != gameObject)
            {
                // send a drag and clear message like
                // OnDragAndClear_Skillbar({index})
                Player player = Utils.ClientLocalPlayer();
                player.SendMessage("OnDragAndClear_" + tag,
                                   name.ToInt(),
                                   SendMessageOptions.DontRequireReceiver);
            }

            // reset flag
            draggedToSlot = false;

            // enable button again
            GetComponent<Button>().interactable = true;
        }
    }

    // d.pointerDrag is the object that was dragged
    public void OnDrop(PointerEventData d)
    {
        // one mouse button is enough for drag and drop
        if (dropable && d.button == button)
        {
            // was the dropped GameObject a UIDragAndDropable?
            UIDragAndDropable dropDragable = d.pointerDrag.GetComponent<UIDragAndDropable>();
            if (dropDragable)
            {
                // let the dragable know that it was dropped onto a slot
                dropDragable.draggedToSlot = true;

                // only do something if we didn't drop it on itself. this way we
                // don't have to ignore raycasts etc.
                // message is sent to drag and drop handler for game specifics
                if (dropDragable != this)
                {
                    // send a drag and drop message like
                    // OnDragAndDrop_Skillbar_Inventory({from, to})
                    Player player = Utils.ClientLocalPlayer();
                    int from = dropDragable.name.ToInt();
                    int to = name.ToInt();
                    player.SendMessage("OnDragAndDrop_" + dropDragable.tag + "_" + tag,
                                       new int[] { from, to },
                                       SendMessageOptions.DontRequireReceiver);
                }
            }
        }
    }

    void OnDisable()
    {
        Destroy(currentlyDragged);
    }

    void OnDestroy()
    {
        Destroy(currentlyDragged);
    }
}
