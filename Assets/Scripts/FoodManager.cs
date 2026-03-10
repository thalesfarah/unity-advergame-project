using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FoodManager : MonoBehaviour
{
    // Dictionary to store parent transforms for each unique order item (e.g., "Burger_1")
    private Dictionary<string, Transform> foodTypeParents = new Dictionary<string, Transform>();
    // Stores the last available attachment point (socket) for each group to stack ingredients
    private Dictionary<string, Transform> lastSocketPoints = new Dictionary<string, Transform>();

    [SerializeField] ParticleSystem confettiEffect;

    [Header("UI References")]
    [SerializeField] GameObject[] layerButtonGroups; // Groups of buttons for each construction step (Layer 1, 2, etc.)
    [SerializeField]
    TextMeshProUGUI
        missingFoodLayerWarningText, // Warning when player tries to skip a layer without ingredients
        finishOrderWarningText,      // Warning when player tries to buy before finishing assembly
        totalPriceText;              // Text display for the total cost
    [SerializeField] Button nextStepButton;
    [SerializeField] Button backButton;
    [SerializeField] Button removeLastItemButton;
    [SerializeField] Button[] removeCategoryButton, addCategoryButton;
    [SerializeField] GameObject panel; // Success/Confirmation panel

    [Header("Setup")]
    [SerializeField] Transform defaultSpawnPoint; // Initial position where the base of the food starts

    // Internal state variables
    private string activeCategory = "";     // Current category (e.g., "Burger")
    private string activeGroupId = "";      // Unique ID for the current instance (e.g., "Burger_2")
    private int orderItemCounter = 0;       // Counter to generate unique Group IDs

    private int currentLayerTarget = 0;     // The current step of assembly (Layer 1, 2, etc.)
    private int maxLayersActiveCategory = 0; // Total layers required for the chosen category
    private float totalPrice = 0f;          // Cumulative price of the entire order

    private void Start()
    {
        // Initialization of variables and UI state
        currentLayerTarget = 0;
        totalPrice = 0f;
        orderItemCounter = 0;

        if (finishOrderWarningText != null) finishOrderWarningText.gameObject.SetActive(false);

        UpdatePriceUI();

        // Hide action buttons until a category is selected
        if (nextStepButton != null) nextStepButton.gameObject.SetActive(false);
        if (backButton != null) backButton.gameObject.SetActive(false);
        if (removeLastItemButton != null) removeLastItemButton.gameObject.SetActive(false);

        UpdateUIButtons();
    }

    // Triggers the celebratory confetti visual effect
    public void ConfettiEffect()
    {
        if (confettiEffect != null)
        {
            confettiEffect.gameObject.SetActive(true);
            confettiEffect.Stop();
            confettiEffect.Play();
        }
    }

    // Handles the logic when the player clicks the 'Confirm/Buy' button
    public void TryFinishOrder()
    {
        // Check if the player is currently in the middle of assembling an item
        if (GameManager.gameManager.currentGameState == GameManager.GameState.choosingIngredients)
        {
            StopAllCoroutines(); // Stop previous warnings to avoid overlapping
            StartCoroutine(ShowFinishOrderWarning());
            return;
        }
        else
        {
            // ORDER SUCCESS: Finalize state and show success UI
            GameManager.gameManager.ChangeState(GameManager.GameState.orderFinished);
            panel.SetActive(true);
            Debug.Log("Order purchased successfully!");

            // Reset total price to zero for the next order
            totalPrice = 0f;
            UpdatePriceUI();

            // Clear the scene and internal data for a fresh start
            ResetOrder();
        }
    }

    // Coroutine to display the "Finish assembly first" warning for a few seconds
    private IEnumerator ShowFinishOrderWarning()
    {
        if (finishOrderWarningText != null)
        {
            finishOrderWarningText.text = "Please finish your current item assembly first!";
            finishOrderWarningText.gameObject.SetActive(true);
            yield return new WaitForSeconds(2.5f);
            finishOrderWarningText.gameObject.SetActive(false);
        }
    }

    // Initiates the creation of a new food item (e.g., starts a Burger assembly)
    public void SelectCategory(FoodData categoryData)
    {
        if (categoryData == null) return;

        // Generate a new unique ID for this specific item
        orderItemCounter++;
        activeCategory = categoryData.categoryName;
        activeGroupId = activeCategory + "_" + orderItemCounter;

        totalPrice += categoryData.price;
        UpdatePriceUI();

        maxLayersActiveCategory = categoryData.maxLayersInCategory;
        currentLayerTarget = 1; // Move to the first ingredient selection step
        UpdateUIButtons();

        // Reveal assembly controls
        if (nextStepButton != null)
        {
            nextStepButton.gameObject.SetActive(true);
            nextStepButton.interactable = false; // Disable until at least one ingredient is added
        }
        if (backButton != null) backButton.gameObject.SetActive(true);
        if (removeLastItemButton != null) removeLastItemButton.gameObject.SetActive(true);

        // Update Global State
        GameManager.gameManager.ChangeState(GameManager.GameState.choosingIngredients);
    }

    // Instantiates an ingredient and attaches it to the current food stack
    public void AddIngredient(FoodData foodData)
    {
        // Validate if the ingredient belongs to the current layer and category
        if (foodData == null || foodData.myLayer != currentLayerTarget) return;
        if (foodData.categoryName != activeCategory) return;

        totalPrice += foodData.price;
        UpdatePriceUI();

        // Determine spawn position based on the last added socket
        Transform currentParent = GetOrCreateCategoryParent(activeGroupId);
        Vector3 targetPos = (lastSocketPoints.ContainsKey(activeGroupId) && lastSocketPoints[activeGroupId] != null)
            ? lastSocketPoints[activeGroupId].position : defaultSpawnPoint.position;
        Quaternion targetRot = (lastSocketPoints.ContainsKey(activeGroupId) && lastSocketPoints[activeGroupId] != null)
            ? lastSocketPoints[activeGroupId].rotation : defaultSpawnPoint.rotation;

        // Create the 3D model
        GameObject newIngredient = Instantiate(foodData.foodPrefab, targetPos, targetRot);
        newIngredient.transform.SetParent(currentParent);

        // Record ingredient data and update the stacking socket point
        IngredientSocket info = newIngredient.GetComponent<IngredientSocket>();
        if (info != null)
        {
            info.myLayer = foodData.myLayer;
            info.ingredientPrice = foodData.price;
            if (info.socketTransform != null) lastSocketPoints[activeGroupId] = info.socketTransform;
        }

        if (nextStepButton != null) nextStepButton.interactable = true;
    }

    // Deletes the very last ingredient added in the current layer
    public void RemoveLastItem()
    {
        if (currentLayerTarget <= 0 || string.IsNullOrEmpty(activeGroupId)) return;
        if (!foodTypeParents.ContainsKey(activeGroupId)) return;

        Transform parent = foodTypeParents[activeGroupId];
        GameObject itemToDestroy = null;
        IngredientSocket itemInfo = null;

        // Search backwards through children to find the most recent item in this layer
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            IngredientSocket info = child.GetComponent<IngredientSocket>();
            if (info != null && info.myLayer == currentLayerTarget)
            {
                itemToDestroy = child.gameObject;
                itemInfo = info;
                break;
            }
        }

        if (itemToDestroy != null)
        {
            totalPrice -= itemInfo.ingredientPrice;
            UpdatePriceUI();
            DestroyImmediate(itemToDestroy);
            RecalculateSocket(); // Update the stack height point after removal

            if (nextStepButton != null)
                nextStepButton.interactable = CheckIfCurrentLayerHasItems();
        }
    }

    // Reverts to the previous layer and removes all items added in the current layer
    public void GoBack()
    {
        if (currentLayerTarget <= 0) return;

        // Wipe current layer ingredients
        if (foodTypeParents.ContainsKey(activeGroupId))
        {
            Transform parent = foodTypeParents[activeGroupId];
            List<GameObject> toDestroy = new List<GameObject>();

            foreach (Transform child in parent)
            {
                IngredientSocket info = child.GetComponent<IngredientSocket>();
                if (info != null && info.myLayer == currentLayerTarget)
                {
                    totalPrice -= info.ingredientPrice;
                    toDestroy.Add(child.gameObject);
                }
            }
            foreach (GameObject obj in toDestroy) DestroyImmediate(obj);
            UpdatePriceUI();
        }

        currentLayerTarget--;

        // If back to start, cancel the whole item construction
        if (currentLayerTarget == 0)
        {
            if (foodTypeParents.ContainsKey(activeGroupId))
            {
                Destroy(foodTypeParents[activeGroupId].gameObject);
                foodTypeParents.Remove(activeGroupId);
            }
            lastSocketPoints.Remove(activeGroupId);
            ResetOrder();
            return;
        }

        RecalculateSocket();
        UpdateUIButtons();

        if (nextStepButton != null) nextStepButton.interactable = CheckIfCurrentLayerHasItems();
    }

    // Finds the new top-most socket after an item is removed or layer is changed
    private void RecalculateSocket()
    {
        if (!foodTypeParents.ContainsKey(activeGroupId)) return;

        Transform parent = foodTypeParents[activeGroupId];
        IngredientSocket lastValidSocket = null;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            IngredientSocket info = parent.GetChild(i).GetComponent<IngredientSocket>();
            if (info != null)
            {
                lastValidSocket = info;
                break;
            }
        }

        if (lastValidSocket != null && lastValidSocket.socketTransform != null)
            lastSocketPoints[activeGroupId] = lastValidSocket.socketTransform;
        else
            lastSocketPoints[activeGroupId] = defaultSpawnPoint;
    }

    // Refreshes the currency text on the UI
    private void UpdatePriceUI()
    {
        if (totalPriceText != null)
        {
            totalPriceText.text = "Total: R$ " + totalPrice.ToString("F2");
        }
    }

    // Logic for the 'Next' button: checks if the layer is complete and advances
    public IEnumerator VerifyLayerFoodExists()
    {
        if (!CheckIfCurrentLayerHasItems())
        {
            if (nextStepButton != null) nextStepButton.interactable = false;
            missingFoodLayerWarningText.gameObject.SetActive(true);
            yield return new WaitForSeconds(2f);
            missingFoodLayerWarningText.gameObject.SetActive(false);
        }
        else
        {
            // If it was the last layer, finish the assembly
            if (currentLayerTarget >= maxLayersActiveCategory)
                FinishCurrentCategory();
            else
            {
                currentLayerTarget++;
                UpdateUIButtons();
                if (nextStepButton != null) nextStepButton.interactable = CheckIfCurrentLayerHasItems();
            }
        }
    }

    // Toggles visibility of button groups based on current assembly step
    private void UpdateUIButtons()
    {
        for (int i = 0; i < layerButtonGroups.Length; i++)
        {
            if (layerButtonGroups[i] != null)
                layerButtonGroups[i].SetActive(i == currentLayerTarget);
        }

        bool inIngredientSelection = currentLayerTarget > 0;
        if (backButton != null) backButton.gameObject.SetActive(inIngredientSelection);
        if (removeLastItemButton != null) removeLastItemButton.gameObject.SetActive(inIngredientSelection);
    }

    // Helper: checks if there is at least one object in the current layer
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

    // Completes the assembly of an item and prepares for the next one
    public void FinishCurrentCategory()
    {
        if (foodTypeParents.ContainsKey(activeGroupId))
        {
            Transform categoryGroup = foodTypeParents[activeGroupId];
            // Briefly keep the object active before hiding it (simulates sending it to a tray)
            StartCoroutine(DeactivateCategoryAfterDelay(categoryGroup.gameObject, 2f));
        }

        activeCategory = "";
        activeGroupId = "";
        currentLayerTarget = 0;
        UpdateUIButtons();

        // Clean up UI for the main menu state
        if (nextStepButton != null) nextStepButton.gameObject.SetActive(false);
        if (backButton != null) backButton.gameObject.SetActive(false);
        if (removeLastItemButton != null) removeLastItemButton.gameObject.SetActive(false);

        // Inform the Global State that selection is done
        GameManager.gameManager.ChangeState(GameManager.GameState.ingredientsSelectionFinished);

        // Hide removal UI and show Category selection again
        foreach (Button removeButton in removeCategoryButton)
        {
            if (removeButton != null) removeButton.gameObject.SetActive(false);
        }

        foreach (Button addButton in addCategoryButton)
        {
            if (addButton != null) addButton.gameObject.SetActive(true);
        }
    }

    // Simply hides a completed food item after a short delay
    private IEnumerator DeactivateCategoryAfterDelay(GameObject target, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (target != null)
        {
            target.SetActive(false);
        }
    }

    public void OnButtonClick() => StartCoroutine(VerifyLayerFoodExists());

    // Organizes scene hierarchy by grouping ingredients under a single parent object
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

    // Full system reset: deletes all objects, clears dictionaries, and resets price
    public void ResetOrder()
    {
        foreach (var group in foodTypeParents.Values) if (group != null) Destroy(group.gameObject);
        foodTypeParents.Clear();
        lastSocketPoints.Clear();
        activeCategory = "";
        activeGroupId = "";
        currentLayerTarget = 0;
        totalPrice = 0f;
        orderItemCounter = 0;
        UpdatePriceUI();
        UpdateUIButtons();

        if (nextStepButton != null) nextStepButton.gameObject.SetActive(false);
        if (removeLastItemButton != null) removeLastItemButton.gameObject.SetActive(false);

        GameManager.gameManager.ChangeState(GameManager.GameState.resetOrdering);
    }
}