using UnityEngine;
using System.Collections.Generic;

public class FoodManager : MonoBehaviour
{
    private Dictionary<string, Transform> foodTypeParents = new Dictionary<string, Transform>();

    // Dicionário para guardar o último Transform de conexão de cada categoria
    // Ex: "Hamburger" -> Transform do topo da última carne colocada
    private Dictionary<string, Transform> lastSocketPoints = new Dictionary<string, Transform>();

    public void AddIngredient(Food foodData)
    {
        if (foodData == null) return;

        string categoryName = foodData.GetType().Name;
        Transform currentParent = GetOrCreateCategoryParent(categoryName);

        // 1. Decidir onde spawnar
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

        // 2. Instanciar
        GameObject newIngredient = Instantiate(foodData.foodPrefab, targetPos, targetRot);

        // IMPORTANTE: Colocar no Parent antes de atualizar o socket
        newIngredient.transform.SetParent(currentParent);

        // 3. Atualizar o Socket para o PRÓXIMO item
        IngredientSocket info = newIngredient.GetComponent<IngredientSocket>();

        if (info != null && info.socketTransform != null)
        {
            // Aqui está o pulo do gato: forçamos a atualização da matriz de transformação
            // para garantir que a posição do socket seja lida corretamente no próximo frame
            lastSocketPoints[categoryName] = info.socketTransform;
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
        lastSocketPoints.Clear(); // Limpa os pontos de conexão
    }
}