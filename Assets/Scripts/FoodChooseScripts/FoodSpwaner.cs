using UnityEngine;

public class FoodSpawnerr : MonoBehaviour
{
    public FoodItemSOo[] foodsToSpawn; // Drag your SOs here
    public Transform[] spawnPoints;   // Drag your 4 Spawn Point Transforms here

    void Start()
    {
        SpawnAllFoods();
    }

    void SpawnAllFoods()
    {
        for (int i = 0; i < foodsToSpawn.Length; i++)
        {
            if (i >= spawnPoints.Length) break; // Safety check

            FoodItemSOo data = foodsToSpawn[i];

            // 1. Instantiate the Prefab from the SO
            GameObject newFood = Instantiate(data.foodPrefab, spawnPoints[i].position, Quaternion.identity);

            // 2. Inject the Data into the Holder script
            FoodItemHolderr holder = newFood.GetComponent<FoodItemHolderr>();
            holder.foodData = data;
        }
    }
}