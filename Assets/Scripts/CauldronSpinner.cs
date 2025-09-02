using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives motion for a ring of icons rotating along an elliptical track.
/// Each icon maintains its angle and is moved independently in Update.
/// Enhanced with precision stopping capabilities for brewing minigame.
/// </summary>
public class EllipticalRingAnimator : MonoBehaviour
{
    [Header("Ellipse Shape")]
    public float radiusX = 200f; // Horizontal radius (a)
    public float radiusY = 100f; // Vertical radius (b)

    [Header("Motion Settings")]
    public float initialSpinSpeed = 200f; // Degrees per second
    public float decelerationRate = 2f;   // Speed reduction per second when stopping

    [Header("Runtime Control")]
    public bool isSpinning = false;
    public bool isSlowing = false;

    [HideInInspector] public List<IconEntry> icons = new List<IconEntry>();
    [HideInInspector] public float currentSpeed;
    
    // Precision stopping state
    private Coroutine precisionStopCoroutine;
    
    [System.Serializable]
    public class IconEntry
    {
        public RectTransform iconTransform;
        public float angle; // In degrees
        public string symbolName; // Symbol identifier
    }

    void Start()
    {
        InitializeRing();
    }
    
    void Update()
    {
        // Handle normal ring spinning
        if (isSpinning)
        {
            if (isSlowing)
            {
                currentSpeed = Mathf.Lerp(currentSpeed, 0f, Time.deltaTime * decelerationRate);
                if (currentSpeed < 10f)
                {
                    isSpinning = false;
                    isSlowing = false;
                }
            }

            UpdateIconPositions();
        }
    }
    
    private void InitializeRing()
    {
        icons.Clear();
        foreach (Transform child in transform)
        {
            Image img = child.GetComponent<Image>();
            if (img == null) continue;

            IconEntry entry = new IconEntry();
            entry.iconTransform = child.GetComponent<RectTransform>();
            entry.symbolName = img.sprite != null ? img.sprite.name : "";
            icons.Add(entry);
        }

        // Evenly space icons along the ellipse
        for (int i = 0; i < icons.Count; i++)
        {
            icons[i].angle = 360f * i / icons.Count;
        }
        
        // Initial position update
        UpdateIconPositions();
    }
    
    private void UpdateIconPositions()
    {
        foreach (var icon in icons)
        {
            icon.angle += currentSpeed * Time.deltaTime;
            icon.angle %= 360f;

            float rad = icon.angle * Mathf.Deg2Rad;
            float x = radiusX * Mathf.Cos(rad);
            float y = radiusY * Mathf.Sin(rad);
            icon.iconTransform.anchoredPosition = new Vector2(x, y);
        }
    }

    // Public methods for controlling ring behavior
    public void StartSpin()
    {
        isSpinning = true;
        isSlowing = false;
        currentSpeed = initialSpinSpeed;
        
        // Stop any precision stopping in progress
        if (precisionStopCoroutine != null)
        {
            StopCoroutine(precisionStopCoroutine);
            precisionStopCoroutine = null;
        }
    }

    public void BeginStop()
    {
        isSlowing = true;
    }
    
    public void DecelerateToWinningSymbol(string targetSymbol, float duration)
    {
        if (precisionStopCoroutine != null)
        {
            StopCoroutine(precisionStopCoroutine);
        }
        precisionStopCoroutine = StartCoroutine(PrecisionStopCoroutine(targetSymbol, duration));
    }
    
    private IEnumerator PrecisionStopCoroutine(string targetSymbol, float duration)
    {
        // Find which icon has the target symbol
        int targetIconIndex = -1;
        for (int i = 0; i < icons.Count; i++)
        {
            if (GetSymbolAtIndex(i) == targetSymbol)
            {
                targetIconIndex = i;
                break;
            }
        }
        
        if (targetIconIndex == -1)
        {
            Debug.LogError($"EllipticalRingAnimator: Could not find target symbol {targetSymbol}");
            yield break;
        }
        
        // Calculate how much the ring needs to rotate to get the target icon to winning position (angle 0)
        float currentAngle = icons[targetIconIndex].angle;
        float targetAngle = 0f; // Winning position
        
        // Calculate the shortest rotation distance
        float angleDifference = Mathf.DeltaAngle(currentAngle, targetAngle);
        
        // Add extra rotations to make it look natural (at least 2 full rotations)
        float extraRotations = 720f; // 2 full rotations
        float totalRotation = angleDifference + extraRotations;
        
        // Ensure we rotate in the direction the ring is spinning
        if (totalRotation < 0) totalRotation += 360f;
        
        Debug.Log($"EllipticalRingAnimator: Current angle: {currentAngle}, Target: {targetAngle}, Total rotation: {totalRotation}");
        
        // Calculate deceleration to complete rotation in exactly the specified duration
        float startTime = Time.time;
        float rotationPerSecond = totalRotation / duration;
        
        while (Time.time - startTime < duration)
        {
            float elapsed = Time.time - startTime;
            float progress = elapsed / duration;
            
            // Use smooth deceleration curve
            float speedMultiplier = 1f - (progress * progress);
            float currentSpeedForFrame = rotationPerSecond * speedMultiplier;
            
            // Update rotation
            float deltaRotation = currentSpeedForFrame * Time.deltaTime;
            
            // Update all icons in the ring
            foreach (var icon in icons)
            {
                icon.angle += deltaRotation;
                icon.angle %= 360f;
                
                float rad = icon.angle * Mathf.Deg2Rad;
                float x = radiusX * Mathf.Cos(rad);
                float y = radiusY * Mathf.Sin(rad);
                icon.iconTransform.anchoredPosition = new Vector2(x, y);
            }
            
            yield return null;
        }
        
        // Ensure exact final position - set the target icon to exactly 0 degrees
        icons[targetIconIndex].angle = 0f;
        
        // Update final positions of all icons
        foreach (var icon in icons)
        {
            float rad = icon.angle * Mathf.Deg2Rad;
            float x = radiusX * Mathf.Cos(rad);
            float y = radiusY * Mathf.Sin(rad);
            icon.iconTransform.anchoredPosition = new Vector2(x, y);
        }
        
        // Stop the ring
        isSpinning = false;
        isSlowing = false;
        currentSpeed = 0f;
        precisionStopCoroutine = null;
        
        Debug.Log($"EllipticalRingAnimator: Stopped with {targetSymbol} at winning position");
    }

    // Public methods for querying ring state
    public int GetClosestIconIndex(Vector2 targetPosition)
    {
        float minDist = float.MaxValue;
        int closest = -1;
        for (int i = 0; i < icons.Count; i++)
        {
            float dist = Vector2.Distance(icons[i].iconTransform.anchoredPosition, targetPosition);
            if (dist < minDist)
            {
                minDist = dist;
                closest = i;
            }
        }
        return closest;
    }

    public string GetSymbolAtIndex(int index)
    {
        if (index >= 0 && index < icons.Count)
        {
            return icons[index].symbolName;
        }
        return "";
    }
    
    public string GetSymbolAtWinningPosition()
    {
        // The winning position is at angle 0 (top of the ellipse)
        Vector2 winningPosition = new Vector2(0, radiusY);
        int closestIndex = GetClosestIconIndex(winningPosition);
        return GetSymbolAtIndex(closestIndex);
    }
    
    public int GetWinningIconIndex()
    {
        // The winning position is at angle 0 (top of the ellipse)  
        Vector2 winningPosition = new Vector2(0, radiusY);
        return GetClosestIconIndex(winningPosition);
    }
}
