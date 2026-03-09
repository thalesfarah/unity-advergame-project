using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager gameManager;

    public enum GameState 
    {
        startedOrdering,
        choosingIngredients,
        ingredientsSelectionFinished,
        orderFinished,
        resetOrdering
    }
    public GameState currentGameState;

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = this;
            DontDestroyOnLoad(gameObject);
            currentGameState = GameState.startedOrdering;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ChangeState(GameState newState)
    {
        currentGameState = newState;
        Debug.Log("Estado alterado para: " + newState);
    }
    public void QuitGame() 
    {
        Application.Quit();
    }
}