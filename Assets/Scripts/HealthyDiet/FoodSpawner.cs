using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodSpawner : MonoBehaviour
{
    public static FoodSpawner Instance;

    [Header("Food Lists")]
    public List<FoodItemSO> healthyFoods;
    public List<FoodItemSO> unhealthyFoods;

    [Header("Conveyor Spawn Locations")]
    [Tooltip("Index 0 is Start Point, Last Index is End Point")]
    public Transform[] spawnPoints;

    [Header("Conveyor Settings")]
    public float spawnDelay = 2.5f;   // Kitni der baad naya food aayega
    public float moveSpeed = 2.5f;    // Food ki aage badhne ki speed
    public int poolSize = 15;         // Total items pool me

    // Object Pool ke liye Queue
    private Queue<GameObject> foodPool = new Queue<GameObject>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        InitializePool();
        StartCoroutine(SpawnFoodRoutine());
    }

    void InitializePool()
    {
        // Dono lists ko mix kar rahe hain
        List<FoodItemSO> allFoods = new List<FoodItemSO>();
        allFoods.AddRange(healthyFoods);
        allFoods.AddRange(unhealthyFoods);

        for (int i = 0; i < poolSize; i++)
        {
            // Randomly select food
            FoodItemSO randomFood = allFoods[Random.Range(0, allFoods.Count)];
            GameObject newFood = Instantiate(randomFood.foodPrefab);

            newFood.SetActive(false); // Shuru me invisible rakhenge

            FoodItemHolder holder = newFood.GetComponent<FoodItemHolder>();
            if (holder != null)
            {
                holder.foodData = randomFood;
                newFood.name = randomFood.foodName;
            }

            // Pool me daal do
            foodPool.Enqueue(newFood);
        }
    }

    IEnumerator SpawnFoodRoutine()
    {
        while (true)
        {
            if (foodPool.Count > 0)
            {
                // Pool se nikal kar First Point par rakho
                GameObject food = foodPool.Dequeue();
                food.transform.position = spawnPoints[0].position;
                food.SetActive(true); // Visible karo
            }

            // Wait karo naye food ke liye
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    // Is function ko tab call karenge jab food last point tak pahunch jaye ya plate par rakh diya jaye
    public void ReturnToPool(GameObject food)
    {
        food.SetActive(false);
        foodPool.Enqueue(food); // Wapas queue me daal diya (Next turn ke liye)
    }

    public void RefreshFoods()
    {
        // Conveyor belt system me manual refresh ki zaroorat kam hoti hai, 
        // par aap chaho toh yahan screen ke saare active objects ko pool me return karwa sakte ho.
    }
}