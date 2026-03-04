using System;
using UnityEngine;
public class CustomFood : MonoBehaviour
{
    [SerializeField] GameObject foodPrefab;
    [SerializeField] Transform foodSpawn;
    [SerializeField] Material[] foodMaterials;

    GameObject spawnedFood;
    int currentMaterialIndex = 0;
    public void SpawnFood(GameObject foodPrefabReference) 
    {
        foodPrefab = foodPrefabReference;
        if (foodPrefab == null)
        {
            Debug.LogWarning("`foodPrefab` não está atribuído.");
            return;
        }
        
        if (spawnedFood !=null) 
        {
            Destroy(spawnedFood);
        }
        spawnedFood = Instantiate(foodPrefab, foodSpawn.transform.position, Quaternion.identity);
        //ApplyMaterialIndex(currentMaterialIndex);
    }
   public void ApplyMaterialIndex(int index)
    {
        if (foodMaterials == null || foodMaterials.Length == 0)
        {
            Debug.LogWarning("Nenhum material configurado em `foodMaterials`.");
            return;
        }

        if (index < 0 || index >= foodMaterials.Length)
        {
            Debug.LogWarning($"Índice de material inválido: {index}");
            return;
        }

        GameObject target = spawnedFood ?? foodPrefab;
        if (target == null)
        {
            Debug.LogWarning("Nenhum objeto de comida disponível para alterar material.");
            return;
        }

        MeshRenderer renderer = target.GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            Debug.LogWarning("MeshRenderer não encontrado no objeto de comida.");
            return;
        }

        renderer.material = foodMaterials[index];
        currentMaterialIndex = index;
    }
}
