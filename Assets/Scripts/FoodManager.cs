using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FoodManager : MonoBehaviour
{
    private Dictionary<string, Transform> foodTypeParents = new Dictionary<string, Transform>();

    private Dictionary<string, Transform> lastSocketPoints = new Dictionary<string, Transform>();

    [SerializeField] GameObject[] buttons;

    [SerializeField] TextMeshProUGUI missingFoodLayerWarningText;

    string activeCategory = "";


    private void Start()
    {
        missingFoodLayerWarningText.text = 
            "You should add one kind of ingredient at least to proceed";
    }
    public void AddIngredient(Food foodData)
    {

        if (foodData == null) return;

        string categoryName = foodData.GetType().Name;

        if (string.IsNullOrEmpty(activeCategory)) 
        {
            activeCategory = categoryName;
            GameManager.gameManager.ChangeState(GameManager.GameState.choosingIngredients);

        }
        else if (activeCategory != categoryName)
        {
            Debug.LogWarning($"Você precisa finalizar o {activeCategory} antes de começar {categoryName}!");
            return;
        }

        Transform currentParent = GetOrCreateCategoryParent(categoryName);

        Vector3 targetPos;

        Quaternion targetRot;

        if (lastSocketPoints.ContainsKey(categoryName) && lastSocketPoints[categoryName] != null)
        {
            targetPos = lastSocketPoints[categoryName].position;

            targetRot = lastSocketPoints[categoryName].rotation;
        }
        else
        {
            targetPos = foodData.foodPrefabSpawnPos.position;

            targetRot = foodData.foodPrefabSpawnPos.rotation;
        }

        
        GameObject newIngredient = Instantiate(foodData.foodPrefab, targetPos, targetRot);
        
        newIngredient.transform.SetParent(currentParent);

        IngredientSocket info = newIngredient.GetComponent<IngredientSocket>();

        if (info != null && info.socketTransform != null)
        {
            
            lastSocketPoints[categoryName] = info.socketTransform;
        }
    }
    // Método para ser chamado pelo botão "Finalizar Item" (ex: "Fechar Hambúrguer")
    public void FinishCurrentCategory()
    {
        if (string.IsNullOrEmpty(activeCategory)) return;

        Debug.Log($"{activeCategory} finalizado. Agora você pode escolher outra categoria.");
        activeCategory = ""; // Libera para a próxima categoria

        // Se quiser que todos os botões voltem a aparecer após finalizar
        foreach (GameObject button in buttons)
        {
            button.SetActive(true);
        }
    }

    private Transform GetOrCreateCategoryParent(string categoryName)
    {
        if (!foodTypeParents.ContainsKey(categoryName))
        {
            GameObject newGroup = new GameObject(categoryName);

            newGroup.transform.SetParent(this.transform);

            foodTypeParents.Add(categoryName, newGroup.transform);
        }
        return foodTypeParents[categoryName];
    }

    public void ResetOrder()
    {
        foreach (var group in foodTypeParents.Values)
        {
            if (group != null) Destroy(group.gameObject);
        }
        foodTypeParents.Clear();

        lastSocketPoints.Clear();

        activeCategory = "";

        GameManager.gameManager.ChangeState(GameManager.GameState.resetOrdering);
    }
    public IEnumerator VerifyLayerFoodExists() 
    {
        bool missingFoodLayer = false;
        if (GameObject.FindGameObjectsWithTag("Food_Layer2").Length < 1)
        {
            missingFoodLayer = true;
            missingFoodLayerWarningText.gameObject.SetActive(true);
            yield return new WaitForSeconds(3f);
            missingFoodLayerWarningText.gameObject.SetActive(false);
            
            Debug.Log("No food objects found in the scene.");
            
        }
        if (missingFoodLayer)
            foreach (GameObject button in buttons)
            {
                button.SetActive(true);
            }
        else
            foreach (GameObject button in buttons)
            {
                button.SetActive(false);
            }  
    }
    public void OnButtonClick() 
    {
        StartCoroutine(VerifyLayerFoodExists());
    }
}