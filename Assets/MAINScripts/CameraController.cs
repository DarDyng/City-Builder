using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Camera Movement")]
    [SerializeField] private float dragSpeed = 2.0f;
    [SerializeField] private float zoomSpeed = 5.0f;
    [SerializeField] private float minZoom = 5.0f;
    [SerializeField] private float maxZoom = 50.0f;
    [SerializeField] private bool invertDragDirection = true;

    [Header("Boundaries")]
    [SerializeField] private bool useBoundaries = true;
    [SerializeField] private float minX = -50f;
    [SerializeField] private float maxX = 50f;
    [SerializeField] private float minZ = -50f;
    [SerializeField] private float maxZ = 50f;

    private Vector2 lastMousePosition;
    private bool isDragging = false;
    private Camera cam;
    private Plane dragPlane;

    // ��������� �� ���������� ������� �����
    private Mouse mouse;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("CameraController requires a Camera component!");
            enabled = false;
        }

        // �������� ��������� �� ������� �����
        mouse = Mouse.current;
        if (mouse == null)
        {
            Debug.LogError("No mouse detected!");
            enabled = false;
        }

        // ��������� ������� ��� �������� ���� ����
        // �� ������� ����������� ���������� �� ���� �� ����� Y=0
        dragPlane = new Plane(Vector3.up, Vector3.zero);
    }

    void Update()
    {
        if (mouse != null)
        {
            HandleMouseDrag();
            HandleZoom();
        }
    }

    void HandleMouseDrag()
    {
        // ���������� ���������� �������� ������ ����
        if (mouse.middleButton.wasPressedThisFrame)
        {
            isDragging = true;
            lastMousePosition = mouse.position.ReadValue();
            return;
        }

        // ���������� ���������� ������ ����
        if (mouse.middleButton.wasReleasedThisFrame)
        {
            isDragging = false;
            return;
        }

        // ���� �� ����������
        if (isDragging && mouse.middleButton.isPressed)
        {
            Vector2 currentMousePosition = mouse.position.ReadValue();

            // ��������� ������� ���� �� ������� ����
            Vector3 lastWorldPos = ProjectMouseOntoWorld(lastMousePosition);
            Vector3 currentWorldPos = ProjectMouseOntoWorld(currentMousePosition);

            if (lastWorldPos != Vector3.zero && currentWorldPos != Vector3.zero)
            {
                // ���������� ������ � ������� �����������
                Vector3 worldPosDifference = lastWorldPos - currentWorldPos;

                // ��������� ��������, ���� �������
                if (!invertDragDirection)
                {
                    worldPosDifference = -worldPosDifference;
                }

                // ������ ������ �� ����� ������ � ������� �����������
                // �������� Y-����������, ��� �������� ���� �� ������������� ������
                Vector3 movement = new Vector3(
                    worldPosDifference.x * dragSpeed,
                    0,
                    worldPosDifference.z * dragSpeed
                );

                transform.Translate(movement, Space.World);

                // ����������� ���������, ���� ���� �������
                if (useBoundaries)
                {
                    Vector3 position = transform.position;
                    position.x = Mathf.Clamp(position.x, minX, maxX);
                    position.z = Mathf.Clamp(position.z, minZ, maxZ);
                    transform.position = position;
                }
            }

            lastMousePosition = currentMousePosition;
        }
    }

    // �������� ������� ���� �� ������� ����
    private Vector3 ProjectMouseOntoWorld(Vector2 mousePosition)
    {
        Ray ray = cam.ScreenPointToRay(new Vector3(mousePosition.x, mousePosition.y, 0));

        if (dragPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }

        return Vector3.zero;
    }

    void HandleZoom()
    {
        // ������������� ������ ������� ����
        float scroll = mouse.scroll.y.ReadValue();
        if (Mathf.Abs(scroll) > 0.01f)
        {
            float newSize = cam.orthographicSize - scroll * zoomSpeed * 0.1f;
            cam.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
        }
    }
}