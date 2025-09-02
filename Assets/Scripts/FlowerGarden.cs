using UnityEngine;
using UnityEngine.UI;
using TMPro;



public class FlowerGarden : MonoBehaviour
{
    [Header("Production Settings")]
    public float productionInterval = 10f;  // seconds per ingredient
    public int storageCapacity = 1;
    public int storedIngredients = 0;

    [Header("Upgrade Settings")]
    public int level = 1;
    public int upgradeCost = 0;

    [Header("UI Elements")]
    public Button collectButton;
    public Button upgradeButton;
    public TextMeshProUGUI storedText;
    public TextMeshProUGUI upgradeCostText;
    public TextMeshProUGUI currentLevel;
    public GameObject starExplosionPrefab;
    public RectTransform spawnAnchor;
    
    [Header("Visuals")]
    public Image flowerImage;
    public Sprite[] flowerLevelSprites;

    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= productionInterval && storedIngredients < storageCapacity)
        {
            storedIngredients += 1;
            timer = 0f;
            UpdateStoredText();
        }

        collectButton.interactable = (storedIngredients > 0);
        upgradeButton.interactable = (GameManager.Instance.magic >= upgradeCost);
    }

    public void CollectIngredients()
    {
        if (storedIngredients == 0)
        {
            Debug.Log("Upgrade your flower garden to produce ingredients faster.");
            return;
        }
        GameManager.Instance.AddIngredients(storedIngredients);
        storedIngredients = 0;
        UpdateStoredText();
    }

    public void UpgradeFlower()
    {

        if (!GameManager.Instance.SpendMagic(upgradeCost)) return;

        level += 1;

        // Upgrade logic
        storageCapacity += 1;
        productionInterval = Mathf.Max(4f, productionInterval - 1f);
        upgradeCost += level * level;  // fixed: was level^2 which is XOR

        // Update sprite if level is Fibonacci
        if (IsFibonacci(level))
        {
            int spriteIndex = CountFibonacciNumbersUpTo(level) - 1;
            if (spriteIndex >= 0 && spriteIndex < flowerLevelSprites.Length)
            {
                flowerImage.sprite = flowerLevelSprites[spriteIndex];
            }
        }
        currentLevel.text = $"Flower Garden Level: {level}";
        UpdateStoredText();
        if (starExplosionPrefab != null && spawnAnchor != null)
           {
            Debug.Log("Trying to spawn star explosion");
            GameObject fx = Instantiate(starExplosionPrefab, spawnAnchor.position, Quaternion.identity, spawnAnchor.parent);
            ParticleSystem ps = fx.GetComponentInChildren<ParticleSystem>();
            if (ps != null) ps.Play();
            Destroy(fx, 2f); // Cleanup after effect is done
        }
    }

    void UpdateStoredText()
    {
        storedText.text = $"Stored: {storedIngredients}/{storageCapacity}";
        upgradeCostText.text = $"Next Upgrade: {upgradeCost} Magic";
        
    }

    // ==== Fibonacci Helpers ====

    private bool IsFibonacci(int n)
    {
        return IsPerfectSquare(5 * n * n + 4) || IsPerfectSquare(5 * n * n - 4);
    }

    private bool IsPerfectSquare(int x)
    {
        int s = (int)Mathf.Sqrt(x);
        return s * s == x;
    }

    private int CountFibonacciNumbersUpTo(int max)
    {
        int count = 0;
        int a = 1, b = 1;
        while (a <= max)
        {
            count++;
            int temp = a + b;
            a = b;
            b = temp;
        }
        return count;
    }
}
