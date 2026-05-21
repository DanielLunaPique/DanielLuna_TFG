using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class InGameMenu : MonoBehaviour
{
    [Header("UI Referencias")]
    [Tooltip("Arrastra aquí el panel o imagen que contiene todo tu menú de pausa")]
    public GameObject panelMenu;

    [Tooltip("Arrastra aquí el TextMeshPro donde saldrá el código")]
    public TextMeshProUGUI textoCodigoSala;

    // Variable global estática para bloquear al jugador
    public static bool MenuAbierto = false;

    void Start()
    {
        // 1. Asegurarnos de que el menú empieza cerrado al cargar la escena
        MenuAbierto = false;
        if (panelMenu != null) panelMenu.SetActive(false);

        // 2. Rescatar el código de sala del menú principal
        if (textoCodigoSala != null)
        {
            string codigo = MainMenuNetworkUI.JoinCodeText;
            if (string.IsNullOrEmpty(codigo))
            {
                textoCodigoSala.text = "CÓDIGO: Oculto (Red Local/Host)";
            }
            else
            {
                textoCodigoSala.text = $"CÓDIGO DE UNIÓN DE SALA: {codigo}";
            }
        }
    }

    void Update()
    {
        // 3. Detectar Tabulador o Escape
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab))
        {
            AlternarMenu();
        }
    }

    public void AlternarMenu()
    {
        MenuAbierto = !MenuAbierto;

        if (panelMenu != null)
        {
            panelMenu.SetActive(MenuAbierto);
        }

        if (MenuAbierto)
        {
            // Liberar el ratón para poder clicar botones
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // Bloquear el ratón para seguir jugando
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // Esta función es la que tienes que enlazar al botón "Salir" del Canvas
    public void BotonSalirPartida()
    {
        Debug.Log("[InGameMenu] Saliendo al menú principal...");

        MenuAbierto = false; // Reseteamos la variable por si acaso

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        if (GameManager.Instance != null)
        {
            Destroy(GameManager.Instance.gameObject);
        }

        SceneManager.LoadScene("Main Menu");
    }
}