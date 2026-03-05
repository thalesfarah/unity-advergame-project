using UnityEngine;

[CreateAssetMenu(fileName = "NewFoodItem", menuName = "Food/Ingredient")]
public class FoodData : ScriptableObject
{
    public string ingredientName;
    public string categoryName;
    public float price;
    public GameObject foodPrefab;
    public int myLayer;           // Camada deste item específico
    public int maxLayersInCategory; // Total de camadas que esta categoria possui
}