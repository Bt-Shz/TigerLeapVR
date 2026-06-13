using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI; // Needed for the UI Text

public class MemoryGameManager : MonoBehaviour
{
    [Header("Data")]
    public List<ItemData> allItems;
    public List<Transform> spawnPoints;

    [Header("UI Elements")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI scoreText; // If using TextMeshPro, change this to: public TMPro.TextMeshProUGUI scoreText;

    // Tracking the game state
    private List<ItemData> memorizedItems = new List<ItemData>();
    private List<GameObject> activeSpawnedObjects = new List<GameObject>();
    private int currentRound = 0;
    private int score = 0;
    private bool isTestingPhase = false;

    void Start()
    {
        gameOverPanel.SetActive(false);
        StartCoroutine(MemorizePhaseRoutine());
    }

    IEnumerator MemorizePhaseRoutine()
    {
        isTestingPhase = false;

        // Safety check: You need at least 7 items in your project for this logic to work!
        // (4 to memorize + 3 fakes to spawn in the test rounds)
        if (allItems.Count < 7)
        {
            Debug.LogError("You need at least 7 ItemData objects assigned in the inspector!");
            yield break;
        }

        List<ItemData> availableItems = new List<ItemData>(allItems);
        List<Transform> availablePoints = new List<Transform>(spawnPoints);

        // 1. Pick and spawn 4 unique items to memorize
        for (int i = 0; i < 4; i++)
        {
            int randomItemIndex = Random.Range(0, availableItems.Count);
            ItemData selectedItem = availableItems[randomItemIndex];

            memorizedItems.Add(selectedItem); // Save it to test later!
            availableItems.RemoveAt(randomItemIndex);

            int randomPointIndex = Random.Range(0, availablePoints.Count);
            Transform selectedPoint = availablePoints[randomPointIndex];
            availablePoints.RemoveAt(randomPointIndex);

            SpawnItem(selectedItem, selectedPoint);
        }

        // 2. Wait for 3 seconds so the player can memorize them
        yield return new WaitForSeconds(3f);

        // 3. Clear the board and start the testing phase
        ClearBoard();
        StartTestRound();
    }

    void StartTestRound()
    {
        isTestingPhase = true;

        // Check if we finished all 4 rounds
        if (currentRound >= 4)
        {
            EndGame();
            return;
        }

        // The item the player NEEDS to find this round
        ItemData correctItem = memorizedItems[currentRound];

        // Gather 3 "fake" items that are NOT part of the memorized list
        List<ItemData> fakeItemsPool = new List<ItemData>();
        foreach (ItemData item in allItems)
        {
            if (!memorizedItems.Contains(item))
            {
                fakeItemsPool.Add(item);
            }
        }

        // Build the list of 4 items to spawn this round (1 correct, 3 fake)
        List<ItemData> roundItemsToSpawn = new List<ItemData>();
        roundItemsToSpawn.Add(correctItem);

        for (int i = 0; i < 3; i++)
        {
            int randomFakeIndex = Random.Range(0, fakeItemsPool.Count);
            roundItemsToSpawn.Add(fakeItemsPool[randomFakeIndex]);
            fakeItemsPool.RemoveAt(randomFakeIndex);
        }

        // Shuffle spawn points and spawn them
        List<Transform> availablePoints = new List<Transform>(spawnPoints);
        foreach (ItemData itemToSpawn in roundItemsToSpawn)
        {
            int randomPointIndex = Random.Range(0, availablePoints.Count);
            Transform selectedPoint = availablePoints[randomPointIndex];
            availablePoints.RemoveAt(randomPointIndex);

            SpawnItem(itemToSpawn, selectedPoint);
        }
    }

    void SpawnItem(ItemData itemData, Transform spawnPoint)
    {
        GameObject spawnedObj = Instantiate(itemData.itemPrefab, spawnPoint.position, Quaternion.identity);
        activeSpawnedObjects.Add(spawnedObj); // Track it so we can delete it later

        ItemDisplay displayScript = spawnedObj.GetComponent<ItemDisplay>();
        if (displayScript != null)
        {
            displayScript.Setup(itemData, this); // Pass 'this' manager script so the item can talk back!
        }
    }

    void ClearBoard()
    {
        foreach (GameObject obj in activeSpawnedObjects)
        {
            Destroy(obj);
        }
        activeSpawnedObjects.Clear();
    }

    // The ItemDisplay script calls this when clicked
    public void OnItemClicked(ItemData clickedItem)
    {
        if (!isTestingPhase) return; // Don't allow clicking during the memorize phase

        ItemData correctItem = memorizedItems[currentRound];

        if (clickedItem == correctItem)
        {
            score++;
            Debug.Log("Correct!");
        }
        else
        {
            Debug.Log("Wrong!");
        }

        currentRound++;
        ClearBoard();
        StartTestRound(); // Loop to the next round
    }

    void EndGame()
    {
        gameOverPanel.SetActive(true);
        scoreText.text = "Score: " + score + " / 4";
    }
}