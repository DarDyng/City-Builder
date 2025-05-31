using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TrafficSystem : MonoBehaviour
{
    [Header("Traffic Settings")]
    [SerializeField] private GameObject[] carPrefabs;
    [SerializeField] private int maxCarsTotal = 20; // Максимум машин загалом
    [SerializeField] private float carSpeed = 5f;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private float carHeight = 0.5f;
    [SerializeField] private float laneOffset = 0.25f; // Зміщення від центру дороги

    [Header("Traffic Rules")]
    [SerializeField] private bool rightHandTraffic = true; // true = правосторонній рух

    [Header("References")]
    [SerializeField] private GridManager gridManager;

    private Transform carsParent;
    private List<Car> activeCars = new List<Car>();
    private List<Vector2Int> roadPositions = new List<Vector2Int>();
    private Dictionary<Vector2Int, List<Vector2Int>> roadConnections = new Dictionary<Vector2Int, List<Vector2Int>>();

    private float tileSize = 20f;
    private float nextSpawnTime = 0f;

    private class Car
    {
        public GameObject gameObject;
        public Vector2Int currentTile;
        public Vector2Int targetTile;
        public Vector2Int previousTile;
        public float moveProgress = 0f;
        public int laneIndex; // 0 = права смуга
        public Vector3 currentLaneOffset;
    }

    void Start()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();

        if (gridManager != null)
        {
            tileSize = gridManager.TileSize;
            gridManager.OnTileChanged += OnTileChanged;
        }

        carsParent = new GameObject("TrafficCars").transform;
        carsParent.parent = transform;

        ScanRoads();
    }

    void Update()
    {
        UpdateCars();

        // Спавн нових машин
        if (Time.time >= nextSpawnTime && roadPositions.Count > 1 && activeCars.Count < maxCarsTotal)
        {
            SpawnCar();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    private void SpawnCar()
    {
        if (carPrefabs == null || carPrefabs.Length == 0 || roadPositions.Count == 0)
            return;

        // Вибираємо випадкову стартову позицію
        Vector2Int startPos = roadPositions[Random.Range(0, roadPositions.Count)];

        // Перевіряємо чи є куди їхати
        if (!roadConnections.ContainsKey(startPos) || roadConnections[startPos].Count == 0)
            return;

        // Вибираємо напрямок
        List<Vector2Int> possibleDirections = roadConnections[startPos];
        Vector2Int firstTarget = possibleDirections[Random.Range(0, possibleDirections.Count)];

        // Визначаємо смугу та зміщення
        Vector2Int direction = firstTarget - startPos;
        Vector3 laneOffset = CalculateLaneOffset(direction, 0); // Завжди права смуга

        // Створюємо машину
        GameObject carPrefab = carPrefabs[Random.Range(0, carPrefabs.Length)];
        Vector3 worldPos = GetWorldPosition(startPos) + laneOffset;
        worldPos.y = carHeight;

        GameObject carObj = Instantiate(carPrefab, worldPos, Quaternion.identity, carsParent);

        Car car = new Car
        {
            gameObject = carObj,
            currentTile = startPos,
            targetTile = firstTarget,
            previousTile = startPos,
            laneIndex = 0,
            currentLaneOffset = laneOffset
        };

        activeCars.Add(car);

        // Встановлюємо початковий поворот
        UpdateCarRotation(car, direction);
    }

    private Vector3 CalculateLaneOffset(Vector2Int direction, int laneIndex)
    {
        Vector3 dir3D = new Vector3(direction.x, 0, direction.y);
        Vector3 perpendicular = Vector3.Cross(Vector3.up, dir3D).normalized;

        // Для правостороннього руху зміщуємо вправо
        float offsetMultiplier = rightHandTraffic ? 1f : -1f;

        return perpendicular * (laneOffset * tileSize * offsetMultiplier);
    }

    private void UpdateCars()
    {
        for (int i = activeCars.Count - 1; i >= 0; i--)
        {
            Car car = activeCars[i];

            // Перевіряємо чи дорога ще існує
            if (!roadPositions.Contains(car.currentTile))
            {
                Destroy(car.gameObject);
                activeCars.RemoveAt(i);
                continue;
            }

            // Рух машини з постійною швидкістю (без гальмування)
            if (car.currentTile != car.targetTile)
            {
                car.moveProgress += (carSpeed / tileSize) * Time.deltaTime;

                if (car.moveProgress >= 1f)
                {
                    // Досягли цільової клітинки
                    car.previousTile = car.currentTile;
                    car.currentTile = car.targetTile;
                    car.moveProgress = 0f;

                    // Знаходимо наступну ціль
                    FindNextTarget(car);
                }
                else
                {
                    // Інтерполяція позиції з урахуванням смуги
                    UpdateCarPosition(car);
                }
            }
        }
    }

    private void UpdateCarPosition(Car car)
    {
        Vector3 startPos = GetWorldPosition(car.currentTile);
        Vector3 endPos = GetWorldPosition(car.targetTile);

        // Обчислюємо зміщення для смуги
        Vector2Int currentDirection = car.targetTile - car.currentTile;
        Vector3 currentLaneOffset = CalculateLaneOffset(currentDirection, car.laneIndex);

        // Плавна зміна зміщення смуги при поворотах
        car.currentLaneOffset = Vector3.Lerp(car.currentLaneOffset, currentLaneOffset, Time.deltaTime * 5f);

        // Позиція з урахуванням смуги
        startPos += car.currentLaneOffset;
        endPos += currentLaneOffset;

        startPos.y = endPos.y = carHeight;

        // Інтерполяція позиції
        car.gameObject.transform.position = Vector3.Lerp(startPos, endPos, car.moveProgress);

        // Оновлення повороту
        UpdateCarRotation(car, currentDirection);
    }

    private void UpdateCarRotation(Car car, Vector2Int direction)
    {
        Vector3 dir3D = new Vector3(direction.x, 0, direction.y);
        if (dir3D != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(dir3D);
            car.gameObject.transform.rotation = Quaternion.Slerp(
                car.gameObject.transform.rotation,
                targetRotation,
                Time.deltaTime * 10f
            );
        }
    }

    private void FindNextTarget(Car car)
    {
        if (!roadConnections.ContainsKey(car.currentTile)) return;

        List<Vector2Int> possibleTargets = new List<Vector2Int>(roadConnections[car.currentTile]);

        // Видаляємо дорогу з якої приїхали (щоб не розвертатися)
        possibleTargets.Remove(car.previousTile);

        if (possibleTargets.Count == 0)
        {
            // Тупик - розвертаємося
            possibleTargets.Add(car.previousTile);
        }

        if (possibleTargets.Count > 0)
        {
            // Вибираємо наступну дорогу з урахуванням пріоритету прямого руху
            Vector2Int currentDirection = car.currentTile - car.previousTile;
            Vector2Int preferredTarget = car.currentTile + currentDirection;

            if (possibleTargets.Contains(preferredTarget) && Random.value > 0.3f)
            {
                // 70% шанс їхати прямо
                car.targetTile = preferredTarget;
            }
            else
            {
                // Випадковий поворот
                car.targetTile = possibleTargets[Random.Range(0, possibleTargets.Count)];
            }
        }
    }

    private void ScanRoads()
    {
        roadPositions.Clear();
        roadConnections.Clear();

        if (gridManager == null) return;

        // Знаходимо всі дороги
        for (int x = 0; x < gridManager.GridWidth; x++)
        {
            for (int z = 0; z < gridManager.GridHeight; z++)
            {
                TileData tile = gridManager.GetTileData(x, z);
                if (tile.type == TileType.Road)
                {
                    Vector2Int pos = new Vector2Int(x, z);
                    roadPositions.Add(pos);
                }
            }
        }

        BuildRoadConnections();
        Debug.Log($"Знайдено {roadPositions.Count} доріг для руху транспорту");
    }

    private void BuildRoadConnections()
    {
        foreach (Vector2Int roadPos in roadPositions)
        {
            List<Vector2Int> connections = new List<Vector2Int>();

            Vector2Int[] directions = {
                new Vector2Int(0, 1),   // Північ
                new Vector2Int(1, 0),   // Схід
                new Vector2Int(0, -1),  // Південь
                new Vector2Int(-1, 0)   // Захід
            };

            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighborPos = roadPos + dir;
                if (roadPositions.Contains(neighborPos))
                {
                    connections.Add(neighborPos);
                }
            }

            roadConnections[roadPos] = connections;
        }
    }

    private Vector3 GetWorldPosition(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x * tileSize, 0, gridPos.y * tileSize);
    }

    private void OnTileChanged(Vector2Int position, TileType newType)
    {
        ScanRoads();

        // Видаляємо машини на видалених дорогах
        if (newType != TileType.Road)
        {
            for (int i = activeCars.Count - 1; i >= 0; i--)
            {
                if (activeCars[i].currentTile == position || activeCars[i].targetTile == position)
                {
                    Destroy(activeCars[i].gameObject);
                    activeCars.RemoveAt(i);
                }
            }
        }
    }

    void OnDestroy()
    {
        if (gridManager != null)
        {
            gridManager.OnTileChanged -= OnTileChanged;
        }
    }

    // Публічні методи для UI
    public void SetCarSpeed(float speed)
    {
        carSpeed = Mathf.Max(0.1f, speed);
    }

    public void SetSpawnInterval(float interval)
    {
        spawnInterval = Mathf.Max(0.5f, interval);
    }

    public void SetMaxCars(int maxCars)
    {
        maxCarsTotal = Mathf.Max(1, maxCars);
    }
}