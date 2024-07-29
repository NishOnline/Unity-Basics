using UnityEngine;
using System.Collections.Generic;

public class PickupAndThrow : MonoBehaviour
{
    public float pickupRange = 3f;
    public float throwForce = 10f;
    public LayerMask pickupLayer;
    public Transform itemHolder;
    public Transform crosshair;
    public Vector3 crosshairDefaultScale;
    public Vector3 crosshairAimScale;
    public Vector3 hiddenPosition = new Vector3(0, -100, 0); // Position to hide items

    private List<GameObject> pickedUpItems = new List<GameObject>();
    private int currentItemIndex = 0;

    void Update()
    {
        if (pickedUpItems.Count < 3)
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, pickupRange, pickupLayer))
            {
                // Change crosshair size when aiming at a pickable item
                crosshair.localScale = crosshairAimScale;

                if (Input.GetKeyDown(KeyCode.E))
                {
                    PickUpItem(hit.collider.gameObject);
                }
            }
            else
            {
                // Reset crosshair size when not aiming at a pickable item
                crosshair.localScale = crosshairDefaultScale;
            }
        }
        else
        {
            // Reset crosshair size when holding items
            crosshair.localScale = crosshairDefaultScale;
        }

        if (pickedUpItems.Count > 0)
        {
            // Switch between items using mouse scroll
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                currentItemIndex = (currentItemIndex + (scroll > 0 ? 1 : -1) + pickedUpItems.Count) % pickedUpItems.Count;
                UpdateItemPositions();
            }

            // Keep the currently selected item in front of the player
            pickedUpItems[currentItemIndex].transform.position = itemHolder.position;
            pickedUpItems[currentItemIndex].transform.rotation = itemHolder.rotation;

            if (Input.GetKeyDown(KeyCode.Q))
            {
                ThrowItem(currentItemIndex);
            }
        }
    }

    void PickUpItem(GameObject item)
    {
        pickedUpItems.Add(item);
        item.GetComponent<Rigidbody>().isKinematic = true;
        item.transform.SetParent(itemHolder);
        currentItemIndex = pickedUpItems.Count - 1;
        UpdateItemPositions();
    }

    void ThrowItem(int index)
    {
        GameObject item = pickedUpItems[index];
        item.GetComponent<Rigidbody>().isKinematic = false;
        item.GetComponent<Rigidbody>().AddForce(Camera.main.transform.forward * throwForce, ForceMode.VelocityChange);
        item.transform.SetParent(null);
        pickedUpItems.RemoveAt(index);

        // Adjust currentItemIndex if necessary
        if (currentItemIndex >= pickedUpItems.Count)
        {
            currentItemIndex = pickedUpItems.Count - 1;
        }

        UpdateItemPositions();
    }

    void UpdateItemPositions()
    {
        for (int i = 0; i < pickedUpItems.Count; i++)
        {
            if (i == currentItemIndex)
            {
                // Show the currently selected item
                pickedUpItems[i].GetComponent<Renderer>().enabled = true;
            }
            else
            {
                // Hide non-selected items
                pickedUpItems[i].transform.position = hiddenPosition;
                pickedUpItems[i].GetComponent<Renderer>().enabled = false;
            }
        }
    }
}
