using UnityEngine;

public class PullDoor : MonoBehaviour
{
    public enum DoorOpeningDirection
    {
        Inwards,
        Outwards,
        Both
    }

    public Transform player;
    public float interactDistance = 3f;
    public RectTransform crosshair;
    public float crosshairMaxSize = 7f;
    public float crosshairMinSize = 3f;
    public DoorOpeningDirection openingDirection = DoorOpeningDirection.Both;
    public float doorOpenAngle = 90f; // Set the angle to open the door in either direction

    private bool isDragging = false;
    private Camera playerCamera;
    private float currentRotation = 0f;
    private Transform doorTransform;

    void Start()
    {
        playerCamera = Camera.main;
        doorTransform = transform.GetChild(0); // Assuming the door is the first child
    }

    void Update()
    {
        if (playerCamera == null)
        {
            Debug.LogError("playerCamera is null");
            return;
        }

        if (player == null)
        {
            Debug.LogError("player is null");
            return;
        }

        if (crosshair == null)
        {
            Debug.LogError("crosshair is null");
            return;
        }

        float distance = Vector3.Distance(player.position, transform.position);
        RaycastHit hit;
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit) && hit.transform == doorTransform && distance <= interactDistance)
        {
            crosshair.sizeDelta = Vector2.Lerp(crosshair.sizeDelta, new Vector2(crosshairMaxSize, crosshairMaxSize), Time.deltaTime * 5);

            if (Input.GetMouseButtonDown(0))
            {
                isDragging = true;
            }
        }
        else
        {
            crosshair.sizeDelta = Vector2.Lerp(crosshair.sizeDelta, new Vector2(crosshairMinSize, crosshairMinSize), Time.deltaTime * 5);
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging)
        {
            float mouseDelta = Input.GetAxis("Mouse Y") * -1; // Change to Mouse Y and invert for up/down behavior
            float newRotation = currentRotation + mouseDelta * 2; // Adjust the sensitivity if needed

            // Clamp the rotation based on the selected opening direction
            switch (openingDirection)
            {
                case DoorOpeningDirection.Inwards:
                    newRotation = Mathf.Clamp(newRotation, -doorOpenAngle, 0f);
                    break;
                case DoorOpeningDirection.Outwards:
                    newRotation = Mathf.Clamp(newRotation, 0f, doorOpenAngle);
                    break;
                case DoorOpeningDirection.Both:
                    newRotation = Mathf.Clamp(newRotation, -doorOpenAngle, doorOpenAngle);
                    break;
            }

            float rotationChange = newRotation - currentRotation;
            transform.Rotate(Vector3.up, rotationChange); // Rotate around local Y-axis of the empty GameObject
            currentRotation = newRotation;
        }
    }
}
