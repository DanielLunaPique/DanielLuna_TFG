using UnityEngine;

public class ControladorCamaraFPS : MonoBehaviour
{
    [Header("Configuración Base")]
    public float sensibilidadRaton = 2f;
    public Transform cuerpoJugador;

    private float rotacionX = 0f;
    private float rotacionY = 0f;

    [Header("Umbral de Ruptura (Opción A)")]
    [Tooltip("Grados que puedes mover el ratón antes de que el imán se desactive (Ej: 10)")]
    public float umbralRupturaIman = 10f;

    // Variables que inyecta el arma actual
    private float velocidadRetrocesoActual = 10f;
    private float topeRetrocesoVerticalActual = 15f;
    private float fuerzaTironRegresoActual = 6f;

    // Variables del latigazo
    private float retrocesoAcumuladoX = 0f;
    private float latigazoPendienteX = 0f;
    private float latigazoPendienteY = 0f;

    // Variables de la memoria de ráfaga y el cable
    private float inicioRafagaX = 0f;
    private bool enRafaga = false;
    private float tiempoUltimoDisparo = 0f;
    private float energiaDeRegreso = 0f;

    // LAS VARIABLES DEL CABLE:
    private bool imanRoto = false;
    private float movimientoRatonAcumulado = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        rotacionX = transform.localEulerAngles.x;
        rotacionY = cuerpoJugador != null ? cuerpoJugador.eulerAngles.y : transform.eulerAngles.y;
    }

    void Update()
    {
        float inputRatonX = Input.GetAxis("Mouse X") * sensibilidadRaton;
        float inputRatonY = Input.GetAxis("Mouse Y") * sensibilidadRaton;

        // --- LA LÓGICA DEL CABLE ---
        if (enRafaga)
        {
            // Sumamos el movimiento del ratón en valor absoluto (da igual si es arriba, abajo, izq o der)
            movimientoRatonAcumulado += Mathf.Abs(inputRatonX) + Mathf.Abs(inputRatonY);

            // Si has movido el ratón más del umbral permitido, ¡se rompe el cable!
            if (movimientoRatonAcumulado > umbralRupturaIman)
            {
                imanRoto = true;
            }
        }

        // Detectar si la ráfaga ha terminado (han pasado 0.15s desde la última bala)
        if (enRafaga && Time.time > tiempoUltimoDisparo + 0.15f)
        {
            enRafaga = false;
            energiaDeRegreso = 1f;
            retrocesoAcumuladoX = 0f;
        }

        // --- 1. APLICAR EL LATIGAZO DEL DISPARO ---
        float pasoRetrocesoX = Mathf.Lerp(0, latigazoPendienteX, Time.deltaTime * velocidadRetrocesoActual);
        float pasoRetrocesoY = Mathf.Lerp(0, latigazoPendienteY, Time.deltaTime * velocidadRetrocesoActual);

        latigazoPendienteX -= pasoRetrocesoX;
        latigazoPendienteY -= pasoRetrocesoY;

        rotacionY += inputRatonX + pasoRetrocesoY;
        rotacionX -= inputRatonY + pasoRetrocesoX;

        // --- 2. LA MAGIA: EL IMÁN (SOLO SI NO SE HA ROTO) ---
        if (!enRafaga && energiaDeRegreso > 0 && !imanRoto)
        {
            float diferencia = inicioRafagaX - rotacionX;
            rotacionX += diferencia * Time.deltaTime * fuerzaTironRegresoActual;
            energiaDeRegreso -= Time.deltaTime * 3f;
        }

        rotacionX = Mathf.Clamp(rotacionX, -89f, 89f);

        transform.localRotation = Quaternion.Euler(rotacionX, 0f, 0f);

        if (cuerpoJugador != null)
        {
            cuerpoJugador.rotation = Quaternion.Euler(0f, rotacionY, 0f);
        }
    }

    public void RecibirRetroceso(float fuerzaArriba, float fuerzaLado, float velRetroceso, float topeVertical, float fuerzaTiron)
    {
        velocidadRetrocesoActual = velRetroceso;
        topeRetrocesoVerticalActual = topeVertical;
        fuerzaTironRegresoActual = fuerzaTiron;

        // INICIO DE LA RÁFAGA: Reparamos el cable y guardamos la posición
        if (!enRafaga)
        {
            inicioRafagaX = rotacionX;
            enRafaga = true;
            energiaDeRegreso = 0f;

            imanRoto = false; // Cable nuevo
            movimientoRatonAcumulado = 0f; // Contador a cero
        }

        tiempoUltimoDisparo = Time.time;

        if (retrocesoAcumuladoX < topeRetrocesoVerticalActual)
        {
            float fuerzaReal = Mathf.Min(fuerzaArriba, topeRetrocesoVerticalActual - retrocesoAcumuladoX);
            retrocesoAcumuladoX += fuerzaReal;
            latigazoPendienteX += fuerzaReal;
        }

        float desvioLado = Random.Range(-fuerzaLado, fuerzaLado);
        latigazoPendienteY += desvioLado;
    }
}