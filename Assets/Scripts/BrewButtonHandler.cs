using UnityEngine;
using UnityEngine.UI;

public class BrewButtonHandler : MonoBehaviour
{
    [Header("References")]
    public Button brewButton;
    public EllipticalRingAnimator[] ringAnimators;
    
    void Start()
    {
        if (brewButton == null)
            brewButton = GetComponent<Button>();
            
        if (ringAnimators == null || ringAnimators.Length == 0)
            ringAnimators = FindObjectsOfType<EllipticalRingAnimator>();
            
        if (brewButton != null)
        {
            brewButton.onClick.RemoveAllListeners();
            brewButton.onClick.AddListener(OnBrewButtonClicked);
        }
        else
        {
            Debug.LogError("BrewButtonHandler: No Button component found!");
        }
        
        if (ringAnimators == null || ringAnimators.Length == 0)
        {
            Debug.LogError("BrewButtonHandler: No EllipticalRingAnimator found in scene!");
        }
        else
        {
            Debug.Log($"Found {ringAnimators.Length} ring animators");
        }
    }
    
    void Update()
    {
        // Check if all rings have stopped spinning and re-enable button if needed
        if (brewButton != null && !brewButton.interactable)
        {
            bool allStopped = true;
            foreach (var animator in ringAnimators)
            {
                if (animator != null && animator.isSpinning)
                {
                    allStopped = false;
                    break;
                }
            }
            
            if (allStopped)
            {
                brewButton.interactable = true;
                Debug.Log("All rings stopped - Brew button re-enabled");
            }
        }
    }
    
    public void OnBrewButtonClicked()
    {
        Debug.Log("Brew button clicked - Starting rings!");
        
        if (ringAnimators != null && ringAnimators.Length > 0)
        {
            // Start all rings spinning
            foreach (var animator in ringAnimators)
            {
                if (animator != null)
                {
                    animator.StartSpin();
                }
            }
            
            // Disable the brew button until all rings stop
            if (brewButton != null)
            {
                brewButton.interactable = false;
                Debug.Log("Brew button disabled until rings stop");
            }
        }
        else
        {
            Debug.LogError("No ring animators available!");
        }
    }
}