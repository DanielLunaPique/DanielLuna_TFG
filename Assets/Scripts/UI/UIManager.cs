using UnityEngine;
using TMPro;
using Unity.Netcode;

// Recuerda: Es un MonoBehaviour normal para que Netcode no lo bloquee
public class UIManager : MonoBehaviour
{
    [Header("Textos de UI")]
    public TextMeshProUGUI textoPuntos;
    public TextMeshProUGUI textoRonda;

    [Header("Modo Espectador")]
    public GameObject panelEspectador;
    public TextMeshProUGUI textoNombreEspectador;

    [Header("Interacción")]
    public TextMeshProUGUI textoInteraccion;

    [Header("Tienda Médica")]
    public GameObject panelTiendaMedica;
    public BotonRevivirUI[] botonesRevivir; // Aquí arrastrarás los 4 botones

    [Header("Easter Egg HUD")]
    public TextMeshProUGUI textoObjetivo;

    // --- VARIABLES DE CONTROL INTERNO ---
    private SistemaPuntosFPS bolsillo;
    [HideInInspector] public bool menuTiendaAbierto = false;
    [HideInInspector] public TiendaMedica tiendaActual;

    // Variables para bloquear al jugador
    private NetworkMovement movJugador;
    private ControladorCamaraFPS camJugador;

    public static UIManager Instance;


    private void Start()
    {
        // 1. Comprobación de Propiedad (Solo el dueño de este cuerpo usa esta UI)
        NetworkObject netObj = GetComponentInParent<NetworkObject>();

        if (netObj != null && !netObj.IsOwner)
        {
            gameObject.SetActive(false);
            return;
        }

        Instance = this; 

        // 2. Buscar referencias en la RAÍZ del jugador (Para encontrar todo sin fallos)
        if (netObj != null)
        {
            Transform raizJugador = netObj.transform;
            movJugador = raizJugador.GetComponentInChildren<NetworkMovement>(true);
            camJugador = raizJugador.GetComponentInChildren<ControladorCamaraFPS>(true);
        }

        // 3. Conectar Sistema de Puntos
        bolsillo = GetComponentInParent<SistemaPuntosFPS>();
        if (bolsillo != null)
        {
            bolsillo.puntos.OnValueChanged += ActualizarTextoPuntos;
            ActualizarTextoPuntos(0, bolsillo.puntos.Value);
        }

        // 4. Conectar Sistema de Rondas
        if (GameManager.Instance != null)
        {
            GameManager.Instance.rondaActual.OnValueChanged += ActualizarTextoRonda;
            ActualizarTextoRonda(0, GameManager.Instance.rondaActual.Value);
        }

        // 5. Apagar los paneles que deben empezar ocultos
        if (textoInteraccion != null) textoInteraccion.gameObject.SetActive(false);
        if (panelEspectador != null) panelEspectador.SetActive(false);
        if (panelTiendaMedica != null) panelTiendaMedica.SetActive(false);
    }

    // ==========================================
    // ACTUALIZACIÓN DE TEXTOS BÁSICOS
    // ==========================================
    private void ActualizarTextoPuntos(int valorViejo, int valorNuevo)
    {
        if (textoPuntos != null) textoPuntos.text = $"$ {valorNuevo}";
    }

    private void ActualizarTextoRonda(int rondaAnterior, int rondaNueva)
    {
        if (textoRonda != null) textoRonda.text = $"{rondaNueva}";
    }

    public void MostrarTextoInteraccion(string mensaje)
    {
        if (textoInteraccion != null)
        {
            textoInteraccion.text = mensaje;
            textoInteraccion.gameObject.SetActive(true);
        }
    }

    public void OcultarTextoInteraccion()
    {
        if (textoInteraccion != null) textoInteraccion.gameObject.SetActive(false);
    }

    public void ActualizarTextoObjetivo(string nuevoTexto)
    {
        if (textoObjetivo != null)
        {
            textoObjetivo.text = nuevoTexto;
        }
    }

    // ==========================================
    // MODO ESPECTADOR
    // ==========================================
    public void MostrarHUDModuloEspectador(string nombreJugador)
    {
        if (panelEspectador != null) panelEspectador.SetActive(true);
        if (textoNombreEspectador != null) textoNombreEspectador.text = "OBSERVANDO A: " + nombreJugador;
    }

    public void OcultarHUDModuloEspectador()
    {
        if (panelEspectador != null) panelEspectador.SetActive(false);
    }

    // ==========================================
    // TIENDA MÉDICA (REVIVIR)
    // ==========================================
    public void AbrirMenuTiendaMedica()
    {
        if (panelTiendaMedica == null) return;

        // 1. Encendemos el panel principal
        panelTiendaMedica.SetActive(true);
        menuTiendaAbierto = true;

        // 2. Liberamos el ratón para poder hacer clic
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 3. Bloqueamos el movimiento, la cámara y paramos las piernas
        if (movJugador != null) movJugador.enabled = false;
        if (camJugador != null) camJugador.enabled = false;

        // 4. Apagamos todos los botones por defecto antes de comprobar los muertos
        foreach (BotonRevivirUI boton in botonesRevivir)
        {
            if (boton != null) boton.gameObject.SetActive(false);
        }

        // 5. Buscamos a los muertos y encendemos sus botones
        SaludJugador[] todosLosJugadores = FindObjectsOfType<SaludJugador>();
        int indiceBoton = 0;
        int costeActualRevivir = (tiendaActual != null) ? tiendaActual.precioActualRevivir.Value : 500;

        foreach (SaludJugador jugador in todosLosJugadores)
        {
            if (jugador.estaMuerto)
            {
                // Si aún nos quedan botones libres en la pantalla...
                if (indiceBoton < botonesRevivir.Length)
                {
                    ulong suID = jugador.OwnerClientId;
                    string suNombre = "Jugador " + suID;

                    // Configuramos el botón (esto lo enciende y resetea su barra verde
                    botonesRevivir[indiceBoton].ConfigurarBoton(suID, suNombre, costeActualRevivir, this);

                    indiceBoton++;
                }
            }
        }
    }

    public void EjecutarCompraRevivir(ulong idDelMuerto, int costeCompra)
    {
        Debug.LogWarning($"¡Compra confirmada! Solicitando revivir al jugador {idDelMuerto} por {costeCompra} puntos.");

        CerrarMenuTiendaMedica();

        // Comprobamos si el cable sigue conectado
        if (tiendaActual != null)
        {
            Debug.Log("Enviando el ServerRpc al servidor...");
            tiendaActual.SolicitarRevivirServerRpc(NetworkManager.Singleton.LocalClientId, idDelMuerto, costeCompra);
        }
        else
        {
            // SI SALE ESTE ERROR: El problema es el Box Collider
            Debug.LogError("¡ERROR FATAL! tiendaActual es NULO. El UIManager no sabe a qué tienda avisar.");
        }
    }

    public void CerrarMenuTiendaMedica()
    {
        if (panelTiendaMedica != null) panelTiendaMedica.SetActive(false);
        menuTiendaAbierto = false;

        // Volvemos a ocultar y bloquear el ratón
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Devolvemos el control de la cámara y el movimiento al jugador
        if (movJugador != null) movJugador.enabled = true;
        if (camJugador != null) camJugador.enabled = true;
    }

    // ==========================================
    // LIMPIEZA DE MEMORIA
    // ==========================================
    private void OnDestroy()
    {
        if (bolsillo != null) bolsillo.puntos.OnValueChanged -= ActualizarTextoPuntos;
        if (GameManager.Instance != null) GameManager.Instance.rondaActual.OnValueChanged -= ActualizarTextoRonda;
    }
}