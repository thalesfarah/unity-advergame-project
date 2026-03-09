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
    [SerializeField] Button removeLastItemButton;
    [SerializeField] Button[] removeCategoryButton, addCategoryButton;

    [Header("Setup")]
    [SerializeField] Transform defaultSpawnPoint;

    private string activeCategory = "";
    private string activeGroupId = ""; // NOVA VARIÁVEL: Identificador único do grupo atual
    private int orderItemCounter = 0;  // CONTADOR: Para garantir que cada ID seja único

    private int currentLayerTarget = 0;
    private int maxLayersActiveCategory = 0;
    private float totalPrice = 0f;

    private void Start()
    {
        currentLayerTarget = 0;
        totalPrice = 0f;
        orderItemCounter = 0;
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

        // Cria um ID único para este novo pedido (Ex: "Hamburger_1", "Hamburger_2")
        orderItemCounter++;
        activeCategory = categoryData.categoryName;
        activeGroupId = activeCategory + "_" + orderItemCounter;

        totalPrice += categoryData.price;
        UpdatePriceUI();

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
        // Validação usando activeCategory original para impedir misturar batata com pão
        if (foodData.categoryName != activeCategory) return;

        totalPrice += foodData.price;
        UpdatePriceUI();

        // A partir daqui usamos o activeGroupId (ex: "Hamburger_2")
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

    public void RemoveLastItem()
    {
        if (currentLayerTarget <= 0 || string.IsNullOrEmpty(activeGroupId)) return;
        if (!foodTypeParents.ContainsKey(activeGroupId)) return;

        Transform parent = foodTypeParents[activeGroupId];
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

        if (currentLayerTarget == 0)
        {
            // Se voltamos ao menu inicial (camada 0) e não finalizamos, vamos deletar o grupo cancelado
            if (foodTypeParents.ContainsKey(activeGroupId))
            {
                Destroy(foodTypeParents[activeGroupId].gameObject);
                foodTypeParents.Remove(activeGroupId);
            }
            lastSocketPoints.Remove(activeGroupId);

            // Subtrai o preço base da categoria cancelada
            // Nota: Se quiser essa funcionalidade precisa guardar o preço base, caso contrário, 
            // no ResetOrder abaixo tudo será zerado mesmo.
            ResetOrder();
            return;
        }

        RecalculateSocket();
        UpdateUIButtons();

        if (nextStepButton != null) nextStepButton.interactable = CheckIfCurrentLayerHasItems();
    }

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
        if (!foodTypeParents.ContainsKey(activeGroupId)) return false;
        foreach (Transform child in foodTypeParents[activeGroupId])
        {
            IngredientSocket info = child.GetComponent<IngredientSocket>();
            if (info != null && info.myLayer == currentLayerTarget) return true;
        }
        return false;
    }

    public void FinishCurrentCategory()
    {
        if (foodTypeParents.ContainsKey(activeGroupId))
        {
            Transform categoryGroup = foodTypeParents[activeGroupId];
            StartCoroutine(DeactivateCategoryAfterDelay(categoryGroup.gameObject, 5f));
        }

        // Lógica de reset da UI
        activeCategory = "";
        activeGroupId = ""; // Limpa o ID ativo
        currentLayerTarget = 0;
        UpdateUIButtons();

        if (nextStepButton != null) nextStepButton.gameObject.SetActive(false);
        if (backButton != null) backButton.gameObject.SetActive(false);
        if (removeLastItemButton != null) removeLastItemButton.gameObject.SetActive(false);

        GameManager.gameManager.ChangeState(GameManager.GameState.ingredientsSelectionFinished);

        foreach (Button removeButton in removeCategoryButton)
        {
            if (removeButton != null) removeButton.gameObject.SetActive(false);
        }

        foreach (Button addButton in addCategoryButton)
        {
            if (addButton != null) addButton.gameObject.SetActive(true);
        }
    }

    private IEnumerator DeactivateCategoryAfterDelay(GameObject target, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (target != null)
        {
            target.SetActive(false);
        }
    }

    public void OnButtonClick() => StartCoroutine(VerifyLayerFoodExists());

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

    public void ResetOrder()
    {
        foreach (var group in foodTypeParents.Values) if (group != null) Destroy(group.gameObject);
        foodTypeParents.Clear();
        lastSocketPoints.Clear();
        activeCategory = "";
        activeGroupId = "";
        currentLayerTarget = 0;
        totalPrice = 0f;
        orderItemCounter = 0; // Reseta o contador também
        UpdatePriceUI();
        UpdateUIButtons();
        if (nextStepButton != null) nextStepButton.gameObject.SetActive(false);
        if (removeLastItemButton != null) removeLastItemButton.gameObject.SetActive(false);
        GameManager.gameManager.ChangeState(GameManager.GameState.resetOrdering);
    }
}