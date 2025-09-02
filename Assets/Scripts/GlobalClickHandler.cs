using UnityEngine;
using UnityEngine.EventSystems;

public class GlobalClickHandler : MonoBehaviour
{
    [Header("References")]
    public EllipticalRingAnimator[] ringAnimators;
    
    void Start()
    {
        if (ringAnimators == null || ringAnimators.Length == 0)
            ringAnimators = FindObjectsOfType<EllipticalRingAnimator>();
            
        if (ringAnimators == null || ringAnimators.Length == 0)
        {
            Debug.LogError("GlobalClickHandler: No EllipticalRingAnimator found in scene!");
        }
        else
        {
            Debug.Log($"GlobalClickHandler found {ringAnimators.Length} ring animators");
        }
    }
    
    void Update()
    {
        // Check for mouse click or touch input
        bool inputDetected = false;
        
        if (Input.GetMouseButtonDown(0))
        {
            inputDetected = true;
        }
        
        // Also check for touch input on mobile
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            inputDetected = true;
        }
        
        if (inputDetected)
        {
            // Check if the click/touch was on a UI element (like the brew button)
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                // Click was on UI, check if it was specifically NOT the brew button
                GameObject clickedObject = EventSystem.current.currentSelectedGameObject;
                if (clickedObject != null && clickedObject.name != "SpinButton")
                {
                    // Clicked on UI but not the brew button - still trigger slow down
                    HandleGlobalClick();
                }
                // If clicked on brew button, do nothing (let the button handle it)
            }
            else
            {
                // Click was not on UI - trigger slow down
                HandleGlobalClick();
            }
        }
    }
    
    private void HandleGlobalClick()
    {
        if (ringAnimators != null && ringAnimators.Length > 0)
        {
            // Check if any rings are currently spinning and not already slowing
            bool anyActivelySpinning = false;
            foreach (var animator in ringAnimators)
            {
                if (animator != null && animator.isSpinning && !animator.isSlowing)
                {
                    anyActivelySpinning = true;
                    break;
                }
            }
            
            if (anyActivelySpinning)
            {
                Debug.Log("Global click detected - Slowing down rings...");
                foreach (var animator in ringAnimators)
                {
                    if (animator != null && animator.isSpinning && !animator.isSlowing)
                    {
                        animator.BeginStop();
                    }
                }
            }
        }
    }
}