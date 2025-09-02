using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// Singleton that manages global game state including currencies, buffs, and brewing orchestration.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // Core currency data
    public int ingredients = 0;
    public int magic = 0;

    // UI elements to display current resources
    public TextMeshProUGUI ingredientsText;
    public TextMeshProUGUI magicText;

    // Buff tracking for cauldron rewards
    public int doubleRewardSpinsRemaining = 0;
    public int quadrupleRewardSpinsRemaining = 0;

    // Scene to return to after a minigame
    public string returnScene = "CauldronScene";

    // Last quit time for persistent real-time logic
    private DateTime lastQuitTime;
    public DateTime LastQuitTime => lastQuitTime;
    
    [Header("Brewing System")]
    public Button brewButton;
    public EllipticalRingAnimator[] rings = new EllipticalRingAnimator[3]; // Ring1, Ring2, Ring3
    
    [Header("Brewing Settings")]
    public float decelerationTime = 2f;
    public float endDelayTime = 0.5f;
    public int threeMatchReward = 15;
    public int twoMatchReward = 5;
    
    // Brewing state
    private bool isBrewingActive = false;
    private int currentClickCount = 0;
    private string[] predeterminedWinningSymbols = new string[3];
    private bool[] ringsStopped = new bool[3];

    void Awake()
    {
        // Set up singleton and ensure it persists across scenes
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // persists across scenes
        }
        else
        {
            Destroy(gameObject); // avoid duplicate singletons
            return;
        }
    }

    void Start()
    {
        // Start the player with some initial ingredients
        ingredients = 3;
        UpdateUI();
        SetupBrewingSystem();
    }
    
    void Update()
    {
        // Handle global clicks during brewing
        if (isBrewingActive && (Input.GetMouseButtonDown(0) || 
            (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)))
        {
            HandleBrewingClick();
        }
        
        // Update brew button state based on ingredients
        if (!isBrewingActive)
        {
            UpdateBrewButtonState();
        }
    }
    
    private void SetupBrewingSystem()
    {
        // Set up brew button
        if (brewButton != null)
        {
            brewButton.onClick.RemoveAllListeners();
            brewButton.onClick.AddListener(StartBrewing);
        }
        
        // Find rings if not assigned
        if (rings[0] == null || rings[1] == null || rings[2] == null)
        {
            EllipticalRingAnimator[] foundRings = FindObjectsOfType<EllipticalRingAnimator>();
            if (foundRings.Length >= 3)
            {
                System.Array.Copy(foundRings, rings, 3);
                Debug.Log("GameManager: Auto-assigned rings");
            }
        }
        
        // Hide rings initially
        HideRings();
        UpdateBrewButtonState();
    }
    
    private void UpdateBrewButtonState()
    {
        if (brewButton != null)
        {
            brewButton.interactable = ingredients > 0 && !isBrewingActive;
        }
    }
    
    public void StartBrewing()
    {
        if (isBrewingActive) return;
        
        Debug.Log("GameManager: Starting brewing minigame");
        
        // Validate and deduct ingredient
        if (!SpendIngredients(1))
        {
            Debug.LogWarning("GameManager: Not enough ingredients to brew");
            return;
        }
        
        // Enter brewing state
        isBrewingActive = true;
        currentClickCount = 0;
        ringsStopped = new bool[3];
        
        // Disable all UI
        SetAllButtonsInteractable(false);
        
        // Show and start spinning rings
        ShowRings();
        StartSpinningAllRings();
        
        // Predetermine winning symbols
        StartCoroutine(DetermineWinningSymbols());
    }
    
    private void ShowRings()
    {
        foreach (var ring in rings)
        {
            if (ring != null)
            {
                CanvasGroup canvasGroup = ring.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                }
            }
        }
    }
    
    private void HideRings()
    {
        foreach (var ring in rings)
        {
            if (ring != null)
            {
                CanvasGroup canvasGroup = ring.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                    canvasGroup.interactable = false;
                    canvasGroup.blocksRaycasts = false;
                }
            }
        }
    }
    
    private void StartSpinningAllRings()
    {
        foreach (var ring in rings)
        {
            if (ring != null)
            {
                ring.StartSpin();
            }
        }
    }
    
    private IEnumerator DetermineWinningSymbols()
    {
        yield return new WaitForEndOfFrame();
        
        for (int i = 0; i < rings.Length; i++)
        {
            var ring = rings[i];
            if (ring != null && ring.icons.Count > 0)
            {
                // Choose a random symbol from the ring
                int randomIndex = UnityEngine.Random.Range(0, ring.icons.Count);
                predeterminedWinningSymbols[i] = ring.GetSymbolAtIndex(randomIndex);
                Debug.Log($"GameManager: Ring {i + 1} predetermined winner: {predeterminedWinningSymbols[i]} (Index {randomIndex})");
            }
        }
    }
    
    private void HandleBrewingClick()
    {
        if (currentClickCount >= 3) return;
        
        int ringIndex = currentClickCount;
        
        if (ringIndex < rings.Length && !ringsStopped[ringIndex] && rings[ringIndex] != null)
        {
            Debug.Log($"GameManager: Starting deceleration for Ring {ringIndex + 1}");
            rings[ringIndex].DecelerateToWinningSymbol(predeterminedWinningSymbols[ringIndex], decelerationTime);
            ringsStopped[ringIndex] = true;
            currentClickCount++;
            
            // Check if all rings are now stopping
            if (currentClickCount >= 3)
            {
                StartCoroutine(WaitForAllRingsToStop());
            }
        }
    }
    
    private IEnumerator WaitForAllRingsToStop()
    {
        // Wait for all rings to finish decelerating
        yield return new WaitForSeconds(decelerationTime);
        
        // Evaluate results
        EvaluateBrewingResults();
        
        // Wait additional delay before re-enabling UI
        yield return new WaitForSeconds(endDelayTime);
        
        // End brewing session
        EndBrewing();
    }
    
    private void EvaluateBrewingResults()
    {
        Debug.Log("GameManager: Evaluating brewing results...");
        
        // Get the actual symbols at winning positions
        string[] finalSymbols = new string[3];
        for (int i = 0; i < rings.Length; i++)
        {
            if (rings[i] != null)
            {
                finalSymbols[i] = rings[i].GetSymbolAtWinningPosition();
                Debug.Log($"GameManager: Ring {i + 1} final symbol: {finalSymbols[i]}");
            }
        }
        
        // Calculate reward
        int reward = CalculateReward(finalSymbols);
        
        if (reward > 0)
        {
            AddMagic(reward);
            Debug.Log($"GameManager: Player wins {reward} magic points!");
        }
        else
        {
            Debug.Log("GameManager: No winning combination");
        }
    }
    
    private int CalculateReward(string[] symbols)
    {
        // Check for three matching symbols
        if (symbols[0] == symbols[1] && symbols[1] == symbols[2])
        {
            return threeMatchReward;
        }
        
        // Check for two consecutive matching symbols
        if (symbols[0] == symbols[1] || symbols[1] == symbols[2])
        {
            return twoMatchReward;
        }
        
        return 0;
    }
    
    private void EndBrewing()
    {
        Debug.Log("GameManager: Ending brewing session");
        
        isBrewingActive = false;
        currentClickCount = 0;
        
        // Hide rings
        HideRings();
        
        // Re-enable UI
        SetAllButtonsInteractable(true);
        UpdateBrewButtonState();
    }
    
    private void SetAllButtonsInteractable(bool interactable)
    {
        Button[] allButtons = FindObjectsOfType<Button>();
        foreach (var button in allButtons)
        {
            if (interactable)
            {
                // Only re-enable brew button if player has ingredients
                if (button == brewButton)
                {
                    UpdateBrewButtonState();
                }
                else
                {
                    button.interactable = true;
                }
            }
            else
            {
                button.interactable = false;
            }
        }
    }

    // Adds a specified amount of ingredients and updates UI
    public void AddIngredients(int amount)
    {
        ingredients += amount;
        UpdateUI();
    }

    // Tries to spend a specified amount of ingredients
    public bool SpendIngredients(int amount)
    {
        if (ingredients >= amount)
        {
            ingredients -= amount;
            UpdateUI();
            return true;
        }
        return false;
    }

    // Adds a specified amount of magic and updates UI
    public void AddMagic(int amount)
    {
        magic += amount;
        UpdateUI();
    }

    // Tries to spend a specified amount of magic
    public bool SpendMagic(int amount)
    {
        if (magic >= amount)
        {
            magic -= amount;
            UpdateUI();
            return true;
        }
        return false;
    }

    // Updates on-screen display of current resources
    void UpdateUI()
    {
        if (ingredientsText != null)
            ingredientsText.text = $"Ingredients: {ingredients}";

        if (magicText != null)
            magicText.text = $"Magic: {magic}";
    }

    // Records the time the app was closed or suspended
    void OnApplicationQuit()
    {
        lastQuitTime = DateTime.Now;
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            lastQuitTime = DateTime.Now;
        }
    }
}
