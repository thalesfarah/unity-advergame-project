using UnityEngine;

public class IngredientSocket : MonoBehaviour
{
    public Transform socketTransform;

    [HideInInspector] public int myLayer;
    [HideInInspector] public float ingredientPrice; // Armazena o preço que este item custou ao ser criado
}