using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UIElements;

    public class MainMenuNetworkUI : MonoBehaviour
    {
        public static string PlayerNickname = "Player";
        public static string JoinCodeText = "";

        [Header("UI Adicional")]
        public GameObject controlsImageObject; // La imagen de controles que se mostrará al pulsar "Opciones"

        // Contenedores
        private VisualElement mainMenuContainer;
        private VisualElement roleSelectionContainer;
        private VisualElement connectionContainer;

        // Botones principales (MainMenuContainer)
        private Button btnPlay;
        private Button btnOptions;
        private Button btnCredits;
        private Button btnExit;

        // Botón para cerrar las opciones
        private Button btnCerrarOpciones;

        // Botones de selección de rol (RoleSelectionContainer)
        private Button btnStartHost;
        private Button btnStartClient;
        private Button btnBackRole;

        // Botones de conexión final (ConnectionContainer)
        private Button btnFinalStart;
        private Button btnBackConnection;

        // Campos de texto
        private TextField inputNickname;
        private TextField inputJoinCode;

        // Etiqueta para mostrar los errores
        private Label lblError;

        private bool isConnectingAsHost;

        void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            var root = uiDocument.rootVisualElement;

            // 1. Encontrar contenedores
            mainMenuContainer = root.Q<VisualElement>("MainMenuContainer");
            roleSelectionContainer = root.Q<VisualElement>("RoleSelectionContainer");
            connectionContainer = root.Q<VisualElement>("ConnectionContainer");

            // 2. Encontrar botones del menú principal
            btnPlay = root.Q<Button>("BtnPlay");
            btnOptions = root.Q<Button>("BtnOptions");
            btnCredits = root.Q<Button>("BtnCredits");
            btnExit = root.Q<Button>("BtnExit");

            // Botón nuevo para cerrar las opciones (Asegúrate de crearlo en el UI Builder dentro de tu panel de controles si lo quieres)
            btnCerrarOpciones = root.Q<Button>("BtnCerrarOpciones");

            // 3. Encontrar botones de rol
            btnStartHost = root.Q<Button>("BtnStartHost");
            btnStartClient = root.Q<Button>("BtnStartClient");
            btnBackRole = root.Q<Button>("BtnBackRole");

            // 4. Encontrar botones y campos de conexión
            btnFinalStart = root.Q<Button>("BtnFinalStart");
            btnBackConnection = root.Q<Button>("BtnBackConnection");
            inputNickname = root.Q<TextField>("InputNickname");
            inputJoinCode = root.Q<TextField>("InputJoinCode");
            lblError = root.Q<Label>("LblError");

            // Configurar el nombre guardado
            SetNicknameLimit(12);
            if (inputNickname != null && PlayerPrefs.HasKey("PlayerName"))
            {
                inputNickname.value = PlayerPrefs.GetString("PlayerName");
            }

            // Suscribir eventos (Menú Principal)
            if (btnPlay != null) btnPlay.clicked += OnPlayClicked;
            if (btnOptions != null) btnOptions.clicked += OnOptionsClicked;
            if (btnCredits != null) btnCredits.clicked += OnCreditsClicked;
            if (btnExit != null) btnExit.clicked += OnExitClicked;
            if (btnCerrarOpciones != null) btnCerrarOpciones.clicked += OnCerrarOpcionesClicked;

            // Suscribir eventos (Roles y Conexión)
            if (btnStartHost != null) btnStartHost.clicked += OnHostSelected;
            if (btnStartClient != null) btnStartClient.clicked += OnClientSelected;
            if (btnFinalStart != null) btnFinalStart.clicked += OnFinalStartClicked;
            if (btnBackRole != null) btnBackRole.clicked += OnBackRoleClicked;
            if (btnBackConnection != null) btnBackConnection.clicked += OnBackConnectionClicked;

            ShowContainer(mainMenuContainer);
            ClearError();
        }

        async void Start()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
            }
            await AuthenticateToUnityAsync();

            UnityEngine.Cursor.lockState = CursorLockMode.None; // Desbloquea el ratón del centro
            UnityEngine.Cursor.visible = true;
        }

        private async Task AuthenticateToUnityAsync()
        {
            try
            {
                await UnityServices.InitializeAsync();
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    Debug.Log($"Conectado a Unity Cloud con ID: {AuthenticationService.Instance.PlayerId}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error al iniciar sesión en Unity Cloud: " + e.Message);
            }
        }

        void OnDisable()
        {
            if (btnPlay != null) btnPlay.clicked -= OnPlayClicked;
            if (btnOptions != null) btnOptions.clicked -= OnOptionsClicked;
            if (btnCredits != null) btnCredits.clicked -= OnCreditsClicked;
            if (btnExit != null) btnExit.clicked -= OnExitClicked;
            if (btnCerrarOpciones != null) btnCerrarOpciones.clicked -= OnCerrarOpcionesClicked;

            if (btnStartHost != null) btnStartHost.clicked -= OnHostSelected;
            if (btnStartClient != null) btnStartClient.clicked -= OnClientSelected;
            if (btnFinalStart != null) btnFinalStart.clicked -= OnFinalStartClicked;

            if (btnBackRole != null) btnBackRole.clicked -= OnBackRoleClicked;
            if (btnBackConnection != null) btnBackConnection.clicked -= OnBackConnectionClicked;

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
            }
        }

        void Update()
        {
            // Usamos el Input de toda la vida para detectar la tecla ESC
            if (controlsImageObject != null && controlsImageObject.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            {
                controlsImageObject.SetActive(false);
            }
        }

        private void SetNicknameLimit(int maxLength)
        {
            if (inputNickname != null)
            {
                inputNickname.maxLength = maxLength;
            }
        }

        // --- LÓGICA DE BOTONES DEL MENÚ PRINCIPAL ---

        private void OnPlayClicked()
        {
            ClearError();
            ShowContainer(roleSelectionContainer);
        }

        private void OnOptionsClicked()
        {
            if (controlsImageObject != null)
            {
                controlsImageObject.SetActive(true);
            }
        }

        private void OnCerrarOpcionesClicked()
        {
            if (controlsImageObject != null)
            {
                controlsImageObject.SetActive(false);
            }
        }

        private void OnCreditsClicked()
        {
            Debug.Log("Abriendo menú de Créditos...");
        }

        private void OnExitClicked()
        {
            Debug.Log("Cerrando el juego...");
            Application.Quit();
        }

        // --- LÓGICA DE ROLES Y NAVEGACIÓN ---

        private void OnHostSelected()
        {
            ClearError();
            isConnectingAsHost = true;
            if (inputJoinCode != null) inputJoinCode.style.display = DisplayStyle.None;
            ShowContainer(connectionContainer);
        }

        private void OnClientSelected()
        {
            ClearError();
            isConnectingAsHost = false;
            if (inputJoinCode != null) inputJoinCode.style.display = DisplayStyle.Flex;
            ShowContainer(connectionContainer);
        }

        private void OnBackRoleClicked()
        {
            ClearError();
            ShowContainer(mainMenuContainer);
        }

        private void OnBackConnectionClicked()
        {
            ClearError();
            ShowContainer(roleSelectionContainer);
        }

        private void ShowContainer(VisualElement containerToShow)
        {
            if (mainMenuContainer != null) mainMenuContainer.style.display = DisplayStyle.None;
            if (roleSelectionContainer != null) roleSelectionContainer.style.display = DisplayStyle.None;
            if (connectionContainer != null) connectionContainer.style.display = DisplayStyle.None;

            if (containerToShow != null) containerToShow.style.display = DisplayStyle.Flex;
        }

        // --- SISTEMA DE ERRORES ---

        private void ShowError(string message)
        {
            if (lblError != null)
            {
                lblError.text = message;
                lblError.style.color = new StyleColor(Color.red);
                lblError.style.display = DisplayStyle.Flex;
            }
            Debug.LogError(message);
        }

        private void ClearError()
        {
            if (lblError != null)
            {
                lblError.text = "";
                lblError.style.display = DisplayStyle.None;
            }
        }

        private void OnClientDisconnectCallback(ulong clientId)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                string reason = NetworkManager.Singleton.DisconnectReason;
                NetworkManager.Singleton.Shutdown();

                if (string.IsNullOrEmpty(reason))
                {
                    ShowError("Fallo de red: El servidor no responde o el código es incorrecto.");
                }
                else
                {
                    ShowError($"Conexión rechazada: {reason}");
                }
            }
        }

        // --- LÓGICA DE RED (NETCODE + RELAY) ---

        private async void OnFinalStartClicked()
        {
            ClearError();

            if (string.IsNullOrWhiteSpace(inputNickname.value))
            {
                ShowError("Por favor, introduce tu nombre militar.");
                return;
            }

            if (!isConnectingAsHost && string.IsNullOrWhiteSpace(inputJoinCode?.value))
            {
                ShowError("Introduce el Código de Sala para unirte.");
                return;
            }

            PlayerNickname = inputNickname.value;
            PlayerPrefs.SetString("PlayerName", PlayerNickname);
            PlayerPrefs.Save();

            byte[] payloadData = Encoding.ASCII.GetBytes(PlayerNickname);
            NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadData;

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

            if (isConnectingAsHost)
            {
                try
                {
                    lblError.text = "Asegurando base militar...";
                    lblError.style.color = new StyleColor(Color.yellow);
                    lblError.style.display = DisplayStyle.Flex;

                    int allocationSize = 3;

                    Allocation allocation = await RelayService.Instance.CreateAllocationAsync(allocationSize, "europe-west4");
                    string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                    JoinCodeText = joinCode;

                    transport.SetHostRelayData(
                        allocation.RelayServer.IpV4,
                        (ushort)allocation.RelayServer.Port,
                        allocation.AllocationIdBytes,
                        allocation.Key,
                        allocation.ConnectionData
                    );

                    if (NetworkManager.Singleton.StartHost())
                    {
                        Debug.LogWarning($"¡OPERACIÓN INICIADA! Tu código de escuadrón es: {joinCode}");
                        NetworkManager.Singleton.SceneManager.LoadScene("Main Scene", UnityEngine.SceneManagement.LoadSceneMode.Single);
                    }
                }
                catch (RelayServiceException e)
                {
                    ShowError($"Error al asegurar la base: {e.Message}");
                    NetworkManager.Singleton.Shutdown();
                }
            }
            else
            {
                try
                {
                    lblError.text = "Buscando señal aliada...";
                    lblError.style.color = new StyleColor(Color.yellow);
                    lblError.style.display = DisplayStyle.Flex;

                    JoinCodeText = inputJoinCode.value;

                    JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(inputJoinCode.value);

                    transport.SetClientRelayData(
                        joinAllocation.RelayServer.IpV4,
                        (ushort)joinAllocation.RelayServer.Port,
                        joinAllocation.AllocationIdBytes,
                        joinAllocation.Key,
                        joinAllocation.ConnectionData,
                        joinAllocation.HostConnectionData
                    );

                    NetworkManager.Singleton.StartClient();
                }
                catch (RelayServiceException)
                {
                    ShowError("Señal perdida: Código de sala incorrecto o escuadrón lleno.");
                    NetworkManager.Singleton.Shutdown();
                }
            }
        }
    }