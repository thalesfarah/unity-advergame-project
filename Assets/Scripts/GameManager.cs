using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Static reference to the instance, allowing other scripts to access it easily (e.g., GameManager.gameManager)
    public static GameManager gameManager;

    // Defines the possible states of the game flow
    public enum GameState
    {
        startedOrdering,              // Initial state when the player opens the menu
        choosingIngredients,          // Active when the player is assembling a food item
        ingredientsSelectionFinished, // When one item is finished but the order isn't placed yet
        orderFinished,                // When the player successfully completes the purchase
        resetOrdering                 // Used to clear data and return to the initial state
    }

    // Tracks the current active state of the game
    public GameState currentGameState;

    private void Awake()
    {
        // SINGLETON PATTERN
        // Ensures only one instance of GameManager exists across all scenes
        if (gameManager == null)
        {
            gameManager = this;
            // Prevents the object from being destroyed when loading new scenes
            DontDestroyOnLoad(gameObject);

            // Set the initial state of the game
            currentGameState = GameState.startedOrdering;
        }
        else
        {
            // If another instance already exists, destroy this one to avoid duplicates
            Destroy(gameObject);
        }
    }

    // Updates the global game state and logs the change for debugging
    public void ChangeState(GameState newState)
    {
        currentGameState = newState;
        Debug.Log("Game State changed to: " + newState);
    }

    // Closes the application (works in built versions of the game)
    public void QuitGame()
    {
        Application.Quit();
    }
}