using UnityEngine;

[CreateAssetMenu(fileName = "NewFoodItem", menuName = "Food/Ingredient")]
public class FoodData : ScriptableObject
{
    public string ingredientName;
    public string categoryName;
    public float price;
    public GameObject foodPrefab;
    public int myLayer;           // A camada deste item (ex: 1 para pão, 2 para carne)
    public int maxLayersInCategory; // Total de camadas que esta categoria possui (ex: 3 para hamburger)
}