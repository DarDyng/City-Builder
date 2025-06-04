using UnityEngine;

public class CityBoundary : MonoBehaviour
{
    [Header("Boundary Settings")]
    [SerializeField] private float boundaryHeight = 2f; // ������ ���
    [SerializeField] private float boundaryThickness = 0.2f; // ������� ����
    [SerializeField] private float tileSize = 20f; // ����� �������

    [Header("Visual Settings")]
    [SerializeField] private Color boundaryColor = new Color(0.5f, 0.5f, 1f, 0.5f); // ������������ ���������
    [SerializeField] private Material boundaryMaterial; 

    [Header("References")]
    [SerializeField] private GridManager gridManager;

    private Transform boundaryParent;
    private int gridWidth = 20;
    private int gridHeight = 20;

    void Start()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (gridManager != null)
        {
            gridWidth = gridManager.GridWidth;
            gridHeight = gridManager.GridHeight;
        }

        CreateBoundary();
    }

    public void CreateBoundary()
    {
        Debug.Log("��������� ���� ����...");

        // ��������� ����������� ��'���
        if (boundaryParent != null)
            DestroyImmediate(boundaryParent.gameObject);

        boundaryParent = new GameObject("CityBoundary").transform;
        boundaryParent.parent = transform;

        // ��������� 4 ����
        CreateWall("North", new Vector3(gridWidth * tileSize / 2f, boundaryHeight / 2f, gridHeight * tileSize),
                   new Vector3(gridWidth * tileSize + boundaryThickness, boundaryHeight, boundaryThickness));

        CreateWall("South", new Vector3(gridWidth * tileSize / 2f, boundaryHeight / 2f, 0),
                   new Vector3(gridWidth * tileSize + boundaryThickness, boundaryHeight, boundaryThickness));

        CreateWall("East", new Vector3(gridWidth * tileSize, boundaryHeight / 2f, gridHeight * tileSize / 2f),
                   new Vector3(boundaryThickness, boundaryHeight, gridHeight * tileSize));

        CreateWall("West", new Vector3(0, boundaryHeight / 2f, gridHeight * tileSize / 2f),
                   new Vector3(boundaryThickness, boundaryHeight, gridHeight * tileSize));

        // ��������� ��������� �� �����
        CreateCornerPosts();

        Debug.Log("���� ���� ��������!");
    }

    private void CreateWall(string name, Vector3 position, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = $"BoundaryWall_{name}";
        wall.transform.parent = boundaryParent;
        wall.transform.position = position;
        wall.transform.localScale = scale;

        // ����������� �������
        Renderer renderer = wall.GetComponent<Renderer>();
        if (boundaryMaterial != null)
        {
            renderer.material = boundaryMaterial;
        }
        else
        {
            // ��������� ������� �������� �������
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = boundaryColor;

            // ������ ��������
            mat.SetFloat("_Mode", 3); // Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;

            renderer.material = mat;
        }

        // �������� �������� ��� �� ������� ��
        Collider collider = wall.GetComponent<Collider>();
        if (collider != null)
            collider.enabled = false;
    }

    private void CreateCornerPosts()
    {
        float postSize = boundaryThickness * 2f;
        float postHeight = boundaryHeight * 1.2f;

        // ������� ����
        Vector3[] cornerPositions = new Vector3[]
        {
            new Vector3(0, postHeight / 2f, 0), // SW
            new Vector3(gridWidth * tileSize, postHeight / 2f, 0), // SE
            new Vector3(0, postHeight / 2f, gridHeight * tileSize), // NW
            new Vector3(gridWidth * tileSize, postHeight / 2f, gridHeight * tileSize) // NE
        };

        for (int i = 0; i < cornerPositions.Length; i++)
        {
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = $"CornerPost_{i}";
            post.transform.parent = boundaryParent;
            post.transform.position = cornerPositions[i];
            post.transform.localScale = new Vector3(postSize, postHeight / 2f, postSize);

            // ��� ����� ������� �� � ����
            Renderer renderer = post.GetComponent<Renderer>();
            if (boundaryParent.GetChild(0).GetComponent<Renderer>() != null)
            {
                renderer.material = boundaryParent.GetChild(0).GetComponent<Renderer>().material;
            }

            // �������� ��������
            Collider collider = post.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;
        }
    }

    public void SetBoundaryColor(Color newColor)
    {
        boundaryColor = newColor;

        // ��������� �� ��������
        if (boundaryParent != null)
        {
            Renderer[] renderers = boundaryParent.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                if (renderer.material != null)
                {
                    renderer.material.color = boundaryColor;
                }
            }
        }
    }

    public void SetBoundaryHeight(float height)
    {
        boundaryHeight = Mathf.Max(0.5f, height);
        CreateBoundary(); // ������������� � ����� �������
    }

    public void ToggleBoundary(bool show)
    {
        if (boundaryParent != null)
            boundaryParent.gameObject.SetActive(show);
    }
}