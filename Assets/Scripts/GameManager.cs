using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager gameManager;

    public enum GameState
    {
        startedOrdering,              // Menu inicial
        choosingIngredients,          // Montando o item atual
        ingredientsSelectionFinished, // Terminou de montar o item, mas não confirmou o pedido
        confirmingOrdering,           // Revisando o pedido (Painel de confirmação aberto)
        orderFinished,                // Compra finalizada com sucesso
        resetOrdering                 // Limpando dados para novo ciclo
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

        // Configuração de resolução para Build PC Portrait
        Screen.SetResolution(1080, 1920, false);
    }

    public void ChangeState(GameState newState)
    {
        currentGameState = newState;
        Debug.Log("Estado do Jogo alterado para: " + newState);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}