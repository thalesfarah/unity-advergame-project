using UnityEngine;

public class IngredientSocket : MonoBehaviour
{
    // The specific point (Transform) where the next ingredient should be attached
    // This allows each ingredient to have a custom 'height' or 'offset' for the stack
    public Transform socketTransform;

    // Stores which layer this specific ingredient belongs to (e.g., Layer 1, 2, etc.)
    [HideInInspector] public int myLayer;

    // Stores the individual price of this specific item at the moment it was created
    // This is crucial for the 'RemoveLastItem' logic to subtract the correct amount from the total
    [HideInInspector] public float ingredientPrice;
}