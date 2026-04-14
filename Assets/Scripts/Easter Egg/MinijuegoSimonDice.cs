using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class MinijuegoSimonDice : NetworkBehaviour
{
    public string idMisionAsociada = "Hackeo";

    [Header("Pantallas/Botones")]
    public InteraccionBotonSimon[] botones; // Arrastra aquí los 4 botones físicos
    public Material matApagado;
    public Material matEncendido; // Un material con Emission activada

    // Variables internas del servidor
    private List<int> secuenciaServidor = new List<int>();
    private int indiceJugadorActual = 0;
    private int nivelActual = 3; // Empezamos con 3 luces
    private int nivelMaximo = 5; // Hay que superar 3 niveles (3, 4 y 5 luces)
    private bool esperandoJugador = false;

    private void Update()
    {
        // Solo arranca si estamos en la misión correcta y el servidor no ha empezado
        if (IsServer && !esperandoJugador && secuenciaServidor.Count == 0)
        {
            if (QuestManager.Instance.pasoActivo != null &&
                QuestManager.Instance.pasoActivo.ID == idMisionAsociada)
            {
                EmpezarNuevaRonda();
            }
        }
    }

    private void EmpezarNuevaRonda()
    {
        secuenciaServidor.Clear();
        indiceJugadorActual = 0;
        esperandoJugador = false;

        // Generamos la secuencia al azar
        for (int i = 0; i < nivelActual; i++)
        {
            secuenciaServidor.Add(Random.Range(0, botones.Length));
        }

        StartCoroutine(ReproducirSecuencia());
    }

    private IEnumerator ReproducirSecuencia()
    {
        yield return new WaitForSeconds(1f); // Pausa dramática

        foreach (int indice in secuenciaServidor)
        {
            // Le decimos a todos los PCs que enciendan esta luz
            IluminarBotonClientRpc(indice, true);

            // Aquí puedes reproducir un sonido de "Bip"

            yield return new WaitForSeconds(0.6f); // Tiempo encendido

            IluminarBotonClientRpc(indice, false);
            yield return new WaitForSeconds(0.2f); // Tiempo apagado entre luces
        }

        esperandoJugador = true;
    }

    [ClientRpc]
    private void IluminarBotonClientRpc(int indiceBoton, bool encender)
    {
        // Cambiamos el material del botón (MeshRenderer)
        MeshRenderer malla = botones[indiceBoton].GetComponent<MeshRenderer>();
        if (malla != null)
        {
            malla.material = encender ? matEncendido : matApagado;
        }
    }

    // ==========================================
    // EL JUGADOR PULSA UN BOTÓN
    // ==========================================
    public void RecibirPulsacion(int indicePulsado)
    {
        if (!IsServer || !esperandoJugador) return;

        // Iluminamos el botón que acaba de pulsar (Feedback visual rápido)
        StartCoroutine(DestelloRapidoBoton(indicePulsado));

        if (indicePulsado == secuenciaServidor[indiceJugadorActual])
        {
            // ¡Acertó!
            indiceJugadorActual++;

            if (indiceJugadorActual >= secuenciaServidor.Count)
            {
                // Superó el nivel
                nivelActual++;
                if (nivelActual > nivelMaximo)
                {
                    Debug.Log("¡HACKEO COMPLETADO!");
                    QuestManager.Instance.NotificarPasoCompletadoServerRpc(idMisionAsociada);
                    esperandoJugador = false; // Bloqueamos para que no se juegue más
                }
                else
                {
                    // Siguiente nivel
                    EmpezarNuevaRonda();
                }
            }
        }
        else
        {
            // ¡Falló! (Aquí podrías hacer un ClientRpc para que todas las luces parpadeen en rojo)
            Debug.LogWarning("¡Secuencia incorrecta! Repitiendo ronda...");
            StartCoroutine(PenalizacionFallo());
        }
    }

    private IEnumerator DestelloRapidoBoton(int indice)
    {
        IluminarBotonClientRpc(indice, true);
        yield return new WaitForSeconds(0.3f);
        IluminarBotonClientRpc(indice, false);
    }

    private IEnumerator PenalizacionFallo()
    {
        esperandoJugador = false;
        yield return new WaitForSeconds(1.5f);
        EmpezarNuevaRonda(); // Repite el nivel actual
    }
}