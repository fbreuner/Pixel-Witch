using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CauldronBrewingSystem : MonoBehaviour
{
    [Header("UI References")]
    public Button brewButton;
    public EllipticalRingAnimator[] ringAnimators;
    public CanvasGroup[] ringCanvasGroups;
    
    [Header("Timing Settings")]
    public float decelerationTime = 2f;
    public float endDelayTime = 0.5f;
    
    [Header("Rewards")]
    public int threeMatchReward = 15;
    public int twoMatchReward = 5;
    
    // Brewing state
    private bool isBrewingActive = false;
    private int currentClickCount = 0;
    private string[] predeterminedWinningSymbols = new string[3];
    private bool[] ringsStopped = new bool[3];
    private Coroutine endDelayCoroutine;
    
    void Start()
    {
        SetupInitialState();
        UpdateBrewButtonState();
    }
    
    void Update()
    {
        // Handle global clicks during brewing
        if (isBrewingActive && Input.GetMouseButtonDown(0))
        {
            HandleBrewingClick();
        }
        
        // Also handle touch input for mobile
        if (isBrewingActive && Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            HandleBrewingClick();
        }
        
        // Update brew button state based on ingredients
        if (!isBrewingActive)
        {
            UpdateBrewButtonState();
        }
    }
    
    private void SetupInitialState()
    {
        // Find components if not assigned
        if (brewButton == null)
            brewButton = GameObject.Find("SpinButton")?.GetComponent<Button>();
            
        if (ringAnimators == null || ringAnimators.Length == 0)
            ringAnimators = FindObjectsOfType<EllipticalRingAnimator>();
            
        if (ringCanvasGroups == null || ringCanvasGroups.Length == 0)
        {
            ringCanvasGroups = new CanvasGroup[3];
            for (int i = 0; i < ringAnimators.Length && i < 3; i++)
            {
                ringCanvasGroups[i] = ringAnimators[i].GetComponent<CanvasGroup>();
            }
        }
        
        // Set up brew button
        if (brewButton != null)
        {
            brewButton.onClick.RemoveAllListeners();
            brewButton.onClick.AddListener(StartBrewing);
        }
        
        // Hide rings initially
        HideRings();
    }
    
    private void UpdateBrewButtonState()
    {
        if (brewButton != null && GameManager.Instance != null)
        {
            brewButton.interactable = GameManager.Instance.ingredients > 0 && !isBrewingActive;
        }
    }
    
    public void StartBrewing()
    {
        if (isBrewingActive || GameManager.Instance == null) return;
        
        Debug.Log("Starting brewing minigame");
        
        // Deduct ingredient
        if (!GameManager.Instance.SpendIngredients(1))
        {
            Debug.LogWarning("Not enough ingredients to brew");
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
        StartSpinningRings();
        
        // Predetermine winning symbols
        DetermineWinningSymbols();
    }
    
    private void ShowRings()
    {
        foreach (var canvasGroup in ringCanvasGroups)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }
    }
    
    private void HideRings()
    {
        foreach (var canvasGroup in ringCanvasGroups)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }
    }
    
    private void StartSpinningRings()
    {
        foreach (var ringAnimator in ringAnimators)
        {
            if (ringAnimator != null)
            {
                ringAnimator.StartSpin();
            }
        }
    }
    
    private void DetermineWinningSymbols()
    {
        // Wait a frame to ensure rings are spinning
        StartCoroutine(DetermineWinningSymbolsCoroutine());
    }
    
    private IEnumerator DetermineWinningSymbolsCoroutine()
    {
        yield return new WaitForEndOfFrame();
        
        for (int i = 0; i < ringAnimators.Length && i < 3; i++)
        {
            var ringAnimator = ringAnimators[i];
            if (ringAnimator != null && ringAnimator.icons.Count > 0)
            {
                // Choose a random symbol from the ring (since it's spinning randomly)
                int randomIndex = Random.Range(0, ringAnimator.icons.Count);
                predeterminedWinningSymbols[i] = ringAnimator.GetSymbolAtIndex(randomIndex);
                Debug.Log($"Ring {i + 1} predetermined winner: {predeterminedWinningSymbols[i]} (Index {randomIndex})");
            }
        }
    }
    
    private void HandleBrewingClick()
    {
        if (currentClickCount >= 3) return;
        
        int ringIndex = currentClickCount;
        
        if (ringIndex < ringAnimators.Length && !ringsStopped[ringIndex])
        {
            Debug.Log($"Starting deceleration for Ring {ringIndex + 1}");
            StartCoroutine(DecelerateRingToWinningPosition(ringIndex));
            ringsStopped[ringIndex] = true;
            currentClickCount++;
            
            // Check if all rings are now stopping
            if (currentClickCount >= 3)
            {
                StartCoroutine(WaitForAllRingsToStop());
            }
        }
    }
    
    private IEnumerator DecelerateRingToWinningPosition(int ringIndex)
    {
        var ringAnimator = ringAnimators[ringIndex];
        if (ringAnimator == null) yield break;
        
        string targetSymbol = predeterminedWinningSymbols[ringIndex];
        
        // Find which icon has the target symbol
        int targetIconIndex = -1;
        for (int i = 0; i < ringAnimator.icons.Count; i++)
        {
            if (ringAnimator.GetSymbolAtIndex(i) == targetSymbol)
            {
                targetIconIndex = i;
                break;
            }
        }
        
        if (targetIconIndex == -1)
        {
            Debug.LogError($"Could not find target symbol {targetSymbol} in ring {ringIndex}");
            yield break;
        }
        
        // Calculate how much the ring needs to rotate to get the target icon to winning position (angle 0)
        float currentAngle = ringAnimator.icons[targetIconIndex].angle;
        float targetAngle = 0f; // Winning position
        
        // Calculate the shortest rotation distance
        float angleDifference = Mathf.DeltaAngle(currentAngle, targetAngle);
        
        // Add extra rotations to make it look natural (at least 2 full rotations)
        float extraRotations = 720f; // 2 full rotations
        float totalRotation = angleDifference + extraRotations;
        
        // Ensure we rotate in the direction the ring is spinning
        if (totalRotation < 0) totalRotation += 360f;
        
        Debug.Log($"Ring {ringIndex + 1}: Current angle: {currentAngle}, Target: {targetAngle}, Total rotation: {totalRotation}");
        
        // Calculate deceleration to complete rotation in exactly 2 seconds
        float startTime = Time.time;
        float startSpeed = ringAnimator.currentSpeed;
        float rotationPerSecond = totalRotation / decelerationTime;
        
        float rotatedSoFar = 0f;
        
        while (Time.time - startTime < decelerationTime)
        {
            float elapsed = Time.time - startTime;
            float progress = elapsed / decelerationTime;
            
            // Use smooth deceleration curve
            float speedMultiplier = 1f - (progress * progress);
            float currentSpeed = rotationPerSecond * speedMultiplier;
            
            // Update rotation
            float deltaRotation = currentSpeed * Time.deltaTime;
            rotatedSoFar += deltaRotation;
            
            // Update all icons in the ring
            foreach (var icon in ringAnimator.icons)
            {
                icon.angle += deltaRotation;
                icon.angle %= 360f;
                
                float rad = icon.angle * Mathf.Deg2Rad;
                float x = ringAnimator.radiusX * Mathf.Cos(rad);
                float y = ringAnimator.radiusY * Mathf.Sin(rad);
                icon.iconTransform.anchoredPosition = new Vector2(x, y);
            }
            
            yield return null;
        }
        
        // Ensure exact final position - set the target icon to exactly 0 degrees
        ringAnimator.icons[targetIconIndex].angle = 0f;
        
        // Update final positions of all icons
        foreach (var icon in ringAnimator.icons)
        {
            float rad = icon.angle * Mathf.Deg2Rad;
            float x = ringAnimator.radiusX * Mathf.Cos(rad);
            float y = ringAnimator.radiusY * Mathf.Sin(rad);
            icon.iconTransform.anchoredPosition = new Vector2(x, y);
        }
        
        // Stop the ring
        ringAnimator.isSpinning = false;
        ringAnimator.isSlowing = false;
        ringAnimator.currentSpeed = 0f;
        
        Debug.Log($"Ring {ringIndex + 1} stopped with {targetSymbol} at winning position");
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
        Debug.Log("Evaluating brewing results...");
        
        // Get the actual symbols at winning positions
        string[] finalSymbols = new string[3];
        for (int i = 0; i < ringAnimators.Length && i < 3; i++)
        {
            finalSymbols[i] = ringAnimators[i].GetSymbolAtWinningPosition();
            Debug.Log($"Ring {i + 1} final symbol: {finalSymbols[i]}");
        }
        
        // Calculate reward
        int reward = CalculateReward(finalSymbols);
        
        if (reward > 0 && GameManager.Instance != null)
        {
            GameManager.Instance.AddMagic(reward);
            Debug.Log($"Player wins {reward} magic points!");
        }
        else
        {
            Debug.Log("No winning combination");
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
        Debug.Log("Ending brewing session");
        
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
}