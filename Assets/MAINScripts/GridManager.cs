// ===== 1. GridManager.cs =====
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum TileType { Ground, Road, Building }
public enum RoadType { None, Straight, Turn, TJunction, Crossroad, PedestrianCrossing }
public enum BuildMode { None, Road, Building, Delete }

[System.Serializable]
public struct TileData
{
    public TileType type;
    public RoadType roadType;
    public GameObject currentObject;
    public GameObject buildingObject; // НОВИЙ: окремо зберігаємо будинок
    public BuildingData buildingData;

    public bool IsEmpty => type == TileType.Ground && currentObject != null;
    public bool HasRoad => type == TileType.Road;
    public bool HasBuilding => type == TileType.Building;
}

[System.Serializable]
public struct BuildingData
{
    public string buildingId;
    public Vector2Int size;
    public Vector2Int originPosition;
}

[CreateAssetMenu(fileName = "BuildingConfig", menuName = "City Builder/Building Config")]
public class BuildingConfig : ScriptableObject
{
    public string buildingName;
    public string buildingId;
    public Vector2Int size = Vector2Int.one;
    public GameObject prefab;
    public Sprite icon;
    public string description;
    public int cost = 100;
}

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 20;
    [SerializeField] private int gridHeight = 20;
    [SerializeField] private float tileSize = 1f;

    [Header("Prefabs")]
    [SerializeField] private GameObject groundTilePrefab;
    [SerializeField] private GameObject[] roadPrefabs; // 5 типів доріг
    [SerializeField] private GameObject concreteTilePrefab; // НОВИЙ ПРЕФАБ пластини бетонної під будівлею

    [Header("Building Configs")]
    [SerializeField] private BuildingConfig[] availableBuildings;

    [Header("Input & Preview")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GameObject roadPreviewPrefab;
    [SerializeField] private GameObject buildingPreviewPrefab;

    private TileData[,] gridData;
    private Transform groundParent;
    private Transform roadParent;
    private Transform buildingParent;

    // Input system
    private Mouse mouse;
    private BuildMode currentMode = BuildMode.None;
    private BuildingConfig selectedBuilding;
    private GameObject currentPreview;
    private Vector2Int lastPreviewPosition = Vector2Int.one * -1;

    // Events
    public System.Action<Vector2Int, TileType> OnTileChanged;
    public System.Action<BuildMode> OnModeChanged;

    // Public properties
    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;
    public float TileSize => tileSize;
    public BuildMode CurrentMode => currentMode;
    public BuildingConfig SelectedBuilding => selectedBuilding;

    void Start()
    {
        InitializeGrid();
        InitializeInput();
        GenerateWorld(); // Генеруємо світ одразу
        DebugBuildingInfo();
    }

    void Update()
    {
        HandleInput();
        UpdatePreview();
    }

    private void InitializeGrid()
    {
        gridData = new TileData[gridWidth, gridHeight];

        groundParent = new GameObject("Ground").transform;
        roadParent = new GameObject("Roads").transform;
        buildingParent = new GameObject("Buildings").transform;

        groundParent.parent = transform;
        roadParent.parent = transform;
        buildingParent.parent = transform;
    }

    private void InitializeInput()
    {
        mouse = Mouse.current;
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    public void GenerateWorld()
    {
        Debug.Log("=== GenerateWorld Debug ===");
        Debug.Log($"groundTilePrefab: {groundTilePrefab?.name ?? "NULL"}");
        Debug.Log($"gridData: {gridData?.Length ?? 0}");
        Debug.Log($"groundParent: {groundParent?.name ?? "NULL"}");
        Debug.Log($"gridWidth: {gridWidth}, gridHeight: {gridHeight}");

        // Перевірки перед виконанням
        if (groundTilePrefab == null)
        {
            Debug.LogError("Ground Tile Prefab is NULL!");
            return;
        }

        // Очищуємо існуючий світ
        ClearWorld();

        // Перевірка після ClearWorld
        if (groundParent == null)
        {
            Debug.LogError("Ground Parent is NULL after ClearWorld!");
            return;
        }

        Debug.Log("Starting world generation...");

        // Генеруємо тільки землю
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector3 position = new Vector3(x * tileSize, 0f, z * tileSize);

                if (x == 0 && z == 0) Debug.Log($"Creating tile at ({x},{z}) position {position}");

                GameObject groundTile = Instantiate(groundTilePrefab, position, Quaternion.identity, groundParent);
                groundTile.name = $"Ground_{x}_{z}";

                gridData[x, z] = new TileData
                {
                    type = TileType.Ground,
                    roadType = RoadType.None,
                    currentObject = groundTile,
                    buildingObject = null,
                    buildingData = new BuildingData()
                };

                if (x == 0 && z == 0) Debug.Log("First tile created successfully");
            }
        }

        Debug.Log("World generation completed!");
    }

    private void ClearWorld()
    {
        // Очищуємо preview
        DestroyCurrentPreview();

        // Знищуємо всі дочірні об'єкти
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }

        // Повторно створюємо батьківські об'єкти
        groundParent = new GameObject("Ground").transform;
        roadParent = new GameObject("Roads").transform;
        buildingParent = new GameObject("Buildings").transform;

        groundParent.parent = transform;
        roadParent.parent = transform;
        buildingParent.parent = transform;
    }

    // ===== INPUT HANDLING =====
    private void HandleInput()
    {
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            Vector2Int gridPosition = GetGridPositionFromMouse();
            if (gridPosition.x >= 0)
            {
                HandleGridClick(gridPosition);
            }
        }

        if (mouse.rightButton.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetBuildMode(BuildMode.None, null);
        }
    }

    private void HandleGridClick(Vector2Int gridPosition)
    {
        int x = gridPosition.x;
        int z = gridPosition.y;

        switch (currentMode)
        {
            case BuildMode.Road:
                if (CanPlaceRoad(x, z))
                {
                    PlaceRoad(x, z);
                }
                break;

            case BuildMode.Building:
                if (selectedBuilding != null && CanPlaceBuilding(x, z, selectedBuilding.size))
                {
                    PlaceBuilding(x, z, selectedBuilding);
                }
                break;

            case BuildMode.Delete:
                RemoveTile(x, z);
                break;
        }
    }

    private Vector2Int GetGridPositionFromMouse()
    {
        Vector2 mousePosition = mouse.position.ReadValue();
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(mousePosition.x, mousePosition.y, 0));

        // Raycast на площину Y=0
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 worldPosition = ray.GetPoint(distance);

            int x = Mathf.RoundToInt(worldPosition.x / tileSize);
            int z = Mathf.RoundToInt(worldPosition.z / tileSize);

            if (IsValidPosition(x, z))
            {
                return new Vector2Int(x, z);
            }
        }

        return Vector2Int.one * -1;
    }

    // ===== PREVIEW SYSTEM =====
    private void UpdatePreview()
    {
        Vector2Int gridPosition = GetGridPositionFromMouse();

        if (gridPosition.x >= 0 && gridPosition != lastPreviewPosition)
        {
            UpdatePreviewPosition(gridPosition);
            lastPreviewPosition = gridPosition;
        }
    }

    private void UpdatePreviewPosition(Vector2Int gridPosition)
    {
        DestroyCurrentPreview();

        if (currentMode == BuildMode.None) return;

        int x = gridPosition.x;
        int z = gridPosition.y;

        switch (currentMode)
        {
            case BuildMode.Road:
                if (CanPlaceRoad(x, z) && roadPreviewPrefab != null)
                {
                    Vector3 previewPos = new Vector3(x * tileSize, 0.01f, z * tileSize);
                    CreatePreview(roadPreviewPrefab, previewPos, Vector3.one);
                }
                break;

            case BuildMode.Building:
                if (selectedBuilding != null && CanPlaceBuilding(x, z, selectedBuilding.size))
                {
                    Vector3 previewPos = new Vector3(
                        (x + (selectedBuilding.size.x - 1) * 0.5f) * tileSize,
                        0.5f, // Така ж висота як у реального будинку
                        (z + (selectedBuilding.size.y - 1) * 0.5f) * tileSize
                    );

                    // Використовуємо префаб самого будинку!
                    CreatePreview(selectedBuilding.prefab, previewPos, Vector3.one);
                }
                break;

            case BuildMode.Delete:
                TileData tileData = GetTileData(x, z);
                if (tileData.type != TileType.Ground)
                {
                    Vector3 deletePos = new Vector3(x * tileSize, 0.01f, z * tileSize);
                    CreateDeletePreview(deletePos);
                }
                break;
        }
    }

    private void CreatePreview(GameObject prefab, Vector3 position, Vector3 scale)
    {
        currentPreview = Instantiate(prefab, position, Quaternion.identity);
        currentPreview.transform.localScale = scale;

        // Робимо напівпрозорим
        Renderer[] renderers = currentPreview.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.materials;
            foreach (Material material in materials)
            {
                // Встановлюємо прозорість
                Color color = material.color;
                color.a = 0.5f;
                material.color = color;

                // Робимо матеріал прозорим
                material.SetFloat("_Mode", 3); // Transparent mode
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
            }
        }

        // Вимикаємо колайдери щоб не блокувати raycast
        Collider[] colliders = currentPreview.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
    }

    private void CreateDeletePreview(Vector3 position)
    {
        currentPreview = GameObject.CreatePrimitive(PrimitiveType.Cube);
        currentPreview.transform.position = position;
        currentPreview.transform.localScale = new Vector3(1, 0.1f, 1);

        Renderer renderer = currentPreview.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(1f, 0f, 0f, 0.5f);
        }

        Collider collider = currentPreview.GetComponent<Collider>();
        if (collider != null)
        {
            DestroyImmediate(collider);
        }
    }

    private void DestroyCurrentPreview()
    {
        if (currentPreview != null)
        {
            DestroyImmediate(currentPreview);
            currentPreview = null;
        }
    }

    // ===== PUBLIC METHODS FOR UI =====
    public void SetBuildMode(BuildMode mode, BuildingConfig building = null)
    {
        currentMode = mode;
        selectedBuilding = building;

        DestroyCurrentPreview();
        lastPreviewPosition = Vector2Int.one * -1;

        OnModeChanged?.Invoke(mode);
        Debug.Log($"Режим змінено на: {mode}");
    }

    public bool IsValidPosition(int x, int z)
    {
        return x >= 0 && x < gridWidth && z >= 0 && z < gridHeight;
    }

    public TileData GetTileData(int x, int z)
    {
        if (!IsValidPosition(x, z)) return new TileData();
        return gridData[x, z];
    }

    public bool CanPlaceRoad(int x, int z)
    {
        if (!IsValidPosition(x, z)) return false;
        return gridData[x, z].type == TileType.Ground;
    }

    public bool CanPlaceBuilding(int x, int z, Vector2Int size)
    {
        // Перевіряємо всі клітинки, які займе будинок
        for (int dx = 0; dx < size.x; dx++)
        {
            for (int dz = 0; dz < size.y; dz++)
            {
                int checkX = x + dx;
                int checkZ = z + dz;

                if (!IsValidPosition(checkX, checkZ))
                {
                    Debug.Log($"Invalid position: ({checkX}, {checkZ})");
                    return false;
                }

                TileType tileType = gridData[checkX, checkZ].type;
                if (tileType != TileType.Ground)
                {
                    Debug.Log($"Cannot place building at ({checkX}, {checkZ}) - tile type: {tileType}");
                    return false;
                }
            }
        }

        Debug.Log($"Can place building at ({x}, {z}) with size {size}");
        return true;
    }

    public void PlaceBuilding(int x, int z, BuildingConfig buildingConfig)
    {
        if (!CanPlaceBuilding(x, z, buildingConfig.size)) return;

        Debug.Log($"Placing building {buildingConfig.buildingName} at ({x}, {z})");

        // Створюємо будинок
        Vector3 buildingPos = new Vector3(
            (x + (buildingConfig.size.x - 1) * 0.5f) * tileSize,
            0.5f,
            (z + (buildingConfig.size.y - 1) * 0.5f) * tileSize
        );

        GameObject building = Instantiate(buildingConfig.prefab, buildingPos, Quaternion.identity, buildingParent);
        building.name = $"Building_{buildingConfig.buildingId}_{x}_{z}";

        // Оновлюємо дані сітки
        BuildingData buildingData = new BuildingData
        {
            buildingId = buildingConfig.buildingId,
            size = buildingConfig.size,
            originPosition = new Vector2Int(x, z)
        };

        // Замінюємо землю на бетон під всім будинком і оновлюємо дані
        for (int dx = 0; dx < buildingConfig.size.x; dx++)
        {
            for (int dz = 0; dz < buildingConfig.size.y; dz++)
            {
                int tileX = x + dx;
                int tileZ = z + dz;

                // Видаляємо стару землю
                if (gridData[tileX, tileZ].currentObject != null)
                {
                    DestroyImmediate(gridData[tileX, tileZ].currentObject);
                }

                // Створюємо бетон
                Vector3 concretePos = new Vector3(tileX * tileSize, 0f, tileZ * tileSize);
                GameObject concreteTile = Instantiate(concreteTilePrefab, concretePos, Quaternion.identity, groundParent);
                concreteTile.name = $"Concrete_{tileX}_{tileZ}";

                // Оновлюємо дані тайла
                TileData tileData = gridData[tileX, tileZ];
                tileData.type = TileType.Building;
                tileData.buildingData = buildingData;
                tileData.currentObject = concreteTile; // currentObject завжди бетон
                tileData.buildingObject = building; // buildingObject завжди будинок
                gridData[tileX, tileZ] = tileData;

                Debug.Log($"Updated tile ({tileX}, {tileZ}): concrete={concreteTile.name}, building={building.name}");
            }
        }

        Debug.Log($"Building placed successfully: {building.name}");
    }

    public void PlaceRoad(int x, int z)
    {
        if (!CanPlaceRoad(x, z)) return;

        if (gridData[x, z].currentObject != null)
        {
            DestroyImmediate(gridData[x, z].currentObject);
        }

        Vector3 position = new Vector3(x * tileSize, 0f, z * tileSize);
        GameObject roadTile = Instantiate(roadPrefabs[0], position, Quaternion.identity, roadParent);
        roadTile.name = $"Road_{x}_{z}";

        gridData[x, z] = new TileData
        {
            type = TileType.Road,
            roadType = RoadType.Straight,
            currentObject = roadTile,
            buildingObject = null, // Додав це поле
            buildingData = new BuildingData()
        };

        UpdateRoadConnections();
        OnTileChanged?.Invoke(new Vector2Int(x, z), TileType.Road);
    }

    public void RemoveTile(int x, int z)
    {
        if (!IsValidPosition(x, z)) return;

        TileData tileData = gridData[x, z];

        if (tileData.type == TileType.Building)
        {
            // Видаляємо будинок з бетоном
            RemoveBuildingOnly(tileData.buildingData.originPosition.x, tileData.buildingData.originPosition.y, tileData.buildingData.size);
        }
        else if (tileData.type == TileType.Road)
        {
            // Для доріг як було - видаляємо дорогу і повертаємо землю
            if (tileData.currentObject != null)
            {
                DestroyImmediate(tileData.currentObject);
            }
            CreateGroundTile(x, z);
            UpdateRoadConnections();
        }

        OnTileChanged?.Invoke(new Vector2Int(x, z), TileType.Ground);
    }

    // ВИПРАВЛЕНИЙ метод видалення будинку
    private void RemoveBuildingOnly(int originX, int originZ, Vector2Int size)
    {
        Debug.Log($"Removing building at ({originX}, {originZ}) size {size.x}x{size.y}");

        // Видаляємо будинок з будь-якої клітинки (всі посилаються на той самий об'єкт)
        GameObject buildingToRemove = gridData[originX, originZ].buildingObject;
        if (buildingToRemove != null)
        {
            Debug.Log($"Destroying building: {buildingToRemove.name}");
            DestroyImmediate(buildingToRemove);
        }

        // Замінюємо бетон на землю для ВСІХ клітинок будинку
        for (int dx = 0; dx < size.x; dx++)
        {
            for (int dz = 0; dz < size.y; dz++)
            {
                int tileX = originX + dx;
                int tileZ = originZ + dz;

                // Отримуємо поточний тайл
                TileData tileData = gridData[tileX, tileZ];

                // Видаляємо бетон (currentObject)
                if (tileData.currentObject != null)
                {
                    Debug.Log($"Destroying concrete: {tileData.currentObject.name}");
                    DestroyImmediate(tileData.currentObject);
                }

                // Створюємо нову землю
                Vector3 position = new Vector3(tileX * tileSize, 0f, tileZ * tileSize);
                GameObject groundTile = Instantiate(groundTilePrefab, position, Quaternion.identity, groundParent);
                groundTile.name = $"Ground_{tileX}_{tileZ}";

                // Оновлюємо дані тайла
                tileData.type = TileType.Ground;
                tileData.currentObject = groundTile;
                tileData.buildingObject = null;
                tileData.buildingData = new BuildingData();
                gridData[tileX, tileZ] = tileData;

                Debug.Log($"Restored ground at ({tileX}, {tileZ})");
            }
        }
    }

    private void CreateGroundTile(int x, int z)
    {
        Vector3 position = new Vector3(x * tileSize, 0f, z * tileSize);
        GameObject groundTile = Instantiate(groundTilePrefab, position, Quaternion.identity, groundParent);
        groundTile.name = $"Ground_{x}_{z}";

        gridData[x, z] = new TileData
        {
            type = TileType.Ground,
            roadType = RoadType.None,
            currentObject = groundTile,
            buildingObject = null,
            buildingData = new BuildingData()
        };
    }

    // ===== ROAD CONNECTION SYSTEM =====
    private void UpdateRoadConnections()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                if (gridData[x, z].type == TileType.Road)
                {
                    UpdateRoadTile(x, z);
                }
            }
        }
    }

    private void UpdateRoadTile(int x, int z)
    {
        bool hasNorth = IsValidPosition(x, z + 1) && gridData[x, z + 1].HasRoad;
        bool hasSouth = IsValidPosition(x, z - 1) && gridData[x, z - 1].HasRoad;
        bool hasEast = IsValidPosition(x + 1, z) && gridData[x + 1, z].HasRoad;
        bool hasWest = IsValidPosition(x - 1, z) && gridData[x - 1, z].HasRoad;

        int connectionCount = (hasNorth ? 1 : 0) + (hasSouth ? 1 : 0) + (hasEast ? 1 : 0) + (hasWest ? 1 : 0);

        RoadType newRoadType = RoadType.Straight;
        Quaternion rotation = Quaternion.identity;

        if (connectionCount == 0 || connectionCount == 1)
        {
            // Для одинокої дороги або тупика - зберігаємо оригінальний тип
            newRoadType = gridData[x, z].roadType; // Зберігаємо що було (Straight або PedestrianCrossing)
            if (hasNorth || hasSouth)
                rotation = Quaternion.identity;
            else
                rotation = Quaternion.Euler(0, 90, 0);
        }
        else if (connectionCount == 2)
        {
            if ((hasNorth && hasSouth) || (hasEast && hasWest))
            {
                // Пряма дорога - зберігаємо оригінальний тип
                newRoadType = gridData[x, z].roadType; // Зберігаємо що було (Straight або PedestrianCrossing)
                rotation = (hasNorth && hasSouth) ? Quaternion.identity : Quaternion.Euler(0, 90, 0);
            }
            else
            {
                // Поворот - завжди Turn
                newRoadType = RoadType.Turn;

                if (hasNorth && hasEast)
                    rotation = Quaternion.Euler(0, 90, 0);
                else if (hasEast && hasSouth)
                    rotation = Quaternion.Euler(0, 180, 0);
                else if (hasSouth && hasWest)
                    rotation = Quaternion.Euler(0, 270, 0);
                else if (hasWest && hasNorth)
                    rotation = Quaternion.Euler(0, 0, 0);
            }
        }
        else if (connectionCount == 3)
        {
            // T-подібне перехрестя
            newRoadType = RoadType.Crossroad;

            if (!hasNorth)
                rotation = Quaternion.Euler(0, 270, 0);
            else if (!hasEast)
                rotation = Quaternion.Euler(0, 0, 0);
            else if (!hasSouth)
                rotation = Quaternion.Euler(0, 90, 0);
            else if (!hasWest)
                rotation = Quaternion.Euler(0, 180, 0);
        }
        else if (connectionCount == 4)
        {
            // Повне перехрестя
            newRoadType = RoadType.TJunction;
            rotation = Quaternion.identity;
        }

        // Оновлюємо тільки якщо щось змінилося
        if (gridData[x, z].roadType != newRoadType ||
            gridData[x, z].currentObject.transform.rotation != rotation)
        {
            UpdateRoadVisual(x, z, newRoadType, rotation);
        }
    }

    private void UpdateRoadVisual(int x, int z, RoadType roadType, Quaternion rotation)
    {
        if (gridData[x, z].currentObject != null)
        {
            DestroyImmediate(gridData[x, z].currentObject);
        }

        GameObject prefab = roadPrefabs[(int)roadType];
        Vector3 position = new Vector3(x * tileSize, 0f, z * tileSize);
        GameObject newRoadTile = Instantiate(prefab, position, rotation, roadParent);
        newRoadTile.name = $"Road_{roadType}_{x}_{z}";

        TileData tileData = gridData[x, z];
        tileData.roadType = roadType;
        tileData.currentObject = newRoadTile;
        // buildingObject залишається null для доріг
        gridData[x, z] = tileData;
    }

    public BuildingConfig[] GetAvailableBuildings()
    {
        return availableBuildings;
    }

    void OnDestroy()
    {
        DestroyCurrentPreview();
    }

    public void DebugBuildingInfo()
    {
        Debug.Log("=== Building Debug Info ===");
        Debug.Log($"Available buildings count: {availableBuildings?.Length ?? 0}");

        if (availableBuildings != null)
        {
            for (int i = 0; i < availableBuildings.Length; i++)
            {
                var building = availableBuildings[i];
                Debug.Log($"Building {i}: {building?.buildingName ?? "NULL"}, Prefab: {building?.prefab?.name ?? "NULL"}");
            }
        }

        BuildingConfig currentBuilding = selectedBuilding;
        Debug.Log($"Selected building: {currentBuilding?.buildingName ?? "NULL"}");
    }
}