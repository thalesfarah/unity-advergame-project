using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FoodManager : MonoBehaviour
{
    private Dictionary<string, Transform> foodTypeParents = new Dictionary<string, Transform>();
    private Dictionary<string, Transform> lastSocketPoints = new Dictionary<string, Transform>();

    [SerializeField] ParticleSystem confettiEffect;

    [Header("UI References")]
    [SerializeField] GameObject[] layerButtonGroups;
    [SerializeField] TextMeshProUGUI missingFoodLayerWarningText, finishOrderWarningText, totalPriceText;
    [SerializeField] Button nextStepButton, backButton, removeLastItemButton;
    [SerializeField] Button[] removeCategoryButton, addCategoryButton;
    public GameObject panel;

    [Header("Setup")]
    [SerializeField] Transform defaultSpawnPoint;

    private string activeCategory = "";
    private string activeGroupId = "";
    private int orderItemCounter = 0;
    private int currentLayerTarget = 0;
    private int maxLayersActiveCategory = 0;
    private float totalPrice = 0f;

    private void Start()
    {
        ResetVariables();
        UpdatePriceUI();
        UpdateUIButtons();
    }

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

    // Starts building a new food item from a category
    public void SelectCategory(FoodData categoryData)
    {
        if (GameManager.gameManager.currentGameState == GameManager.GameState.confirmingOrdering ||
            GameManager.gameManager.currentGameState == GameManager.GameState.choosingIngredients)
        {
            return;
        }

        if (categoryData == null) return;

        orderItemCounter++;
        activeCategory = categoryData.categoryName;
        activeGroupId = activeCategory + "_" + orderItemCounter;

        totalPrice += categoryData.price;
        UpdatePriceUI();

        maxLayersActiveCategory = categoryData.maxLayersInCategory;
        currentLayerTarget = 1;

        UpdateUIButtons();

        if (nextStepButton != null)
        {
            nextStepButton.gameObject.SetActive(true);
            nextStepButton.interactable = false;
        }
        if (backButton != null) backButton.gameObject.SetActive(true);
        if (removeLastItemButton != null) removeLastItemButton.gameObject.SetActive(true);

        GameManager.gameManager.ChangeState(GameManager.GameState.choosingIngredients);
    }

    // Adds a visual ingredient and updates price/stacking
    public void AddIngredient(FoodData foodData)
    {
        if (foodData == null || foodData.myLayer != currentLayerTarget) return;
        if (foodData.categoryName != activeCategory) return;

        totalPrice += foodData.price;
        UpdatePriceUI();

        Transform currentParent = GetOrCreateCategoryParent(activeGroupId);
        Vector3 targetPos = (lastSocketPoints.ContainsKey(activeGroupId) && lastSocketPoints[activeGroupId] != null)
            ? lastSocketPoints[activeGroupId].position : defaultSpawnPoint.position;
        Quaternion targetRot = (lastSocketPoints.ContainsKey(activeGroupId) && lastSocketPoints[activeGroupId] != null)
            ? lastSocketPoints[activeGroupId].rotation : defaultSpawnPoint.rotation;

        GameObject newIngredient = Instantiate(foodData.foodPrefab, targetPos, targetRot);
        newIngredient.transform.SetParent(currentParent);

        IngredientSocket info = newIngredient.GetComponent<IngredientSocket>();
        if (info != null)
        {
            info.myLayer = foodData.myLayer;
            info.ingredientPrice = foodData.price;
            if (info.socketTransform != null) lastSocketPoints[activeGroupId] = info.socketTransform;
        }

        if (nextStepButton != null) nextStepButton.interactable = true;
    }

    // Returns to the previous layer
    public void GoBack()
    {
        if (currentLayerTarget > 1)
        {
            currentLayerTarget--;
            UpdateUIButtons();
        }
    }

    // Removes the last item from the current build
    public void RemoveLastItem()
    {
        if (GameManager.gameManager.currentGameState != GameManager.GameState.choosingIngredients) return;

        if (foodTypeParents.ContainsKey(activeGroupId))
        {
            Transform currentParent = foodTypeParents[activeGroupId];
            int childCount = currentParent.childCount;

            if (childCount > 1)
            {
                Transform lastItem = currentParent.GetChild(childCount - 1);
                IngredientSocket info = lastItem.GetComponent<IngredientSocket>();

                if (info != null)
                {
                    totalPrice -= info.ingredientPrice;
                    UpdatePriceUI();
                }

                Destroy(lastItem.gameObject);

                Transform penultItem = currentParent.GetChild(childCount - 2);
                IngredientSocket penultInfo = penultItem.GetComponent<IngredientSocket>();

                if (penultInfo != null && penultInfo.socketTransform != null)
                {
                    lastSocketPoints[activeGroupId] = penultInfo.socketTransform;
                }
                else
                {
                    lastSocketPoints[activeGroupId] = defaultSpawnPoint;
                }
            }
        }
    }

    // Finishes building the current item
    public void FinishCurrentCategory()
    {
        if (foodTypeParents.ContainsKey(activeGroupId))
        {
            Transform categoryGroup = foodTypeParents[activeGroupId];
            StartCoroutine(DeactivateCategoryAfterDelay(categoryGroup.gameObject, 2f));
        }

        activeCategory = "";
        activeGroupId = "";
        currentLayerTarget = 0;
        UpdateUIButtons();

        if (nextStepButton != null) nextStepButton.gameObject.SetActive(false);
        if (backButton != null) backButton.gameObject.SetActive(false);
        if (removeLastItemButton != null) removeLastItemButton.gameObject.SetActive(false);

        GameManager.gameManager.ChangeState(GameManager.GameState.startedOrdering);
    }

    // Triggers the final order step
    public void TryFinishOrder()
    {
        if (GameManager.gameManager.currentGameState == GameManager.GameState.choosingIngredients)
        {
            StopAllCoroutines();
            StartCoroutine(ShowFinishOrderWarning());
            return;
        }

        GameManager.gameManager.ChangeState(GameManager.GameState.orderFinished);
        if (panel != null) panel.SetActive(true);

        
    }

    // Resets everything for a new customer
    public void ResetOrder()
    {
        foreach (var group in foodTypeParents.Values) if (group != null) Destroy(group.gameObject);
        foodTypeParents.Clear();
        lastSocketPoints.Clear();
        ResetVariables();
        UpdatePriceUI();
        UpdateUIButtons();

        if (panel != null) panel.SetActive(false);

        GameManager.gameManager.ChangeState(GameManager.GameState.startedOrdering);
    }

    private void UpdatePriceUI() { if (totalPriceText != null) totalPriceText.text = "Total: R$ " + totalPrice.ToString("F2"); }

    private void UpdateUIButtons()
    {
        for (int i = 0; i < layerButtonGroups.Length; i++)
            if (layerButtonGroups[i] != null) layerButtonGroups[i].SetActive(i == currentLayerTarget);
    }

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

    // Plays the confetti particle effect
    public void ConfettiEffect()
    {
        if (confettiEffect != null)
        {
            confettiEffect.gameObject.SetActive(true);
            confettiEffect.Play();
        }
    }
}