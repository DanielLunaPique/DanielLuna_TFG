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

        // 1. Detectamos si el jugador está intentando esprintar
        bool quiereCorrer = Input.GetKey(KeyCode.LeftShift) && Input.GetAxis("Vertical") > 0;

        // 2. Si YA estaba recargando y de repente se pone a correr -> CANCELAR RECARGA
        if (controladorFPS.estaRecargando && quiereCorrer)
        {
            CancelarRecarga(arma);
            return;
        }

        // --- EL SEGURO ANTI-BUGS ---
        // Si el jugador está corriendo, abortamos la función aquí mismo.
        // Esto impide que el arma intente recargar (manual o automáticamente) 
        // dándole prioridad absoluta al sprint.
        if (quiereCorrer) return;

        // 3. Recarga Manual (Solo llega aquí si NO está corriendo)
        if (Input.GetKeyDown(KeyCode.R) && !controladorFPS.estaRecargando)
        {
            if (arma.balasActuales < arma.estadisticas.balasCargador && arma.balasReserva > 0)
            {
                corrutinaRecargaActiva = StartCoroutine(RutinaRecarga(arma));
            }
        }

        // 4. Recarga Automática (Solo llega aquí si NO está corriendo)
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

        // --- NUEVO: CORTAR EL AUDIO SI SE CANCELA LA RECARGA ---
        if (audioFuente != null && audioFuente.isPlaying)
        {
            audioFuente.Stop();
        }

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
            // --- NUEVO: SINCRONIZAR AUDIO Y TIEMPO ---
            audioFuente.clip = arma.estadisticas.sonidoRecarga;

            // Calculamos a qué velocidad tiene que ir el audio para que encaje perfecto
            float duracionOriginal = arma.estadisticas.sonidoRecarga.length;
            audioFuente.pitch = duracionOriginal / arma.estadisticas.tiempoRecarga;

            audioFuente.Play();
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
                contenedor.localPosition = posAbajo;
                yield return null;
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

        contenedor.localPosition = posOriginal;
        controladorFPS.estaRecargando = false;
        corrutinaRecargaActiva = null;

        // --- NUEVO: RESTAURAR EL PITCH POR SI ACASO ---
        if (audioFuente != null) audioFuente.pitch = 1f;
    }
}