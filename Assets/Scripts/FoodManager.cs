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
    [SerializeField] Button nextStepButton;
    [SerializeField] Button backButton; // Opcional: arraste o botão de voltar aqui

    [Header("Setup")]
    [SerializeField] Transform defaultSpawnPoint;

    private string activeCategory = "";
    private int currentLayerTarget = 0;
    private int maxLayersActiveCategory = 0;

    private void Start()
    {
        currentLayerTarget = 0;
        if (nextStepButton != null) nextStepButton.gameObject.SetActive(false);
        if (backButton != null) backButton.gameObject.SetActive(false);
        UpdateUIButtons();
    }

    public void SelectCategory(FoodData categoryData)
    {
        if (categoryData == null) return;
        activeCategory = categoryData.categoryName;
        maxLayersActiveCategory = categoryData.maxLayersInCategory;
        currentLayerTarget = 1;
        UpdateUIButtons();

        if (nextStepButton != null)
        {
            nextStepButton.gameObject.SetActive(true);
            nextStepButton.interactable = false;
        }
        if (backButton != null) backButton.gameObject.SetActive(true);

        GameManager.gameManager.ChangeState(GameManager.GameState.choosingIngredients);
    }

    public void AddIngredient(FoodData foodData)
    {
        if (foodData == null || foodData.myLayer != currentLayerTarget) return;
        if (foodData.categoryName != activeCategory) return;

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
            if (info.socketTransform != null) lastSocketPoints[activeCategory] = info.socketTransform;
        }

        if (nextStepButton != null) nextStepButton.interactable = true;
    }

    // --- LOGICA DE ENGENHARIA REVERSA ---
    public void GoBack()
    {
        if (currentLayerTarget <= 0) return;

        // 1. Remover itens da camada atual antes de voltar
        if (foodTypeParents.ContainsKey(activeCategory))
        {
            Transform parent = foodTypeParents[activeCategory];
            // Criamos uma lista temporária para evitar erro de modificação de coleção durante o loop
            List<GameObject> toDestroy = new List<GameObject>();

            foreach (Transform child in parent)
            {
                IngredientSocket info = child.GetComponent<IngredientSocket>();
                if (info != null && info.myLayer == currentLayerTarget)
                {
                    toDestroy.Add(child.gameObject);
                }
            }

            foreach (GameObject obj in toDestroy) DestroyImmediate(obj);
        }

        // 2. Decrementar camada
        currentLayerTarget--;

        // 3. Se voltamos para o Menu Principal
        if (currentLayerTarget == 0)
        {
            ResetOrder(); // Limpa tudo e volta ao menu
            return;
        }

        // 4. Se ainda estamos dentro do lanche, precisamos achar o novo socket point "do topo"
        UpdateLastSocketAfterBack();

        // 5. Atualizar UI
        UpdateUIButtons();

        // 6. Verificar se a camada para a qual voltamos tem itens (para habilitar o botão de avançar)
        if (nextStepButton != null) nextStepButton.interactable = CheckIfCurrentLayerHasItems();
    }

    private void UpdateLastSocketAfterBack()
    {
        if (!foodTypeParents.ContainsKey(activeCategory)) return;

        Transform parent = foodTypeParents[activeCategory];
        IngredientSocket lastItemFound = null;

        // Procuramos o último item da camada agora ativa (currentLayerTarget)
        foreach (Transform child in parent)
        {
            IngredientSocket info = child.GetComponent<IngredientSocket>();
            if (info != null && info.myLayer == currentLayerTarget)
            {
                lastItemFound = info;
            }
        }

        if (lastItemFound != null)
            lastSocketPoints[activeCategory] = lastItemFound.socketTransform;
        else
            lastSocketPoints[activeCategory] = defaultSpawnPoint; // Volta para a base se não houver nada abaixo
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

        // Controle de visibilidade do botão de voltar
        if (backButton != null) backButton.gameObject.SetActive(currentLayerTarget > 0);
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
        UpdateUIButtons();
        if (nextStepButton != null) nextStepButton.gameObject.SetActive(false);
        GameManager.gameManager.ChangeState(GameManager.GameState.resetOrdering);
    }
}