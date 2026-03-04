using UnityEngine;
public abstract class Food : MonoBehaviour
{
    public string categoryName;
    public float price;
    public GameObject foodPrefab;
    public Transform foodPrefabSpawnPos, nextFoodLayerPos;


}
