using UnityEngine;
using UnityEngine.Animations;

public class ControladorArmasFPS : MonoBehaviour
{
    [Header("Gestión de Armas")]
    public DatosArma armaActual;
    public Transform fantasmaManoIzquierda;
    public Transform fantasmaManoDerecha;

    [Header("Configuración de Balanceo (Sway)")]
    public float intensidadSway = 2f;
    public float swayMaximo = 3f;

    [Header("Configuración de Caminar (Bobbing)")]
    public float velocidadBobbing = 14f;
    public float cantidadBobbing = 0.02f;

    [Header("Posición Base (Cadera)")]
    public Vector3 posicionBase = new Vector3(0, 0, 0);

    // Variables privadas
    private Vector3 posicionObjetivo;
    private Quaternion rotacionObjetivo;
    private float tiempoBobbing;
    private bool estaApuntando;

    // Variables donde guardaremos el cálculo del Nodo de Mira
    private Vector3 adsPosicionObjetivo;
    private Quaternion adsRotacionObjetivo;

    // Variables de Retroceso Procedural
    private Vector3 recoilPosicionActual;
    private Quaternion recoilRotacionActual;

    [HideInInspector] public float multiplicadorDispersion = 1f;
    [HideInInspector] public float penalizacionDisparo = 0f;
    [HideInInspector] public bool estaRecargando = false;
    public NetworkMovement movimiento;

    void Start()
    {
        // Por ahora lo calculamos al darle al Play. 
        // En el futuro, llamaremos a esto desde el script de Inventario al cambiar de arma.
        RecalcularPuntoDeMira();
    }

    // NUEVA FUNCIÓN: La matemática mágica
    public void RecalcularPuntoDeMira()
    {
        if (armaActual == null || armaActual.puntoDeMira == null) return;

        // 1. Guardamos la postura de la cadera
        Vector3 posOriginal = transform.localPosition;
        Quaternion rotOriginal = transform.localRotation;

        // 2. Centramos el contenedor en 0,0,0
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // 3. Calculamos la posición y rotación LOCALES de la mirilla respecto al contenedor
        Vector3 posMiraLocal = transform.InverseTransformPoint(armaActual.puntoDeMira.position);
        Quaternion rotMiraLocal = Quaternion.Inverse(transform.rotation) * armaActual.puntoDeMira.rotation;

        // 4. LA MATEMÁTICA CORRECTA: Invertimos rotación y rotamos la posición negativa
        adsRotacionObjetivo = Quaternion.Inverse(rotMiraLocal);
        adsPosicionObjetivo = adsRotacionObjetivo * (-posMiraLocal);

        // 5. Devolvemos el arma a la cadera
        transform.localPosition = posOriginal;
        transform.localRotation = rotOriginal;
    }

    public void ActualizarAgarresIK()
    {
        if (armaActual == null) return;

        // Comprobamos que tenemos el fantasma y el agarre nuevo
        if (fantasmaManoIzquierda != null && armaActual.agarreManoIzquierda != null)
        {
            ParentConstraint constraintIzq = fantasmaManoIzquierda.GetComponent<ParentConstraint>();

            if (constraintIzq != null)
            {
                // 1. Borramos cualquier arma antigua que estuviera agarrando
                while (constraintIzq.sourceCount > 0)
                {
                    constraintIzq.RemoveSource(0);
                }

                // 2. Le decimos que su nuevo "padre" es el agarre de la nueva arma
                ConstraintSource nuevaFuente = new ConstraintSource();
                nuevaFuente.sourceTransform = armaActual.agarreManoIzquierda;
                nuevaFuente.weight = 1f;

                constraintIzq.AddSource(nuevaFuente);

                // 3. Forzamos a que la distancia sea 0 (pegado al milímetro)
                constraintIzq.SetTranslationOffset(0, Vector3.zero);
                constraintIzq.SetRotationOffset(0, Vector3.zero);
            }
        }
    }

    void Update()
    {
        if (armaActual == null) return;

        penalizacionDisparo = Mathf.Lerp(penalizacionDisparo, 0f, Time.deltaTime * 5f);

        if (adsRotacionObjetivo == new Quaternion(0, 0, 0, 0))
        {
            RecalcularPuntoDeMira();
        }

        CalcularSway();
        CalcularBobbingYEstados();

        // 1. Aplicamos el movimiento al Contenedor
        // --- EL EFECTO MUELLE DEL RETROCESO ---
        // Hacemos que el retroceso vuelva a cero rápidamente (Time.deltaTime * 15f es la velocidad de recuperación)
        recoilPosicionActual = Vector3.Lerp(recoilPosicionActual, Vector3.zero, Time.deltaTime * 15f);
        recoilRotacionActual = Quaternion.Slerp(recoilRotacionActual, Quaternion.identity, Time.deltaTime * 15f);

        // --- APLICAR MOVIMIENTO FINAL ---
        // Sumamos la posición/rotación base con el golpe de retroceso actual
        Vector3 posicionFinal = posicionObjetivo + recoilPosicionActual;
        Quaternion rotacionFinal = rotacionObjetivo * recoilRotacionActual;

        transform.localPosition = Vector3.Lerp(transform.localPosition, posicionFinal, Time.deltaTime * 8f);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, rotacionFinal, Time.deltaTime * 8f);
    }

    void CalcularSway()
    {
        float multiplicadorApuntado = estaApuntando ? 0.1f : 1f;

        float ratonX = -Input.GetAxis("Mouse X") * intensidadSway * multiplicadorApuntado;
        float ratonY = -Input.GetAxis("Mouse Y") * intensidadSway * multiplicadorApuntado;

        ratonX = Mathf.Clamp(ratonX, -swayMaximo, swayMaximo);
        ratonY = Mathf.Clamp(ratonY, -swayMaximo, swayMaximo);

        Quaternion rotacionX = Quaternion.AngleAxis(ratonY, Vector3.right);
        Quaternion rotacionY = Quaternion.AngleAxis(ratonX, Vector3.up);

        rotacionObjetivo = rotacionX * rotacionY;
    }

    void CalcularBobbingYEstados()
    {
        estaApuntando = Input.GetMouseButton(1);
        bool estaCorriendo = movimiento.esprintandoRealmente;
        bool estaAgachado = Input.GetKey(KeyCode.LeftControl); 

        // --- CALCULAMOS LA PRECISIÓN SEGÚN EL ESTADO ---
        float inputMovimiento = Mathf.Abs(Input.GetAxis("Horizontal")) + Mathf.Abs(Input.GetAxis("Vertical"));

        if (estaApuntando) multiplicadorDispersion = 0.1f; // Casi un láser
        else if (estaCorriendo) multiplicadorDispersion = 2.5f; // Súper impreciso
        else if (estaAgachado && inputMovimiento < 0.1f) multiplicadorDispersion = 0.5f; // Agachado quieto: muy preciso
        else if (inputMovimiento > 0.1f) multiplicadorDispersion = 1.5f; // Caminando: pierde precisión
        else multiplicadorDispersion = 1f; // De pie, quieto (Cadera base)

        if (estaApuntando)
        {
            // ESTADO: APUNTANDO (Usamos lo que calculó nuestra función del Nodo de Mira)
            posicionObjetivo = adsPosicionObjetivo;
            rotacionObjetivo *= adsRotacionObjetivo;
        }

        else if (estaCorriendo)
        {
            // ESTADO: SPRINT CLÁSICO (Old Gen CoD)
            posicionObjetivo = armaActual.posicionSprint;
            rotacionObjetivo *= Quaternion.Euler(armaActual.rotacionSprint);

            // Aumentamos el tiempo más rápido que al caminar, pero sin ser exagerado
            tiempoBobbing += Time.deltaTime * (velocidadBobbing * 0.6f);

            // Usamos curvas suaves continuas (Seno y Coseno normales, sin valor absoluto)
            // Multiplicamos por 2 el tiempo en la Y para que haga el clásico símbolo de infinito (8 acostado)
            float curvaBobbingY = Mathf.Sin(tiempoBobbing * 2f) * (cantidadBobbing * 1.5f);
            float curvaBobbingX = Mathf.Cos(tiempoBobbing) * (cantidadBobbing * 2f);

            // Aplicamos la posición
            posicionObjetivo += new Vector3(curvaBobbingX, curvaBobbingY, 0f);

            // Añadimos una rotación súper sutil para que el cañón tenga inercia y peso
            rotacionObjetivo *= Quaternion.Euler(-curvaBobbingY * 15f, curvaBobbingX * 15f, curvaBobbingX * 5f);
        }

        else
        {
            // ESTADO: CADERA
            float inputMovimientoPlayer = Mathf.Abs(Input.GetAxis("Horizontal")) + Mathf.Abs(Input.GetAxis("Vertical"));
            inputMovimientoPlayer = Mathf.Clamp01(inputMovimientoPlayer);

            if (inputMovimientoPlayer > 0.1f)
            {
                tiempoBobbing += Time.deltaTime * velocidadBobbing;
                float curvaBobbingY = Mathf.Sin(tiempoBobbing) * cantidadBobbing;
                float curvaBobbingX = Mathf.Cos(tiempoBobbing / 2f) * cantidadBobbing;

                posicionObjetivo = posicionBase + new Vector3(curvaBobbingX, curvaBobbingY, 0f);
            }
            else
            {
                tiempoBobbing = 0;
                posicionObjetivo = posicionBase;
            }
        }
    }
    public void AplicarRetroceso()
    {
        if (armaActual == null) return;

        // 1. Creamos un multiplicador. Si estamos apuntando es 0.5 (la mitad), si no, es 1 (normal).
        float multiplicadorApuntado = estaApuntando ? 0.5f : 1f;

        // 2. Damos el "golpe" hacia atrás, multiplicándolo por nuestro freno
        recoilPosicionActual += new Vector3(0, 0, armaActual.retrocesoZ * multiplicadorApuntado);

        // 3. Hacemos lo mismo con la rotación (para que el cañón tampoco suba tanto al apuntar)
        float desvioAleatorio = Random.Range(-armaActual.retrocesoRotacionYAleatoria, armaActual.retrocesoRotacionYAleatoria);
        recoilRotacionActual *= Quaternion.Euler(
            armaActual.retrocesoRotacionX * multiplicadorApuntado,
            desvioAleatorio * multiplicadorApuntado,
            0
        );
    }
}