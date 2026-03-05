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
    [Tooltip("Arraste os grupos de botões: Index 0=Categorias, 1=Pão, 2=Carne, etc.")]
    [SerializeField] GameObject[] layerButtonGroups;
    [SerializeField] TextMeshProUGUI missingFoodLayerWarningText;
    [SerializeField] Button nextStepButton;

    [Header("Setup")]
    [SerializeField] Transform defaultSpawnPoint;

    private string activeCategory = "";
    private int currentLayerTarget = 0;
    private int maxLayersActiveCategory = 0;

    private void Start()
    {
        currentLayerTarget = 0;
        if (nextStepButton != null) nextStepButton.gameObject.SetActive(false); // Esconde o "Avançar" no menu inicial
        UpdateUIButtons();
    }

    // Chamado pelos botões do Menu (Hambúrguer, Batata...)
    public void SelectCategory(FoodData categoryData)
    {
        if (categoryData == null) return;

        activeCategory = categoryData.categoryName;
        maxLayersActiveCategory = categoryData.maxLayersInCategory;

        currentLayerTarget = 1; // Vai para a primeira camada de ingredientes (Pão)
        UpdateUIButtons();

        if (nextStepButton != null)
        {
            nextStepButton.gameObject.SetActive(true); // Mostra o botão de avançar
            nextStepButton.interactable = false;      // Mas bloqueia até escolherem o pão
        }

        GameManager.gameManager.ChangeState(GameManager.GameState.choosingIngredients);
    }

    // Chamado pelos botões de Ingredientes (Pão de Gergelim, Carne Angus...)
    public void AddIngredient(FoodData foodData)
    {
        if (foodData == null || foodData.myLayer != currentLayerTarget) return;

        // Se o player clicou num botão de outra categoria por erro
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

        // LIBERA O BOTÃO: Agora que tem um item, o jogador pode avançar
        if (nextStepButton != null) nextStepButton.interactable = true;
    }

    public IEnumerator VerifyLayerFoodExists()
    {
        bool layerComplete = CheckIfCurrentLayerHasItems();

        if (!layerComplete)
        {
            // Se tentar burlar/clicar sem item, o botão trava de novo e avisa
            if (nextStepButton != null) nextStepButton.interactable = false;

            missingFoodLayerWarningText.gameObject.SetActive(true);
            yield return new WaitForSeconds(2f);
            missingFoodLayerWarningText.gameObject.SetActive(false);
        }
        else
        {
            // SUCESSO: Só aqui a UI realmente muda!
            if (currentLayerTarget >= maxLayersActiveCategory)
            {
                FinishCurrentCategory();
            }
            else
            {
                currentLayerTarget++;
                UpdateUIButtons();
                // Trava o botão para a PRÓXIMA camada até escolherem o novo item
                if (nextStepButton != null) nextStepButton.interactable = false;
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
}