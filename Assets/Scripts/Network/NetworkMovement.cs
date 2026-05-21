using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(CharacterController))]
public class NetworkMovement : NetworkBehaviour
{
    [Header("Referencias Principales")]
    public CharacterController controller;
    public Animator animatorCuerpo;
    public Animator animatorBrazosFPS;
    public Transform camaraPivot;

    [Header("Sistemas del Jugador")]
    public SistemaEstaminaFPS sistemaEstamina;
    public ControladorArmasFPS controladorArmas;

    [Header("Objetos a apagar en enemigos")]
    public GameObject camaraPrincipal;
    public GameObject camaraArma;
    public GameObject objetoBrazosFPS;
    public GameObject armaManos;

    [Header("Configuración Visual")]
    public SkinnedMeshRenderer[] mallasCuerpoTerceraPersona;

    [Header("Físicas y Movimiento")]
    public float velocidadCaminar = 2.5f;
    public float velocidadCorrer = 5.0f;
    public float velocidadAgachado = 1.5f;
    public float velocidadTumbado = 0.8f;
    public float velocidadApuntando = 1.2f;

    public float alturaSalto = 1.2f;
    public float gravedad = -15f;

    [Header("Detección de Suelo")]
    public Transform comprobadorSuelo;
    public float radioEsferaSuelo = 0.4f;
    public LayerMask capaSuelo;
    public bool estaEnSuelo;

    [Header("Físicas de Postura")]
    public float alturaNormal = 2f;
    public float alturaAgachadoFisico = 1f;
    public float alturaTumbadoFisico = 0.5f;

    [Header("Corrección de Animación TPS")]
    public Transform huesoColumna; // Aquí arrastrarás la espina dorsal de tu modelo
    public Vector3 compensacionApuntado = new Vector3(0f, 15f, 0f); // Grados a corregir

    private float alturaCamaraNormal;
    public float alturaCamaraAgachado = 0.5f;
    public float alturaCamaraTumbado = -0.2f;

    public float velocidadTransicionPostura = 8f;

    private Vector3 velocidadCaida;

    // --- VARIABLES DE ESTADO UNIFICADO ---
    // Guardamos la decisión global en vez de calcularla 3 veces distintas
    private float inputX;
    private float inputZ;
    private bool inputApuntando;
    private bool inputDisparando;
    private bool quiereCorrer;

    private bool estaAgachado = false;
    private bool estaTumbado = false;
    [HideInInspector] public bool esprintandoRealmente = false;

    public NetworkVariable<float> inclinacionRed = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    // Hashes de Animator
    private readonly int inputXHash = Animator.StringToHash("InputX");
    private readonly int inputZHash = Animator.StringToHash("InputZ");
    private readonly int isSprintingHash = Animator.StringToHash("IsSprinting");
    private readonly int isCrouchingHash = Animator.StringToHash("IsCrouching");
    private readonly int isProneHash = Animator.StringToHash("IsProne");
    private readonly int isAimingHash = Animator.StringToHash("IsAiming");
    private readonly int triggerShootHash = Animator.StringToHash("TriggerShoot");

    public override void OnNetworkSpawn()
    {
        // 1. LA CURA DEL FANTASMA: Buscamos la cabeza para TODOS los ordenadores
        if (camaraPivot == null)
        {
            // Buscamos a tu hijo llamado "Camera Root" (como sale en tu captura)
            Transform root = transform.Find("Camera Root");

            // Si lo encuentra se lo asigna. Si no, usa el cuerpo entero como plan B.
            camaraPivot = root != null ? root : transform;
        }

        // 2. Lógica exclusiva del dueño de este jugador
        if (IsOwner)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Ahora es seguro leer la altura porque sabemos que camaraPivot existe sí o sí
            alturaCamaraNormal = camaraPivot.localPosition.y;

            foreach (SkinnedMeshRenderer malla in mallasCuerpoTerceraPersona)
            {
                if (malla != null) malla.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
        }
        else
        {
            // Lógica para los clones de tus amigos (se apagan sus cámaras)
            if (camaraPrincipal != null) camaraPrincipal.SetActive(false);
            if (camaraArma != null) camaraArma.SetActive(false);
            if (objetoBrazosFPS != null) objetoBrazosFPS.SetActive(false);
            if (armaManos != null) armaManos.SetActive(false);
        }
    }

    // --- LA ESTRUCTURA AAA DEL UPDATE ---
    void Update()
    {
        if (IsOwner)
        {
            RecogerInputsYPosturas(); // 1. Leemos el ratón y teclado
            ResolverEstamina();       // 2. Decidimos qué puede y no puede hacer
            ComprobarSuelo();         // 3. Chequeamos la gravedad
            MoverJugador();           // 4. Movemos la cápsula
            LogicaPostura();          // 5. Agachamos/Tumbamos el cuerpo
            AnimarJugadores();        // 6. Lanzamos las animaciones

            if (camaraPivot != null) inclinacionRed.Value = camaraPivot.localEulerAngles.x;
        }
        else
        {
            if (camaraPivot != null)
            {
                camaraPivot.localRotation = Quaternion.Euler(inclinacionRed.Value, 0f, 0f);
            }
        }
    }

    private void LateUpdate()
    {
        // LateUpdate se ejecuta DESPUÉS de que el Animator coloque los huesos.
        // Aquí forzamos una corrección manual.
        if (huesoColumna != null && animatorCuerpo != null)
        {
            // Si el Animator dice que este jugador está apuntando...
            if (animatorCuerpo.GetBool("IsAiming"))
            {
                // Le giramos la cintura los grados que necesitemos
                huesoColumna.Rotate(compensacionApuntado, Space.Self);
            }
        }
    }

    void RecogerInputsYPosturas()
    {
        if (InGameMenu.MenuAbierto) return;
        inputX = Input.GetAxis("Horizontal");
        inputZ = Input.GetAxis("Vertical");
        quiereCorrer = Input.GetKey(KeyCode.LeftShift);
        inputApuntando = Input.GetMouseButton(1);
        inputDisparando = Input.GetMouseButtonDown(0);

        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            if (estaTumbado)
            {
                estaTumbado = false;
                estaAgachado = true;
            }
            else estaAgachado = !estaAgachado;
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (estaTumbado) estaTumbado = false;
            else
            {
                estaAgachado = false;
                estaTumbado = true;
            }
        }

        // Auto-levantarse
        if (quiereCorrer && inputZ > 0 && !inputApuntando)
        {
            estaAgachado = false;
            estaTumbado = false;
        }
    }

    void ResolverEstamina()
    {
        // Miramos si la estamina nos da permiso
        bool tieneEstamina = (sistemaEstamina == null || sistemaEstamina.puedeCorrer);

        // Calculamos la verdad absoluta
        esprintandoRealmente = quiereCorrer && inputZ > 0 && !inputApuntando && !estaAgachado && !estaTumbado && tieneEstamina;

        if (sistemaEstamina != null)
        {
            if (esprintandoRealmente)
                sistemaEstamina.ConsumirEstamina();
            else
                sistemaEstamina.RegenerarEstamina();
        }
    }

    void ComprobarSuelo()
    {
        estaEnSuelo = Physics.CheckSphere(comprobadorSuelo.position, radioEsferaSuelo, capaSuelo);
        if (estaEnSuelo && velocidadCaida.y < 0) velocidadCaida.y = -2f;
    }

    void MoverJugador()
    {
        float velocidadActual = velocidadCaminar;
        bool estaRecargando = controladorArmas != null && controladorArmas.estaRecargando;

        if (InGameMenu.MenuAbierto) return;

        if (estaTumbado) velocidadActual = velocidadTumbado;
        else if (estaAgachado) velocidadActual = velocidadAgachado;
        else if (inputApuntando) velocidadActual = velocidadApuntando;
        else if (esprintandoRealmente) velocidadActual = velocidadCorrer;

        if (estaRecargando && !estaAgachado && !estaTumbado)
        {
            velocidadActual = velocidadCaminar * 0.7f;
        }

        Vector3 inputDir = new Vector3(inputX, 0f, inputZ);
        inputDir = Vector3.ClampMagnitude(inputDir, 1f);

        Vector3 move = transform.right * inputDir.x + transform.forward * inputDir.z;
        controller.Move(move * velocidadActual * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && estaEnSuelo && !estaAgachado && !estaTumbado)
        {
            velocidadCaida.y = Mathf.Sqrt(alturaSalto * -2f * gravedad);
        }

        velocidadCaida.y += gravedad * Time.deltaTime;
        controller.Move(velocidadCaida * Time.deltaTime);
    }

    void LogicaPostura()
    {
        float alturaObjetivo = estaTumbado ? alturaTumbadoFisico : (estaAgachado ? alturaAgachadoFisico : alturaNormal);
        float centroYObjetivo = alturaObjetivo / 2f;
        float camaraYObjetivo = estaTumbado ? alturaCamaraTumbado : (estaAgachado ? alturaCamaraAgachado : alturaCamaraNormal);

        controller.height = Mathf.Lerp(controller.height, alturaObjetivo, Time.deltaTime * velocidadTransicionPostura);
        controller.center = new Vector3(0, Mathf.Lerp(controller.center.y, centroYObjetivo, Time.deltaTime * velocidadTransicionPostura), 0);

        Vector3 posCamara = camaraPivot.localPosition;
        posCamara.y = Mathf.Lerp(posCamara.y, camaraYObjetivo, Time.deltaTime * velocidadTransicionPostura);
        camaraPivot.localPosition = posCamara;
    }

    void AnimarJugadores()
    {
        float animZ = inputZ * (esprintandoRealmente ? 2f : 1f);

        // Usamos la variable global unificada
        MandarParametrosAnimator(animatorCuerpo, inputX, animZ, esprintandoRealmente, inputApuntando, inputDisparando);
        //MandarParametrosAnimator(animatorBrazosFPS, inputX, animZ, esprintandoRealmente, inputApuntando, inputDisparando);
    }

    void MandarParametrosAnimator(Animator anim, float x, float z, bool sprint, bool apuntando, bool disparando)
    {
        if (anim == null) return;
        anim.SetFloat(inputXHash, x, 0.1f, Time.deltaTime);
        anim.SetFloat(inputZHash, z, 0.1f, Time.deltaTime);

        // Ya no calculamos nada aquí. Si llega "true", es que TODO es correcto.
        anim.SetBool(isSprintingHash, sprint);
        anim.SetBool(isCrouchingHash, estaAgachado);
        anim.SetBool(isProneHash, estaTumbado);
        anim.SetBool(isAimingHash, apuntando);
        if (disparando) anim.SetTrigger(triggerShootHash);
    }
}