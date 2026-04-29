using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class Diana : NetworkBehaviour
{
    private GestorDianas miGestor;
    public AudioClip sonidoImpacto;
    public NetworkVariable<bool> abatida = new NetworkVariable<bool>(false);
    private Vector3 escalaOriginal;

    public override void OnNetworkSpawn()
    {
        escalaOriginal = transform.localScale;
        abatida.OnValueChanged += AlCambiarEstado;

        // Buscamos al gestor al nacer para evitar NullReference
        miGestor = GetComponentInParent<SoporteDiana>()?.miGestor;
        if (miGestor == null) miGestor = FindObjectOfType<GestorDianas>();
    }

    [ServerRpc(RequireOwnership = false)]
    public void RecibirDisparoServerRpc()
    {
        Debug.Log($"[DIANA {gameObject.name}] ¡Me han disparado! Verificando estado...");

        if (miGestor == null)
        {
            Debug.LogError("[DIANA] Error: No sé quién es mi gestor.");
            return;
        }

        if (!miGestor.juegoActivo.Value)
        {
            Debug.LogWarning("[DIANA] Bloqueado: El gestor dice que el juego NO está activo.");
            return;
        }

        if (abatida.Value)
        {
            Debug.LogWarning("[DIANA] Bloqueado: Ya estaba muerta.");
            return;
        }

        Debug.Log("[DIANA] Disparo válido. Cambiando estado y avisando al gestor.");
        abatida.Value = true;
        miGestor.ComprobarProgresoServer();
    }

    private void AlCambiarEstado(bool antigua, bool nueva)
    {
        if (nueva)
        {
            if (sonidoImpacto != null) AudioSource.PlayClipAtPoint(sonidoImpacto, transform.position);
            StartCoroutine(EfectoMuerte());
        }
        else
        {
            gameObject.SetActive(true);
            transform.localScale = escalaOriginal;
        }
    }

    private IEnumerator EfectoMuerte()
    {
        // Configuramos los tiempos y tamaños para el "Pop"
        float tiempoCrecer = 0.1f;      // Muy rápido al recibir la bala
        float tiempoEsperar = 0.05f;    // Se congela un instante para que el ojo lo capte
        float tiempoEncoger = 0.25f;    // Desaparece limpiamente
        Vector3 escalaMaxima = escalaOriginal * 1.2f; // Crece un 20%

        // Fase 1: Crecer un poquito (Impacto)
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / tiempoCrecer;
            transform.localScale = Vector3.Lerp(escalaOriginal, escalaMaxima, t);
            yield return null;
        }

        // Mantenemos la pose un instante
        yield return new WaitForSeconds(tiempoEsperar);

        // Fase 2: Encoger a cero y desaparecer
        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / tiempoEncoger;
            transform.localScale = Vector3.Lerp(escalaMaxima, Vector3.zero, t);
            yield return null;
        }

        gameObject.SetActive(false); // Apagamos el objeto
    }
}