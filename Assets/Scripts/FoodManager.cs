using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FoodManager : MonoBehaviour
{
    private Dictionary<string, Transform> foodTypeParents = new Dictionary<string, Transform>();
    private Dictionary<string, Transform> lastSocketPoints = new Dictionary<string, Transform>();

    [Header("UI References")]
    [SerializeField] GameObject[] buttons;
    [SerializeField] TextMeshProUGUI missingFoodLayerWarningText;

    [Header("Setup")]
    [SerializeField] Transform defaultSpawnPoint;

    private string activeCategory = "";
    private int currentLayerTarget = 1;
    private int maxLayersActiveCategory = 0; // Armazena o limite da categoria atual

    private void Start()
    {
        missingFoodLayerWarningText.text = "Adicione um ingrediente para prosseguir";
        missingFoodLayerWarningText.gameObject.SetActive(false);
    }

    public void AddIngredient(FoodData foodData)
    {
        if (foodData == null) return;

        // 1. Lógica de Categoria e Definição de Limite
        if (string.IsNullOrEmpty(activeCategory))
        {
            activeCategory = foodData.categoryName;
            maxLayersActiveCategory = foodData.maxLayersInCategory; // Define o limite aqui
            currentLayerTarget = 1;
            GameManager.gameManager.ChangeState(GameManager.GameState.choosingIngredients);
        }
        else if (activeCategory != foodData.categoryName)
        {
            Debug.LogWarning($"Finalize o {activeCategory} antes de começar {foodData.categoryName}!");
            return;
        }

        // 2. Lógica de Camada
        if (foodData.myLayer != currentLayerTarget)
        {
            Debug.LogWarning($"Aguardando camada {currentLayerTarget}.");
            return;
        }

        // 3. Spawn (Mantido)
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
        if (info != null && info.socketTransform != null)
        {
            lastSocketPoints[activeCategory] = info.socketTransform;
        }
    }

    public IEnumerator VerifyLayerFoodExists()
    {
        bool layerComplete = CheckIfCurrentLayerHasItems();

        if (!layerComplete)
        {
            missingFoodLayerWarningText.text = $"Adicione a camada {currentLayerTarget} para avançar!";
            missingFoodLayerWarningText.gameObject.SetActive(true);
            yield return new WaitForSeconds(3f);
            missingFoodLayerWarningText.gameObject.SetActive(false);
        }
        else
        {
            // Se a camada atual foi a última definida no ScriptableObject
            if (currentLayerTarget >= maxLayersActiveCategory)
            {
                Debug.Log($"{activeCategory} concluído com todas as {maxLayersActiveCategory} camadas!");
                FinishCurrentCategory(); // Finaliza e limpa categoria ativa
            }
            else
            {
                currentLayerTarget++;
                Debug.Log($"Camada {currentLayerTarget - 1} confirmada. Próxima: {currentLayerTarget}");
            }
        }
    }

    private bool CheckIfCurrentLayerHasItems()
    {
        // Aqui você pode refinar a busca para garantir que o item na cena 
        // realmente pertence à currentLayerTarget se desejar.
        return foodTypeParents.ContainsKey(activeCategory) && foodTypeParents[activeCategory].childCount > 0;
    }

    public void FinishCurrentCategory()
    {
        activeCategory = "";
        currentLayerTarget = 1;
        maxLayersActiveCategory = 0;

        foreach (GameObject button in buttons) button.SetActive(true);
        GameManager.gameManager.ChangeState(GameManager.GameState.ingredientsSelectionFinished);
    }

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
        currentLayerTarget = 1;
        maxLayersActiveCategory = 0;
        GameManager.gameManager.ChangeState(GameManager.GameState.resetOrdering);
    }

    public void OnButtonClick()
    {
        StartCoroutine(VerifyLayerFoodExists());
    }
}