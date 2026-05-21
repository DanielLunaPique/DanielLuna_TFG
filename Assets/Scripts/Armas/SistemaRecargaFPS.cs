using UnityEngine;
using System.Collections;

public class SistemaRecargaFPS : MonoBehaviour
{
    [Header("Referencias")]
    public ControladorArmasFPS controladorFPS;

    [Header("Audio")]
    public AudioSource audioFuente;

    [Header("Animación Procedural")]
    public float distanciaBajarRecarga = 0.4f;
    public float tiempoBajarSubir = 0.25f;

    private Coroutine corrutinaRecargaActiva;

    void Update()
    {
        if (controladorFPS == null || controladorFPS.armaActual == null) return;

        DatosArma arma = controladorFPS.armaActual;

        if (controladorFPS.estaRecargando && Input.GetKey(KeyCode.LeftShift) && Input.GetAxis("Vertical") > 0)
        {
            CancelarRecarga(arma);
            return;
        }

        if (Input.GetKeyDown(KeyCode.R) && !controladorFPS.estaRecargando)
        {
            if (arma.balasActuales < arma.estadisticas.balasCargador && arma.balasReserva > 0)
            {
                GetComponentInParent<SistemaVoces>().ReproducirFrase(SistemaVoces.TipoVoz.Recarga);
                corrutinaRecargaActiva = StartCoroutine(RutinaRecarga(arma));
            }
        }

        if (arma.balasActuales <= 0 && !controladorFPS.estaRecargando && arma.balasReserva > 0)
        {
            corrutinaRecargaActiva = StartCoroutine(RutinaRecarga(arma));
        }
    }

    private void CancelarRecarga(DatosArma arma)
    {
        if (corrutinaRecargaActiva != null)
        {
            StopCoroutine(corrutinaRecargaActiva);
            corrutinaRecargaActiva = null;
        }

        controladorFPS.estaRecargando = false;

        // Si cancelamos, restauramos el contenedor a su posición original en el centro (0,0,0)
        if (arma.transform.parent != null)
        {
            arma.transform.parent.localPosition = Vector3.zero;
        }
    }

    private IEnumerator RutinaRecarga(DatosArma arma)
    {
        controladorFPS.estaRecargando = true;

        if (audioFuente != null && arma.estadisticas.sonidoRecarga != null)
        {
            audioFuente.PlayOneShot(arma.estadisticas.sonidoRecarga);
        }

        Transform contenedor = arma.transform.parent;

        Vector3 posOriginal = Vector3.zero;
        Vector3 posAbajo = posOriginal - new Vector3(0, distanciaBajarRecarga, 0);

        // --- FASE 1: BAJAR EL ARMA ---
        float tiempoPasado = 0f;
        while (tiempoPasado < tiempoBajarSubir)
        {
            tiempoPasado += Time.deltaTime;
            contenedor.localPosition = Vector3.Lerp(posOriginal, posAbajo, tiempoPasado / tiempoBajarSubir);
            yield return null;
        }

        // --- FASE 2: ESPERAR (¡MANTENIENDO EL ARMA ABAJO A LA FUERZA!) ---
        float tiempoEspera = arma.estadisticas.tiempoRecarga - (tiempoBajarSubir * 2);
        if (tiempoEspera > 0)
        {
            float tEspera = 0f;
            while (tEspera < tiempoEspera)
            {
                tEspera += Time.deltaTime;

                // Forzamos la posición en cada frame para ganarle la pelea al script de balanceo
                contenedor.localPosition = posAbajo;

                yield return null; // Esperamos al siguiente frame
            }
        }

        // --- MATEMÁTICA DE LA MUNICIÓN ---
        int balasQueFaltan = arma.estadisticas.balasCargador - arma.balasActuales;
        int balasARecargar = Mathf.Min(balasQueFaltan, arma.balasReserva);
        arma.balasActuales += balasARecargar;
        arma.balasReserva -= balasARecargar;

        // --- FASE 3: SUBIR EL ARMA ---
        tiempoPasado = 0f;
        while (tiempoPasado < tiempoBajarSubir)
        {
            tiempoPasado += Time.deltaTime;
            contenedor.localPosition = Vector3.Lerp(posAbajo, posOriginal, tiempoPasado / tiempoBajarSubir);
            yield return null;
        }

        // Aseguramos que quede perfecta en el centro
        contenedor.localPosition = posOriginal;

        controladorFPS.estaRecargando = false;
        corrutinaRecargaActiva = null;
    }
}