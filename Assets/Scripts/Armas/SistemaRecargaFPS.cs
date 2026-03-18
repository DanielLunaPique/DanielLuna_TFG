using UnityEngine;
using System.Collections;

public class SistemaRecargaFPS : MonoBehaviour
{
    [Header("Referencias")]
    public ControladorArmasFPS controladorFPS;

    // [FUTURO ANIMATOR] Aquí pondremos: public Animator animatorBrazos;

    void Update()
    {
        if (controladorFPS == null || controladorFPS.armaActual == null) return;

        DatosArma arma = controladorFPS.armaActual;

        // --- 1. RECARGA MANUAL ---
        if (Input.GetKeyDown(KeyCode.R) && !controladorFPS.estaRecargando)
        {
            // Comprobamos si realmente necesitamos recargar
            if (arma.balasActuales < arma.estadisticas.balasCargador && arma.balasReserva > 0)
            {
                StartCoroutine(RutinaRecarga(arma));
            }
            else
            {
                Debug.Log("El cargador ya está lleno o no tienes balas de reserva.");
            }
        }

        // Si el cargador está vacío, no estamos recargando ya, y tenemos balas en reserva...
        if (arma.balasActuales <= 0 && !controladorFPS.estaRecargando && arma.balasReserva > 0)
        {
            StartCoroutine(RutinaRecarga(arma));
        }
    }

    private IEnumerator RutinaRecarga(DatosArma arma)
    {
        // 1. ENCENDEMOS EL SEMÁFORO ROJO (Bloqueamos disparo y otras acciones)
        controladorFPS.estaRecargando = true;
        Debug.Log("Recargando...");

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

        Debug.Log($"¡Recarga Completa! Cargador: {arma.balasActuales} | Reserva: {arma.balasReserva}");

        // 4. PONEMOS EL SEMÁFORO EN VERDE
        controladorFPS.estaRecargando = false;
    }
}