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

    [Header("Referencias de Audio")]
    public GestorPasosFPS gestorPasos;
    private float faseAnterior;

    [Header("Referencias de Movimiento")]
    public NetworkMovement movimientoRed;

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
        RecalcularPuntoDeMira();
    }

    public void RecalcularPuntoDeMira()
    {
        if (armaActual == null || armaActual.puntoDeMira == null) return;

        Vector3 posOriginal = transform.localPosition;
        Quaternion rotOriginal = transform.localRotation;

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        Vector3 posMiraLocal = transform.InverseTransformPoint(armaActual.puntoDeMira.position);
        Quaternion rotMiraLocal = Quaternion.Inverse(transform.rotation) * armaActual.puntoDeMira.rotation;

        adsRotacionObjetivo = Quaternion.Inverse(rotMiraLocal);
        adsPosicionObjetivo = adsRotacionObjetivo * (-posMiraLocal);

        transform.localPosition = posOriginal;
        transform.localRotation = rotOriginal;
    }

    public void ActualizarAgarresIK()
    {
        if (armaActual == null) return;

        if (fantasmaManoIzquierda != null && armaActual.agarreManoIzquierda != null)
        {
            ParentConstraint constraintIzq = fantasmaManoIzquierda.GetComponent<ParentConstraint>();

            if (constraintIzq != null)
            {
                while (constraintIzq.sourceCount > 0)
                {
                    constraintIzq.RemoveSource(0);
                }

                ConstraintSource nuevaFuente = new ConstraintSource();
                nuevaFuente.sourceTransform = armaActual.agarreManoIzquierda;
                nuevaFuente.weight = 1f;

                constraintIzq.AddSource(nuevaFuente);

                constraintIzq.SetTranslationOffset(0, Vector3.zero);
                constraintIzq.SetRotationOffset(0, Vector3.zero);
            }
        }
    }

    void Update()
    {
        if (armaActual == null) return;

        if (Cursor.lockState != CursorLockMode.Locked) return;

        // --- LA MAGIA: Comprobamos si estamos en un menú (ratón desbloqueado) ---
        bool enMenu = Cursor.lockState != CursorLockMode.Locked;

        penalizacionDisparo = Mathf.Lerp(penalizacionDisparo, 0f, Time.deltaTime * 5f);

        if (adsRotacionObjetivo == new Quaternion(0, 0, 0, 0))
        {
            RecalcularPuntoDeMira();
        }

        // Le pasamos el chivato a las funciones para que sepan qué hacer
        CalcularSway(enMenu);
        CalcularBobbingYEstados(enMenu);

        recoilPosicionActual = Vector3.Lerp(recoilPosicionActual, Vector3.zero, Time.deltaTime * 15f);
        recoilRotacionActual = Quaternion.Slerp(recoilRotacionActual, Quaternion.identity, Time.deltaTime * 15f);

        Vector3 posicionFinal = posicionObjetivo + recoilPosicionActual;
        Quaternion rotacionFinal = rotacionObjetivo * recoilRotacionActual;

        transform.localPosition = Vector3.Lerp(transform.localPosition, posicionFinal, Time.deltaTime * 8f);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, rotacionFinal, Time.deltaTime * 8f);

        bool seEstaMoviendo = (Mathf.Abs(Input.GetAxis("Horizontal")) + Mathf.Abs(Input.GetAxis("Vertical"))) > 0.1f;

        if (movimientoRed.estaEnSuelo && seEstaMoviendo)
        {
            // Al apuntar, los pasos suelen ser un poco más lentos (frecuencia 0.7f por ejemplo)
            float multiplicadorVelocidad = 1f;
            if (Input.GetMouseButton(1)) {

                multiplicadorVelocidad = 0.5f; // Apuntando es más pausado                         
                tiempoBobbing += Time.deltaTime * velocidadBobbing * multiplicadorVelocidad;
            } 

            // Llamamos a la detección de pasos
            DetectarPuntoDeImpacto();
        }
    }

    private void DetectarPuntoDeImpacto()
    {

        if (movimientoRed == null || !movimientoRed.estaEnSuelo) return;
        // Si estamos esprintando, el arma visual usa el doble de velocidad (* 2f).
        // Así que le decimos al sonido que lea esa misma curva rápida.
        float multiplicadorFase = movimiento.esprintandoRealmente ? 2f : 1f;

        // Calculamos el punto más bajo en base a la animación real
        float faseActual = Mathf.Sin(tiempoBobbing * multiplicadorFase);

        // Cuando el arma llega abajo del todo (-0.95), damos el paso
        if (faseActual < -0.95f && faseAnterior >= -0.95f)
        {
            if (gestorPasos != null)
            {
                gestorPasos.ReproducirSonidoPaso();
            }
        }

        faseAnterior = faseActual;
    }

    // Recibe el booleano 'enMenu'
    void CalcularSway(bool enMenu)
    {
        float multiplicadorApuntado = estaApuntando ? 0.2f : 1f;

        float ratonX = 0f;
        float ratonY = 0f;

        // Si NO estamos en el menú, leemos el ratón normalmente
        if (!enMenu)
        {
            ratonX = -Input.GetAxis("Mouse X") * intensidadSway * multiplicadorApuntado;
            ratonY = -Input.GetAxis("Mouse Y") * intensidadSway * multiplicadorApuntado;
        }

        ratonX = Mathf.Clamp(ratonX, -swayMaximo, swayMaximo);
        ratonY = Mathf.Clamp(ratonY, -swayMaximo, swayMaximo);

        Quaternion rotacionX = Quaternion.AngleAxis(ratonY, Vector3.right);
        Quaternion rotacionY = Quaternion.AngleAxis(ratonX, Vector3.up);

        rotacionObjetivo = rotacionX * rotacionY;
    }

    // Recibe el booleano 'enMenu'
    void CalcularBobbingYEstados(bool enMenu)
    {
        // Si estamos en el menú, forzamos todos los controles a "Falso/Cero"
        estaApuntando = enMenu ? false : Input.GetMouseButton(1);
        bool estaCorriendo = enMenu ? false : movimiento.esprintandoRealmente;
        bool estaAgachado = enMenu ? false : Input.GetKey(KeyCode.LeftControl); 
        float inputMovimientoPlayer = enMenu ? 0f : (Mathf.Abs(Input.GetAxis("Horizontal")) + Mathf.Abs(Input.GetAxis("Vertical")));

        if (estaApuntando) multiplicadorDispersion = 0.1f; 
        else if (estaCorriendo) multiplicadorDispersion = 2.5f; 
        else if (estaAgachado && inputMovimientoPlayer < 0.1f) multiplicadorDispersion = 0.5f; 
        else if (inputMovimientoPlayer > 0.1f) multiplicadorDispersion = 1.5f; 
        else multiplicadorDispersion = 1f; 

        if (estaApuntando)
        {
            posicionObjetivo = adsPosicionObjetivo;
            rotacionObjetivo *= adsRotacionObjetivo;
        }
        else if (estaCorriendo)
        {
            posicionObjetivo = armaActual.posicionSprint;
            rotacionObjetivo *= Quaternion.Euler(armaActual.rotacionSprint);

            tiempoBobbing += Time.deltaTime * (velocidadBobbing * 0.6f);

            float curvaBobbingY = Mathf.Sin(tiempoBobbing * 2f) * (cantidadBobbing * 1.5f);
            float curvaBobbingX = Mathf.Cos(tiempoBobbing) * (cantidadBobbing * 2f);

            posicionObjetivo += new Vector3(curvaBobbingX, curvaBobbingY, 0f);
            rotacionObjetivo *= Quaternion.Euler(-curvaBobbingY * 15f, curvaBobbingX * 15f, curvaBobbingX * 5f);
        }
        else
        {
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

        float multiplicadorApuntado = estaApuntando ? 0.5f : 1f;

        recoilPosicionActual += new Vector3(0, 0, armaActual.retrocesoZ * multiplicadorApuntado);

        float desvioAleatorio = Random.Range(-armaActual.retrocesoRotacionYAleatoria, armaActual.retrocesoRotacionYAleatoria);
        recoilRotacionActual *= Quaternion.Euler(
            armaActual.retrocesoRotacionX * multiplicadorApuntado,
            desvioAleatorio * multiplicadorApuntado,
            0
        );
    }
}