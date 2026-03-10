using UnityEngine;

// Creates an entry in the Unity Create menu, allowing you to create new 
// ingredient files directly from the Project window (Right-click > Food > Ingredient).
[CreateAssetMenu(fileName = "NewFoodItem", menuName = "Food/Ingredient")]
public class FoodData : ScriptableObject
{
    [Header("Basic Information")]
    // The display name of the ingredient
    public string ingredientName;

    // The group this item belongs to
    // This is used by FoodManager to filter which items can be stacked together.
    public string categoryName;

    // The cost of adding this specific item to the order
    public float price;

    [Header("Visuals & Logic")]
    // The 3D model (Prefab) that will be instantiated in the scene
    public GameObject foodPrefab;

    // Defines which step of the assembly this item belongs to.
    // (e.g., 1 for Bread, 2 for Meat, 3 for Toppings)
    public int myLayer;

    // Used mainly by the Base/Category items to tell the system 
    // how many total layers the player needs to complete to finish the item.
    public int maxLayersInCategory;
}