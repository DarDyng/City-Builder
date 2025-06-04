using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

/// Тести для логіки City Builder
public class CityBuilderLogicTests
{
    // ===== ПРОСТИЙ ТЕСТ ДЛЯ ПОЧАТКУ =====
    [Test]
    public void Simple_Math_Test()
    {
        // Arrange (Підготовка)
        Debug.Log("🧪 Починаємо Simple_Math_Test");
        int a = 2;
        int b = 3;
        Debug.Log($"Значення: a = {a}, b = {b}");

        // Act (Дія)
        int result = a + b;
        Debug.Log($"Результат обчислення: {a} + {b} = {result}");

        // Assert (Перевірка)
        Assert.AreEqual(5, result); // ✅ Правильно!
        Debug.Log("✅ Тест пройшов успішно!");
    }

    // ===== СКЛАДНІШИЙ ТЕСТ: ПЕРЕВІРКА СІТКИ =====
    [Test]
    public void GridPosition_Validation_WorksCorrectly()
    {
        // Arrange
        int gridWidth = 20;
        int gridHeight = 20;

        // Test valid positions
        Assert.IsTrue(IsValidPosition(0, 0, gridWidth, gridHeight), "Позиція (0,0) має бути валідною");
        Assert.IsTrue(IsValidPosition(10, 15, gridWidth, gridHeight), "Позиція (10,15) має бути валідною");
        Assert.IsTrue(IsValidPosition(19, 19, gridWidth, gridHeight), "Позиція (19,19) має бути валідною");

        // Test invalid positions
        Assert.IsFalse(IsValidPosition(-1, 0, gridWidth, gridHeight), "Негативна X координата має бути невалідною");
        Assert.IsFalse(IsValidPosition(0, -1, gridWidth, gridHeight), "Негативна Z координата має бути невалідною");
        Assert.IsFalse(IsValidPosition(20, 0, gridWidth, gridHeight), "X=20 має бути поза межами для сітки 20x20");
        Assert.IsFalse(IsValidPosition(0, 20, gridWidth, gridHeight), "Z=20 має бути поза межами для сітки 20x20");
    }

    // ===== СКЛАДНИЙ ТЕСТ: РОЗМІЩЕННЯ БУДИНКУ =====
    [Test]
    public void Building_Placement_MultiTile_ChecksAllPositions()
    {
        // Arrange
        var grid = CreateTestGrid(10, 10);
        Vector2Int buildingPosition = new Vector2Int(2, 3);
        Vector2Int buildingSize = new Vector2Int(3, 2); // Будинок 3x2

        // Act - перевіряємо чи можна розмістити будинок
        bool canPlace = CanPlaceBuilding(grid, buildingPosition, buildingSize);

        // Assert
        Assert.IsTrue(canPlace, "Будинок 3x2 має поміститися на позиції (2,3) в сітці 10x10");

        // Test edge case - будинок частково поза межами
        Vector2Int edgePosition = new Vector2Int(8, 9);
        bool canPlaceAtEdge = CanPlaceBuilding(grid, edgePosition, buildingSize);

        Assert.IsFalse(canPlaceAtEdge, "Будинок 3x2 НЕ має поміститися на позиції (8,9) - виходить за межі");
    }

    // ===== ТЕСТ З ПЕРЕШКОДАМИ =====
    [Test]
    public void Building_Placement_AvoidObstacles()
    {
        // Arrange
        var grid = CreateTestGrid(10, 10);

        // Розміщуємо перешкоду (дорогу) на позиції (3,3)
        grid[3, 3] = "Road";

        Vector2Int buildingPosition = new Vector2Int(2, 2);
        Vector2Int buildingSize = new Vector2Int(3, 3); // Будинок 3x3

        // Act
        bool canPlace = CanPlaceBuilding(grid, buildingPosition, buildingSize);

        // Assert
        Assert.IsFalse(canPlace, "Будинок НЕ має розміщуватися поверх дороги");
    }

    // ===== ТЕСТ ОБЧИСЛЕННЯ СВІТОВИХ КООРДИНАТ =====
    [Test]
    public void WorldPosition_Calculation_IsAccurate()
    {
        // Arrange
        Vector2Int gridPos = new Vector2Int(5, 3);
        float tileSize = 2.5f;

        // Act
        Vector3 worldPos = GridToWorldPosition(gridPos, tileSize);

        // Assert
        Assert.AreEqual(12.5f, worldPos.x, 0.001f, "X координата має бути 5 * 2.5 = 12.5");
        Assert.AreEqual(0f, worldPos.y, 0.001f, "Y координата має бути 0");
        Assert.AreEqual(7.5f, worldPos.z, 0.001f, "Z координата має бути 3 * 2.5 = 7.5");
    }

    // ===== ТЕСТ З КІЛЬКОМА КРОКАМИ =====
    [Test]
    public void Traffic_SpawnInterval_CalculationIsCorrect()
    {
        // Arrange
        float spawnInterval = 2.0f;
        float currentTime = 10.0f;
        float lastSpawnTime = 8.5f;

        // Act
        bool shouldSpawn = (currentTime - lastSpawnTime) >= spawnInterval;
        float nextSpawnTime = lastSpawnTime + spawnInterval;

        // Assert
        Assert.IsFalse(shouldSpawn, "Машина НЕ має спавнитися (пройшло тільки 1.5 сек з 2.0 потрібних)");
        Assert.AreEqual(10.5f, nextSpawnTime, 0.001f, "Наступний спавн має бути о 10.5 секунди");

        // Test when enough time has passed
        currentTime = 11.0f;
        shouldSpawn = (currentTime - lastSpawnTime) >= spawnInterval;
        Assert.IsTrue(shouldSpawn, "Машина має спавнитися (пройшло 2.5 сек)");
    }

    // ===== ТЕСТ З ВИПАДКОВИМИ ДАНИМИ =====
    [Test]
    public void Random_BuildingRotation_IsWithinValidRange()
    {
        // Arrange
        Random.InitState(42); // Фіксований seed для передбачуваності

        // Act & Assert - тестуємо 10 випадкових поворотів
        for (int i = 0; i < 10; i++)
        {
            float rotation = Random.Range(0f, 360f);
            Assert.GreaterOrEqual(rotation, 0f, $"Поворот #{i} має бути >= 0");
            Assert.Less(rotation, 360f, $"Поворот #{i} має бути < 360");
        }
    }

    // ===== НОВИЙ ТЕСТ: ПЕРЕВІРКА КОЛЬОРІВ =====
    [Test]
    public void Color_Transparency_ModificationWorks()
    {
        Debug.Log("🎨 Тестуємо роботу з кольорами");

        // Arrange
        Color originalColor = Color.red;
        Debug.Log($"Початковий колір: R={originalColor.r}, G={originalColor.g}, B={originalColor.b}, A={originalColor.a}");

        // Act
        Color transparentColor = originalColor;
        transparentColor.a = 0.5f;

        // Assert
        Assert.AreEqual(1.0f, originalColor.r, "Червоний компонент має бути 1.0");
        Assert.AreEqual(0.0f, originalColor.g, "Зелений компонент має бути 0.0");
        Assert.AreEqual(0.0f, originalColor.b, "Синій компонент має бути 0.0");
        Assert.AreEqual(1.0f, originalColor.a, "Альфа має бути 1.0 (непрозорий)");
        Assert.AreEqual(0.5f, transparentColor.a, "Прозорість має бути 0.5");

        Debug.Log($"✅ Прозорий колір: A={transparentColor.a}");
    }

    // ===== ДИСТАНЦІЯ МІЖ ТОЧКАМИ =====
    [Test]
    public void Distance_Calculation_BetweenTwoPoints()
    {
        Debug.Log("📏 Тестуємо обчислення відстані");

        // Arrange
        Vector3 pointA = new Vector3(0, 0, 0);
        Vector3 pointB = new Vector3(3, 4, 0);
        Debug.Log($"Точка A: {pointA}");
        Debug.Log($"Точка B: {pointB}");

        // Act
        float distance = Vector3.Distance(pointA, pointB);
        float expectedDistance = 5.0f; // 3² + 4² = 9 + 16 = 25, √25 = 5

        Debug.Log($"Обчислена відстань: {distance}");
        Debug.Log($"Очікувана відстань: {expectedDistance}");

        // Assert
        Assert.AreEqual(expectedDistance, distance, 0.001f, "Відстань між (0,0,0) та (3,4,0) має бути 5");
        Debug.Log("✅ Відстань обчислена правильно!");
    }

    // ===== РОБОТА З СПИСКАМИ =====
    [Test]
    public void List_Operations_WorkCorrectly()
    {
        Debug.Log("📋 Тестуємо операції зі списками");

        // Arrange
        List<string> buildings = new List<string>();
        Debug.Log($"Початковий розмір списку: {buildings.Count}");

        // Act
        buildings.Add("House");
        buildings.Add("Shop");
        buildings.Add("School");
        Debug.Log($"Додали 3 будинки, розмір: {buildings.Count}");

        // Assert
        Assert.AreEqual(3, buildings.Count, "Список має містити 3 елементи");
        Assert.Contains("House", buildings, "Список має містити 'House'");
        Assert.Contains("Shop", buildings, "Список має містити 'Shop'");
        Assert.Contains("School", buildings, "Список має містити 'School'");

        // Test removal
        buildings.Remove("Shop");
        Assert.AreEqual(2, buildings.Count, "Після видалення має залишитися 2 елементи");
        Assert.IsFalse(buildings.Contains("Shop"), "'Shop' має бути видалений");

        Debug.Log($"✅ Фінальний список: [{string.Join(", ", buildings)}]");
    }

    // ===== ПЕРЕВІРКА МЕНЮ БУДИНКІВ =====
    [Test]
    public void BuildingMenu_Logic_WorksCorrectly()
    {
        Debug.Log("🏠 Тестуємо логіку меню будинків");

        // Arrange - імітуємо конфігурації будинків
        var house = CreateMockBuilding("House", 1, 1, 100);
        var shop = CreateMockBuilding("Shop", 2, 1, 200);
        var school = CreateMockBuilding("School", 3, 2, 500);

        List<MockBuilding> availableBuildings = new List<MockBuilding> { house, shop, school };
        Debug.Log($"Створено {availableBuildings.Count} типів будинків");

        // Act & Assert
        MockBuilding cheapest = FindCheapestBuilding(availableBuildings);
        MockBuilding mostExpensive = FindMostExpensiveBuilding(availableBuildings);
        MockBuilding largest = FindLargestBuilding(availableBuildings);

        Assert.AreEqual("House", cheapest.name, "Найдешевшим має бути House");
        Assert.AreEqual(100, cheapest.cost, "Вартість House має бути 100");

        Assert.AreEqual("School", mostExpensive.name, "Найдорожчим має бути School");
        Assert.AreEqual(500, mostExpensive.cost, "Вартість School має бути 500");

        Assert.AreEqual("School", largest.name, "Найбільшим має бути School");
        Assert.AreEqual(6, largest.size.x * largest.size.y, "Площа School має бути 6 клітинок");

        Debug.Log($"✅ Найдешевший: {cheapest.name} (${cheapest.cost})");
        Debug.Log($"✅ Найдорожчий: {mostExpensive.name} (${mostExpensive.cost})");
        Debug.Log($"✅ Найбільший: {largest.name} ({largest.size.x}x{largest.size.y})");
    }

    // ===== СИМУЛЯЦІЯ ЧАСУ =====
    [Test]
    public void Time_Simulation_WorksCorrectly()
    {
        Debug.Log("⏰ Тестуємо симуляцію часу");

        // Arrange
        float gameSpeed = 2.0f;
        float realDeltaTime = 0.016f; // ~60 FPS
        float currentGameTime = 0f;

        Debug.Log($"Швидкість гри: {gameSpeed}x");
        Debug.Log($"Delta time: {realDeltaTime}s");

        // Act - симулюємо 10 кадрів
        for (int frame = 1; frame <= 10; frame++)
        {
            currentGameTime += realDeltaTime * gameSpeed;

            if (frame == 5)
            {
                Debug.Log($"Час на 5-му кадрі: {currentGameTime:F3}s");
            }
        }

        float expectedTime = 10 * realDeltaTime * gameSpeed; // 10 * 0.016 * 2 = 0.32

        // Assert
        Assert.AreEqual(expectedTime, currentGameTime, 0.001f,
            "Час симуляції має бути 10 кадрів * delta * швидкість");

        Debug.Log($"✅ Фінальний час: {currentGameTime:F3}s (очікувано: {expectedTime:F3}s)");
    }

    // ===== СКЛАДНА ЛОГІКА ТРАФІКУ =====
    [Test]
    public void Traffic_Logic_CalculatesCorrectPath()
    {
        Debug.Log("🚗 Тестуємо логіку трафіку");

        // Arrange - створюємо просту дорожню мережу
        var roadNetwork = new Dictionary<Vector2Int, List<Vector2Int>>();

        Vector2Int roadA = new Vector2Int(0, 0);
        Vector2Int roadB = new Vector2Int(1, 0);
        Vector2Int roadC = new Vector2Int(2, 0);
        Vector2Int roadD = new Vector2Int(2, 1);

        // Створюємо з'єднання доріг
        roadNetwork[roadA] = new List<Vector2Int> { roadB };
        roadNetwork[roadB] = new List<Vector2Int> { roadA, roadC };
        roadNetwork[roadC] = new List<Vector2Int> { roadB, roadD };
        roadNetwork[roadD] = new List<Vector2Int> { roadC };

        Debug.Log($"Створено мережу з {roadNetwork.Count} дорожніх вузлів");

        // Act
        bool canReachDestination = CanReachDestination(roadNetwork, roadA, roadD);
        int pathLength = FindShortestPath(roadNetwork, roadA, roadD);

        // Assert
        Assert.IsTrue(canReachDestination, "Має існувати шлях від A до D");
        Assert.AreEqual(3, pathLength, "Найкоротший шлях має бути 3 кроки: A→B→C→D");

        Debug.Log("✅ Трафік може дістатися від початку до кінця!");
        Debug.Log($"✅ Довжина шляху: {pathLength} кроків");
    }

    // ===== HELPER МЕТОДИ (допоміжні функції) =====

    private bool IsValidPosition(int x, int z, int gridWidth, int gridHeight)
    {
        return x >= 0 && x < gridWidth && z >= 0 && z < gridHeight;
    }

    private string[,] CreateTestGrid(int width, int height)
    {
        var grid = new string[width, height];

        // Заповнюємо порожніми клітинками
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                grid[x, z] = "Empty";
            }
        }

        return grid;
    }

    private bool CanPlaceBuilding(string[,] grid, Vector2Int position, Vector2Int size)
    {
        int gridWidth = grid.GetLength(0);
        int gridHeight = grid.GetLength(1);

        // Перевіряємо всі клітинки які займе будинок
        for (int dx = 0; dx < size.x; dx++)
        {
            for (int dz = 0; dz < size.y; dz++)
            {
                int checkX = position.x + dx;
                int checkZ = position.y + dz;

                // Перевіряємо чи в межах сітки
                if (!IsValidPosition(checkX, checkZ, gridWidth, gridHeight))
                {
                    return false;
                }

                // Перевіряємо чи клітинка вільна
                if (grid[checkX, checkZ] != "Empty")
                {
                    return false;
                }
            }
        }

        return true;
    }

    private Vector3 GridToWorldPosition(Vector2Int gridPos, float tileSize)
    {
        return new Vector3(
            gridPos.x * tileSize,
            0f,
            gridPos.y * tileSize
        );
    }

    // ===== HELPER МЕТОДИ =====

    // Mock клас для імітації будинку
    public class MockBuilding
    {
        public string name;
        public Vector2Int size;
        public int cost;

        public MockBuilding(string name, int width, int height, int cost)
        {
            this.name = name;
            this.size = new Vector2Int(width, height);
            this.cost = cost;
        }
    }

    private MockBuilding CreateMockBuilding(string name, int width, int height, int cost)
    {
        return new MockBuilding(name, width, height, cost);
    }

    private MockBuilding FindCheapestBuilding(List<MockBuilding> buildings)
    {
        MockBuilding cheapest = buildings[0];
        foreach (var building in buildings)
        {
            if (building.cost < cheapest.cost)
                cheapest = building;
        }
        return cheapest;
    }

    private MockBuilding FindMostExpensiveBuilding(List<MockBuilding> buildings)
    {
        MockBuilding mostExpensive = buildings[0];
        foreach (var building in buildings)
        {
            if (building.cost > mostExpensive.cost)
                mostExpensive = building;
        }
        return mostExpensive;
    }

    private MockBuilding FindLargestBuilding(List<MockBuilding> buildings)
    {
        MockBuilding largest = buildings[0];
        int largestArea = largest.size.x * largest.size.y;

        foreach (var building in buildings)
        {
            int area = building.size.x * building.size.y;
            if (area > largestArea)
            {
                largest = building;
                largestArea = area;
            }
        }
        return largest;
    }

    // Простий алгоритм пошуку шляху
    private bool CanReachDestination(Dictionary<Vector2Int, List<Vector2Int>> network, Vector2Int start, Vector2Int end)
    {
        if (start == end) return true;

        var visited = new HashSet<Vector2Int>();
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (network.ContainsKey(current))
            {
                foreach (var neighbor in network[current])
                {
                    if (neighbor == end) return true;

                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        return false;
    }

    private int FindShortestPath(Dictionary<Vector2Int, List<Vector2Int>> network, Vector2Int start, Vector2Int end)
    {
        if (start == end) return 0;

        var visited = new HashSet<Vector2Int>();
        var queue = new Queue<(Vector2Int position, int distance)>();
        queue.Enqueue((start, 0));
        visited.Add(start);

        while (queue.Count > 0)
        {
            var (current, distance) = queue.Dequeue();

            if (network.ContainsKey(current))
            {
                foreach (var neighbor in network[current])
                {
                    if (neighbor == end) return distance + 1;

                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue((neighbor, distance + 1));
                    }
                }
            }
        }

        return -1; // Шлях не знайдено
    }
}