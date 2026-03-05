using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FoodManager : MonoBehaviour
{
    private Dictionary<string, Transform> foodTypeParents = new Dictionary<string, Transform>();
    private Dictionary<string, Transform> lastSocketPoints = new Dictionary<string, Transform>();

    [Header("UI References")]
    [SerializeField] GameObject[] layerButtonGroups;
    [SerializeField] TextMeshProUGUI missingFoodLayerWarningText;
    [SerializeField] TextMeshProUGUI totalPriceText;
    [SerializeField] Button nextStepButton;
    [SerializeField] Button backButton;
    [SerializeField] Button removeLastItemButton; // NOVA REFERÊNCIA ADICIONADA

    [Header("Setup")]
    [SerializeField] Transform defaultSpawnPoint;

    private string activeCategory = "";
    private int currentLayerTarget = 0;
    private int maxLayersActiveCategory = 0;
    private float totalPrice = 0f;

    private void Start()
    {
        currentLayerTarget = 0;
        totalPrice = 0f;
        UpdatePriceUI();

        // Esconde os botões de ação no menu inicial
        if (nextStepButton != null) nextStepButton.gameObject.SetActive(false);
        if (backButton != null) backButton.gameObject.SetActive(false);
        if (removeLastItemButton != null) removeLastItemButton.gameObject.SetActive(false);

        UpdateUIButtons();
    }

    public void SelectCategory(FoodData categoryData)
    {
        if (categoryData == null) return;

        totalPrice += categoryData.price;
        UpdatePriceUI();

        activeCategory = categoryData.categoryName;
        maxLayersActiveCategory = categoryData.maxLayersInCategory;
        currentLayerTarget = 1;
        UpdateUIButtons();

        // Ativa os botões de controle ao entrar em uma categoria
        if (nextStepButton != null)
        {
            nextStepButton.gameObject.SetActive(true);
            nextStepButton.interactable = false;
        }
        if (backButton != null) backButton.gameObject.SetActive(true);
        if (removeLastItemButton != null) removeLastItemButton.gameObject.SetActive(true);

        GameManager.gameManager.ChangeState(GameManager.GameState.choosingIngredients);
    }

    public void AddIngredient(FoodData foodData)
    {
        if (foodData == null || foodData.myLayer != currentLayerTarget) return;
        if (foodData.categoryName != activeCategory) return;

        totalPrice += foodData.price;
        UpdatePriceUI();

        Transform currentParent = GetOrCreateCategoryParent(activeCategory);
        Vector3 targetPos = (lastSocketPoints.ContainsKey(activeCategory) && lastSocketPoints[activeCategory] != null)
            ? lastSocketPoints[activeCategory].position
            : defaultSpawnPoint.position;
        Quaternion targetRot = (lastSocketPoints.ContainsKey(activeCategory) && lastSocketPoints[activeCategory] != null)
            ? lastSocketPoints[activeCategory].rotation
            : defaultSpawnPoint.rotation;

        GameObject newIngredient = Instantiate(foodData.foodPrefab, targetPos, targetRot);
        newIngredient.transform.SetParent(currentParent);

        IngredientSocket info = newIngredient.GetComponent<IngredientSocket>();
        if (info != null)
        {
            info.myLayer = foodData.myLayer;
            info.ingredientPrice = foodData.price;
            if (info.socketTransform != null) lastSocketPoints[activeCategory] = info.socketTransform;
        }

        if (nextStepButton != null) nextStepButton.interactable = true;
    }

    public void RemoveLastItem()
    {
        if (currentLayerTarget <= 0 || string.IsNullOrEmpty(activeCategory)) return;
        if (!foodTypeParents.ContainsKey(activeCategory)) return;

        Transform parent = foodTypeParents[activeCategory];
        GameObject itemToDestroy = null;
        IngredientSocket itemInfo = null;

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
            RecalculateSocket();

            if (nextStepButton != null)
                nextStepButton.interactable = CheckIfCurrentLayerHasItems();
        }
    }

    public void GoBack()
    {
        if (currentLayerTarget <= 0) return;

        if (foodTypeParents.ContainsKey(activeCategory))
        {
            Transform parent = foodTypeParents[activeCategory];
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

        if (currentLayerTarget == 0)
        {
            ResetOrder();
            return;
        }

        RecalculateSocket();
        UpdateUIButtons();

        if (nextStepButton != null) nextStepButton.interactable = CheckIfCurrentLayerHasItems();
    }

    private void RecalculateSocket()
    {
        if (!foodTypeParents.ContainsKey(activeCategory)) return;

        Transform parent = foodTypeParents[activeCategory];
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
            lastSocketPoints[activeCategory] = lastValidSocket.socketTransform;
        else
            lastSocketPoints[activeCategory] = defaultSpawnPoint;
    }

    private void UpdatePriceUI()
    {
        if (totalPriceText != null)
        {
            totalPriceText.text = "Total: R$ " + totalPrice.ToString("F2");
        }
    }

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

    private void UpdateUIButtons()
    {
        for (int i = 0; i < layerButtonGroups.Length; i++)
        {
            if (layerButtonGroups[i] != null)
                layerButtonGroups[i].SetActive(i == currentLayerTarget);
        }

        // Controla visibilidade dos botões extras
        bool inIngredientSelection = currentLayerTarget > 0;
        if (backButton != null) backButton.gameObject.SetActive(inIngredientSelection);
        if (removeLastItemButton != null) removeLastItemButton.gameObject.SetActive(inIngredientSelection);
    }

    private bool CheckIfCurrentLayerHasItems()
    {
        if (!foodTypeParents.ContainsKey(activeCategory)) return false;
        foreach (Transform child in foodTypeParents[activeCategory])
        {
            IngredientSocket info = child.GetComponent<IngredientSocket>();
            if (info != null && info.myLayer == currentLayerTarget) return true;
        }
        return false;
    }

    public void FinishCurrentCategory()
    {
        activeCategory = "";
        currentLayerTarget = 0;
        UpdateUIButtons();
        if (nextStepButton != null) nextStepButton.gameObject.SetActive(false);
        if (removeLastItemButton != null) removeLastItemButton.gameObject.SetActive(false);
        GameManager.gameManager.ChangeState(GameManager.GameState.ingredientsSelectionFinished);
    }

    public void OnButtonClick() => StartCoroutine(VerifyLayerFoodExists());

    private Transform GetOrCreateCategoryParent(string categoryName)
    {
        if (!foodTypeParents.ContainsKey(categoryName))
        {
            GameObject newGroup = new GameObject("Group_" + categoryName);
            newGroup.transform.SetParent(this.transform);
            foodTypeParents.Add(categoryName, newGroup.transform);
        }
        return foodTypeParents[categoryName];
    }

    public void ResetOrder()
    {
        foreach (var group in foodTypeParents.Values) if (group != null) Destroy(group.gameObject);
        foodTypeParents.Clear();
        lastSocketPoints.Clear();
        activeCategory = "";
        currentLayerTarget = 0;
        totalPrice = 0f;
        UpdatePriceUI();
        UpdateUIButtons();
        if (nextStepButton != null) nextStepButton.gameObject.SetActive(false);
        if (removeLastItemButton != null) removeLastItemButton.gameObject.SetActive(false);
        GameManager.gameManager.ChangeState(GameManager.GameState.resetOrdering);
    }
}