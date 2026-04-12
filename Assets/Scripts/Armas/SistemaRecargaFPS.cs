using UnityEngine;
using System.Collections;

public class SistemaRecargaFPS : MonoBehaviour
{
    [Header("Referencias")]
    public ControladorArmasFPS controladorFPS;

    // [FUTURO ANIMATOR] Aquí pondremos: public Animator animatorBrazos;

    private Coroutine corrutinaRecargaActiva;

    void Update()
    {
        if (controladorFPS == null || controladorFPS.armaActual == null) return;

        DatosArma arma = controladorFPS.armaActual;

        // --- 1. SPRINT CANCEL ---
        // Si estamos recargando, y el jugador pulsa la tecla de correr Y se está moviendo hacia adelante...
        if (controladorFPS.estaRecargando && Input.GetKey(KeyCode.LeftShift) && Input.GetAxis("Vertical") > 0)
        {
            CancelarRecarga();
            return; // Salimos del Update para no hacer nada más en este fotograma
        }

        // --- 2. RECARGA MANUAL ---
        if (Input.GetKeyDown(KeyCode.R) && !controladorFPS.estaRecargando)
        {
            if (arma.balasActuales < arma.estadisticas.balasCargador && arma.balasReserva > 0)
            {
                GetComponentInParent<SistemaVoces>().ReproducirFrase(SistemaVoces.TipoVoz.Recarga);

                // Guardamos la corrutina en nuestra variable
                corrutinaRecargaActiva = StartCoroutine(RutinaRecarga(arma));
            }
        }

        // --- 3. RECARGA AUTOMÁTICA ---
        if (arma.balasActuales <= 0 && !controladorFPS.estaRecargando && arma.balasReserva > 0)
        {
            // Guardamos la corrutina en nuestra variable
            corrutinaRecargaActiva = StartCoroutine(RutinaRecarga(arma));
        }
    }

    private void CancelarRecarga()
    {
        // 1. Matamos el temporizador
        if (corrutinaRecargaActiva != null)
        {
            StopCoroutine(corrutinaRecargaActiva);
            corrutinaRecargaActiva = null;
        }

        // 2. Apagamos el semáforo para que el jugador pueda volver a disparar o correr
        controladorFPS.estaRecargando = false;

        // [FUTURO ANIMATOR] Aquí le dirías a los brazos que vuelvan a la postura de correr
        // animatorBrazos.SetTrigger("CancelarRecarga");
    }

    private IEnumerator RutinaRecarga(DatosArma arma)
    {
        // 1. ENCENDEMOS EL SEMÁFORO ROJO (Bloqueamos disparo y otras acciones)
        controladorFPS.estaRecargando = true;

        // [FUTURO ANIMATOR] Aquí lanzaremos la animación: animatorBrazos.SetTrigger("Recargar");
        // [FUTURO AUDIO] Aquí reproduciremos el sonido: audioSource.PlayOneShot(sonidoRecarga);

        // 2. ESPERAMOS EL TIEMPO QUE TARDE LA RECARGA
        float tiempoRecarga = arma.estadisticas.tiempoRecarga > 0 ? arma.estadisticas.tiempoRecarga : 2f;
        yield return new WaitForSeconds(tiempoRecarga);

        // 3. LA MATEMÁTICA DE LA MUNICIÓN
        int balasQueFaltan = arma.estadisticas.balasCargador - arma.balasActuales;

        int balasARecargar = Mathf.Min(balasQueFaltan, arma.balasReserva);

        arma.balasActuales += balasARecargar;
        arma.balasReserva -= balasARecargar;

        // 4. PONEMOS EL SEMÁFORO EN VERDE
        controladorFPS.estaRecargando = false;
        corrutinaRecargaActiva = null;
    }
}