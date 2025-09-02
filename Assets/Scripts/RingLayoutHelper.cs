using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Automatically generates and spaces icons in an oval ring layout.
/// Attach this to a ring container. Configure ring index (1–3), symbol list, and icon prefab.
/// Setting ringTier to 0 disables regeneration but allows live layout adjustments.
/// </summary>
[ExecuteAlways]
public class RingLayoutHelper : MonoBehaviour
{
    [Tooltip("Ring tier: 1 = 9 icons, 2 = 18 icons, 3 = 27 icons. 0 = do not regenerate.")]
    [Range(0, 3)]
    public int ringTier = 0;

    [Tooltip("Ordered list of symbol sprites to populate the ring.")]
    public List<Sprite> symbolSprites = new List<Sprite>();

    [Tooltip("Prefab to use for each icon (must have Image component).")]
    public GameObject iconPrefab;

    [Tooltip("Base radius from center to icons (used for Y axis).")]
    public float radius = 200f;

    [Tooltip("Oval scaling factor for X axis (1 = circle, >1 = wider ellipse).")]
    public float ovalFactor = 1.5f;

    [Tooltip("Offset angle in degrees for the first icon (0 = up).")]
    public float startAngle = 0f;

    public void OnValidate()
    {
        if (ringTier > 0)
        {
            int targetCount = ringTier * 9;

            // Only generate if children count is not correct
            if (transform.childCount != targetCount)
            {
                GenerateRing();
            }
            else
            {
                UpdateLayout();
            }
        }
        else
        {
            UpdateLayout();
        }
    }

    private void GenerateRing()
    {
        int symbolCount = ringTier * 9;
        float angleStep = 360f / symbolCount;

        // Destroy all existing children
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        for (int i = 0; i < symbolCount; i++)
        {
            GameObject icon = Instantiate(iconPrefab, transform);
            icon.name = $"Icon_{i}";

            Image img = icon.GetComponent<Image>();
            if (img != null && symbolSprites.Count > 0)
            {
                img.sprite = symbolSprites[i % symbolSprites.Count];
            }
        }

        UpdateLayout();
    }

    private void UpdateLayout()
    {
        int count = transform.childCount;
        if (count == 0) return;

        float angleStep = 360f / count;
        float radiusX = radius * ovalFactor;
        float radiusY = radius;

        for (int i = 0; i < count; i++)
        {
            Transform icon = transform.GetChild(i);
            float angle = startAngle - i * angleStep;
            float rad = angle * Mathf.Deg2Rad;
            Vector2 pos = new Vector2(Mathf.Sin(rad) * radiusX, Mathf.Cos(rad) * radiusY);

            RectTransform rt = icon.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = pos;
                rt.localRotation = Quaternion.Euler(0, 0, -angle);
            }
        }
    }
} 
