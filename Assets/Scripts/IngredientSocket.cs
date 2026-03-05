using UnityEngine;

public class IngredientSocket : MonoBehaviour
{
    public Transform socketTransform; // Ponto onde o próximo ingrediente vai ser encaixado

    [HideInInspector]
    public int myLayer; // Esta variável será preenchida automaticamente pelo FoodManager ao instanciar
}