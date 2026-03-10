using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FoodManager : MonoBehaviour
{
    // Storage for the instantiated food objects grouped by their unique ID
    private Dictionary<string, Transform> foodTypeParents = new Dictionary<string, Transform>();
    // Tracks the last attachment point (socket) for each food item to stack ingredients correctly
    private Dictionary<string, Transform> lastSocketPoints = new Dictionary<string, Transform>();

    [SerializeField] ParticleSystem confettiEffect;

    [Header("UI References")]
    [SerializeField] GameObject[] layerButtonGroups; // UI groups for each step (Bread, Meat, etc.)
    [SerializeField] TextMeshProUGUI missingFoodLayerWarningText, finishOrderWarningText, totalPriceText;
    [SerializeField] Button nextStepButton, backButton, removeLastItemButton;
    [SerializeField] Button[] removeCategoryButton, addCategoryButton;
    public GameObject panel; // The confirmation/success popup

    [Header("Setup")]
    [SerializeField] Transform defaultSpawnPoint; // Initial position for new food bases

    // Internal state tracking
    private string activeCategory = "";     // e.g., "Burger"
    private string activeGroupId = "";      // e.g., "Burger_1"
    private int orderItemCounter = 0;       // Incrementing ID for multiple items in one order
    private int currentLayerTarget = 0;     // Current construction step (Layer 1, 2...)
    private int maxLayersActiveCategory = 0;// How many layers the current food needs
    private float totalPrice = 0f;          // Cumulative price of the whole cart

    private void Start()
    {
        ResetVariables();
        UpdatePriceUI();
        UpdateUIButtons();
    }

    // Resets UI and local counters to a clean state
    private void ResetVariables()
    {
        currentLayerTarget = 0;
        totalPrice = 0f;
        orderItemCounter = 0;
        if (finishOrderWarningText != null) finishOrderWarningText.gameObject.SetActive(false);
        if (nextStepButton != null) nextStepButton.gameObject.SetActive(false);
        if (backButton != null) backButton.gameObject.SetActive(false);
        if (removeLastItemButton != null) removeLastItemButton.gameObject.SetActive(false);
    }

    // Called when clicking a main food button (e.g., "Burger")
    public void SelectCategory(FoodData categoryData)
    {
        // STATE CHECK: Ignore click if currently confirming the whole order or already building something
        if (GameManager.gameManager.currentGameState == GameManager.GameState.confirmingOrdering ||
            GameManager.gameManager.currentGameState == GameManager.GameState.choosingIngredients)
        {
            Debug.LogWarning("Finish current assembly or close the panel first!");
            return;
        }

        if (categoryData == null) return;

        // Setup the new item data
        orderItemCounter++;
        activeCategory = categoryData.categoryName;
        activeGroupId = activeCategory + "_" + orderItemCounter;

        totalPrice += categoryData.price;
        UpdatePriceUI();

        maxLayersActiveCategory = categoryData.maxLayersInCategory;
        currentLayerTarget = 1;

        UpdateUIButtons();

        // Enable building UI
        if (nextStepButton != null)
        {
            nextStepButton.gameObject.SetActive(true);
            nextStepButton.interactable = false; // Requires at least one ingredient
        }
        if (backButton != null) backButton.gameObject.SetActive(true);
        if (removeLastItemButton != null) removeLastItemButton.gameObject.SetActive(true);

        // LOCK STATE: Prevent adding other categories until this one is finished
        GameManager.gameManager.ChangeState(GameManager.GameState.choosingIngredients);
    }

    // Spawns an ingredient and attaches it to the food stack
    public void AddIngredient(FoodData foodData)
    {
        if (foodData == null || foodData.myLayer != currentLayerTarget) return;
        if (foodData.categoryName != activeCategory) return;

        totalPrice += foodData.price;
        UpdatePriceUI();

        // Position logic based on the previous ingredient's socket
        Transform currentParent = GetOrCreateCategoryParent(activeGroupId);
        Vector3 targetPos = (lastSocketPoints.ContainsKey(activeGroupId) && lastSocketPoints[activeGroupId] != null)
            ? lastSocketPoints[activeGroupId].position : defaultSpawnPoint.position;
        Quaternion targetRot = (lastSocketPoints.ContainsKey(activeGroupId) && lastSocketPoints[activeGroupId] != null)
            ? lastSocketPoints[activeGroupId].rotation : defaultSpawnPoint.rotation;

        GameObject newIngredient = Instantiate(foodData.foodPrefab, targetPos, targetRot);
        newIngredient.transform.SetParent(currentParent);

        // Save metadata for stacking and pricing
        IngredientSocket info = newIngredient.GetComponent<IngredientSocket>();
        if (info != null)
        {
            info.myLayer = foodData.myLayer;
            info.ingredientPrice = foodData.price;
            if (info.socketTransform != null) lastSocketPoints[activeGroupId] = info.socketTransform;
        }

        if (nextStepButton != null) nextStepButton.interactable = true;
    }

    // Completes the assembly of a single item
    public void FinishCurrentCategory()
    {
        if (foodTypeParents.ContainsKey(activeGroupId))
        {
            Transform categoryGroup = foodTypeParents[activeGroupId];
            // Hide the object after a delay to clear the workspace for the next item
            StartCoroutine(DeactivateCategoryAfterDelay(categoryGroup.gameObject, 2f));
        }

        activeCategory = "";
        activeGroupId = "";
        currentLayerTarget = 0;
        UpdateUIButtons();

        if (nextStepButton != null) nextStepButton.gameObject.SetActive(false);
        if (backButton != null) backButton.gameObject.SetActive(false);
        if (removeLastItemButton != null) removeLastItemButton.gameObject.SetActive(false);

        // UNLOCK STATE: Return to initial state so the player can add more items to the same order
        GameManager.gameManager.ChangeState(GameManager.GameState.startedOrdering);
    }

    // Opens the final checkout panel
    public void TryFinishOrder()
    {
        // Guard: Player can't check out if they are in the middle of a burger
        if (GameManager.gameManager.currentGameState == GameManager.GameState.choosingIngredients)
        {
            StopAllCoroutines();
            StartCoroutine(ShowFinishOrderWarning());
            return;
        }

        // LOCK STATE: Now nothing can be clicked until the panel is closed/confirmed
        GameManager.gameManager.ChangeState(GameManager.GameState.confirmingOrdering);

        if (panel != null) panel.SetActive(true);
    }

    // Resets the entire scene and order (Called by the 'X' button or after Success)
    public void ResetOrder()
    {
        // Cleanup all spawned food objects
        foreach (var group in foodTypeParents.Values) if (group != null) Destroy(group.gameObject);

        foodTypeParents.Clear();
        lastSocketPoints.Clear();
        ResetVariables();
        UpdatePriceUI();
        UpdateUIButtons();

        if (panel != null) panel.SetActive(false);

        // FINAL RESET: Back to square one
        GameManager.gameManager.ChangeState(GameManager.GameState.startedOrdering);
    }

    private void UpdatePriceUI() { if (totalPriceText != null) totalPriceText.text = "Total: R$ " + totalPrice.ToString("F2"); }

    // Toggle visibility of UI groups based on current layer
    private void UpdateUIButtons()
    {
        for (int i = 0; i < layerButtonGroups.Length; i++)
            if (layerButtonGroups[i] != null) layerButtonGroups[i].SetActive(i == currentLayerTarget);
    }

    // Creates a parent container for each unique food item in the hierarchy
    private Transform GetOrCreateCategoryParent(string groupId)
    {
        if (!foodTypeParents.ContainsKey(groupId))
        {
            GameObject newGroup = new GameObject("Group_" + groupId);
            newGroup.transform.SetParent(this.transform);
            foodTypeParents.Add(groupId, newGroup.transform);
        }
        return foodTypeParents[groupId];
    }

    private IEnumerator DeactivateCategoryAfterDelay(GameObject target, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (target != null) target.SetActive(false);
    }

    private IEnumerator ShowFinishOrderWarning()
    {
        if (finishOrderWarningText != null)
        {
            finishOrderWarningText.text = "Finish current item first!";
            finishOrderWarningText.gameObject.SetActive(true);
            yield return new WaitForSeconds(2.5f);
            finishOrderWarningText.gameObject.SetActive(false);
        }
    }

    // Entry point for the 'Next' arrow button
    public void OnButtonClick() => StartCoroutine(VerifyLayerFoodExists());

    public IEnumerator VerifyLayerFoodExists()
    {
        if (!CheckIfCurrentLayerHasItems())
        {
            missingFoodLayerWarningText.gameObject.SetActive(true);
            yield return new WaitForSeconds(2f);
            missingFoodLayerWarningText.gameObject.SetActive(false);
        }
        else
        {
            if (currentLayerTarget >= maxLayersActiveCategory) FinishCurrentCategory();
            else { currentLayerTarget++; UpdateUIButtons(); }
        }
    }

    // Checks if the user added at least one ingredient in the current step
    private bool CheckIfCurrentLayerHasItems()
    {
        if (!foodTypeParents.ContainsKey(activeGroupId)) return false;
        foreach (Transform child in foodTypeParents[activeGroupId])
        {
            IngredientSocket info = child.GetComponent<IngredientSocket>();
            if (info != null && info.myLayer == currentLayerTarget) return true;
        }
        return false;
    }

    public void ConfettiEffect()
    {
        if (confettiEffect != null)
        {
            confettiEffect.gameObject.SetActive(true);
            confettiEffect.Play();
        }
    }
}