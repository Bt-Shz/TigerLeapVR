using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CardSpawner : MonoBehaviour
{
    [Header("Grid Size & Position")]
    public int rows = 5;            // Fixed rows
    public int cols = 9;            // Fixed columns
    [Tooltip("Position where the main card grid will spawn")]
    public Vector3 mainGridPosition = Vector3.zero;  // Position to spawn the main grid

    [Header("Card Prefabs")]
    public GameObject[] cardPrefabs; // Inspector mein unique prefabs

    [Header("Player Hand")]
    public Transform playerHandParent;   // Player ke cards ke parent transform
    public float handCardSpacing = 1.5f;  // Player hand cards ke beech ka spacing
    public int cardsInHand = 3;          // Player ko kitne cards dene hain

    [Header("Spacing")]
    public float offsetX = 1.5f;    // X-axis spacing
    public float offsetZ = 2.0f;    // Z-axis spacing

    [Header("Collection Grid")]
    public Transform collectionGridParent;   // Parent object for the collection grid
    public float collectionOffsetX = 1.5f;    // Increased from 1.2f to 1.5f
    public float collectionOffsetZ = 2.0f;    // Increased from 1.5f to 2.0f
    public Vector3 collectionGridPosition;    // Position of the collection grid

    // Track which positions in the collection grid are filled
    private bool[,] collectionGridFilled;     // 4 rows x 3 columns
    private Vector3[,] collectionGridPositions; // Store actual positions

    private List<GameObject> deck = new List<GameObject>();
    private List<GameObject> playerHandCards = new List<GameObject>();
    private List<int> playerHandCardIndices = new List<int>();

    void Start()
    {
        // Initialize collection grid tracking
        collectionGridFilled = new bool[4, 3];
        collectionGridPositions = new Vector3[4, 3];
        
        // Calculate collection grid positions
        InitializeCollectionGrid();
        
        // Existing code
        SelectPlayerHandCards();
        PrepareDeck();
        SpawnPlayerHand();
        SpawnGrid();
        
        // Tell GameManager how many player hand cards we have
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetupGame(cardsInHand);
        }
    }

    // New method to initialize the collection grid positions
    void InitializeCollectionGrid()
    {
        if (collectionGridParent == null)
        {
            Debug.LogError("Collection grid parent not assigned!");
            return;
        }
        
        // Calculate start position for the grid
        // Adjust these calculations to center the grid with the new spacing
        float startX = -((3 - 1) * collectionOffsetX) / 2f;
        float startZ = -((4 - 1) * collectionOffsetZ) / 2f;
        
        // Store all grid positions
        for (int r = 0; r < 4; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                Vector3 localPos = new Vector3(
                    startX + c * collectionOffsetX,
                    0.01f,  // Small offset in Y to prevent z-fighting
                    startZ + r * collectionOffsetZ
                );
                
                // Store the world position
                collectionGridPositions[r, c] = collectionGridParent.TransformPoint(localPos);
            }
        }
    }

    void SelectPlayerHandCards()
    {
        // Player hand ke liye random unique cards select karo
        playerHandCardIndices.Clear();
        
        // Random indices generate karo player hand ke liye
        List<int> availableIndices = Enumerable.Range(0, cardPrefabs.Length).ToList();
        
        for (int i = 0; i < cardsInHand && availableIndices.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, availableIndices.Count);
            playerHandCardIndices.Add(availableIndices[randomIndex]);
            availableIndices.RemoveAt(randomIndex);
        }
    }
    
    void SpawnPlayerHand()
    {
        // Check karo ki playerHandParent assign hai ya nahi
        if (playerHandParent == null)
        {
            Debug.LogError("Player hand parent not assigned!");
            return;
        }
        
        // Make sure player hand has the right tag
        playerHandParent.tag = "PlayerHand";
        
        float startX = -(cardsInHand - 1) * handCardSpacing / 2f;
        
        // Player hand cards ko spawn karo
        for (int i = 0; i < playerHandCardIndices.Count; i++)
        {
            int cardIndex = playerHandCardIndices[i];
            Vector3 pos = new Vector3(startX + i * handCardSpacing, 0.01f, 0);
            GameObject card = Instantiate(cardPrefabs[cardIndex], playerHandParent);
            card.transform.localPosition = pos;
            
            // Remove standard CardController and add PlayerHandCardController
            Destroy(card.GetComponent<CardController>());
            PlayerHandCardController handCard = card.AddComponent<PlayerHandCardController>();
            
            // Set the card type ID for matching
            handCard.SetCardTypeId(cardIndex);
            
            playerHandCards.Add(card);
        }
    }

    void PrepareDeck()
    {
        deck.Clear();
        int totalSlots = rows * cols;
        
        // Player hand se special cards add karo, har ek ke 4 copies
        foreach (int cardIndex in playerHandCardIndices)
        {
            for (int i = 0; i < 4; i++)
            {
                deck.Add(cardPrefabs[cardIndex]);
            }
        }
        
        // Kitne cards abhi tak add huye
        int specialCardsAdded = playerHandCardIndices.Count * 4;
        
        // Baki cards random fill karo
        int remainingCards = totalSlots - specialCardsAdded;
        
        // Available indices create karo, player hand cards ko exclude karke
        List<int> availableIndices = new List<int>();
        for (int i = 0; i < cardPrefabs.Length; i++)
        {
            if (!playerHandCardIndices.Contains(i))
            {
                availableIndices.Add(i);
            }
        }
        
        // Baki cards add karo
        for (int i = 0; i < remainingCards; i++)
        {
            int randomCardIndex = availableIndices[Random.Range(0, availableIndices.Count)];
            deck.Add(cardPrefabs[randomCardIndex]);
        }
        
        // Shuffle the deck
        for (int i = 0; i < deck.Count; i++)
        {
            int rnd = Random.Range(i, deck.Count);
            var tmp = deck[i];
            deck[i] = deck[rnd];
            deck[rnd] = tmp;
        }
    }

    void SpawnGrid()
    {
        // Center offset calculate karo
        float startX = -((cols - 1) * offsetX) / 2f;
        float startZ = -((rows - 1) * offsetZ) / 2f;

        int index = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (index >= deck.Count) return;

                // Calculate local grid position
                Vector3 localPos = new Vector3(
                    startX + c * offsetX,
                    0.01f,
                    startZ + r * offsetZ
                );

                // Add the mainGridPosition offset to position the entire grid
                Vector3 pos = mainGridPosition + localPos;
                
                // Card ko instantiate karo
                GameObject cardObj = Instantiate(deck[index], pos, Quaternion.identity, transform);
                
                // Set card type ID for matching
                CardController controller = cardObj.GetComponent<CardController>();
                if (controller != null)
                {
                    // Find which prefab index this card matches with
                    for (int i = 0; i < cardPrefabs.Length; i++)
                    {
                        // Compare prefab names (might have (Clone) suffix)
                        if (deck[index].name == cardPrefabs[i].name || 
                            deck[index].name + "(Clone)" == cardPrefabs[i].name || 
                            deck[index].name == cardPrefabs[i].name + "(Clone)")
                        {
                            controller.SetCardTypeId(i);
                            break;
                        }
                    }
                }
                
                index++;
            }
        }
    }

    // Add getter methods for collection grid information
    public List<int> GetPlayerHandIndices()
    {
        return playerHandCardIndices;
    }

    public Vector3 GetCollectionGridPosition(int row, int col)
    {
        if (row >= 0 && row < 4 && col >= 0 && col < 3)
        {
            return collectionGridPositions[row, col];
        }
        
        // Fallback position if invalid
        return Vector3.up * 3;
    }
}