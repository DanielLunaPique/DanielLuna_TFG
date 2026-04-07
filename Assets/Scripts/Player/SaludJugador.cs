using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class SaludJugador : NetworkBehaviour
{
    [Header("Configuración de Vida")]
    public int saludMaxima = 50;
    public NetworkVariable<int> saludActual = new NetworkVariable<int>(50, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public bool estaMuerto = false;

    [Header("Configuración de Regeneración")]
    public float tiempoEsperaRegen = 5f; // Cuánto tarda en empezar a curarse
    public float puntosPorSegundo = 10f; // Cuánta vida recupera por segundo
    private float contadorRegen = 0f;
    private float acumuladorSalud = 0f; // Para manejar decimales en una variable entera

    [Header("Efectos Visuales (HUD)")]
    [Tooltip("La imagen plana roja para el parpadeo rápido")]
    public Image destelloRojo;
    public float velocidadFadeDestello = 3f; // Muy rápido

    [Tooltip("Arrastra aquí todas tus imágenes de gotas de sangre")]
    public Image[] gotasDeSangre;
    public float velocidadFadeGotas = 0.5f; // Más lento

    [Header("Efectos de Audio")]
    public AudioSource audioCorazon;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            saludActual.Value = saludMaxima;
            estaMuerto = false;
        }

        saludActual.OnValueChanged += AlCambiarSalud;
    }

    public override void OnNetworkDespawn()
    {
        saludActual.OnValueChanged -= AlCambiarSalud;
    }

    private void AlCambiarSalud(int vidaAnterior, int vidaNueva)
    {
        if (IsOwner && vidaNueva < vidaAnterior && !estaMuerto)
        {
            // 1. EL DESTELLO RÁPIDO (Se enciende siempre que recibes daño)
            if (destelloRojo != null)
            {
                Color c = destelloRojo.color;
                c.a = 0.35f; // Un 35% de opacidad para que no ciegue al jugador
                destelloRojo.color = c;
            }

            // 2. LÓGICA CRÍTICA VS NORMAL
            if (vidaNueva <= 20)
            {
                // ESTADO CRÍTICO: Mostramos TODAS las gotas
                MostrarGotas(gotasDeSangre.Length);

                // Empezamos los latidos si no estaban sonando ya
                if (audioCorazon != null && !audioCorazon.isPlaying)
                {
                    audioCorazon.Play();
                }
            }
            else
            {
                // ESTADO NORMAL: Mostramos solo la MITAD de las gotas
                MostrarGotas(gotasDeSangre.Length / 2);
            }
        }
    }

    // Enciende un número específico de gotas
    private void MostrarGotas(int cantidadAEncender)
    {
        for (int i = 0; i < gotasDeSangre.Length; i++)
        {
            if (gotasDeSangre[i] != null)
            {
                Color c = gotasDeSangre[i].color;
                // Si está dentro de la cantidad que queremos, le ponemos opacidad a tope
                if (i < cantidadAEncender) c.a = 1f;
                gotasDeSangre[i].color = c;
            }
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        // 1. Lógica Visual de Sangre y Destello (Se queda igual que antes)
        ManejarEfectosVisuales();

        // 2. LÓGICA DE REGENERACIÓN (Solo se procesa en el Servidor)
        if (IsServer)
        {
            ProcesarRegeneracionServidor();
        }
    }

    private void ProcesarRegeneracionServidor()
    {
        if (estaMuerto || saludActual.Value >= saludMaxima) return;

        // Aumentamos el contador de tiempo desde el último daño
        contadorRegen += Time.deltaTime;

        // Si ha pasado el tiempo suficiente, empezamos a curar
        if (contadorRegen >= tiempoEsperaRegen)
        {
            // Usamos un acumulador porque saludActual es int y Time.deltaTime es muy pequeño
            acumuladorSalud += puntosPorSegundo * Time.deltaTime;

            if (acumuladorSalud >= 1f)
            {
                int puntosAAnadir = Mathf.FloorToInt(acumuladorSalud);
                saludActual.Value = Mathf.Min(saludActual.Value + puntosAAnadir, saludMaxima);
                acumuladorSalud -= puntosAAnadir;

                // Si al curarse sale del estado crítico (> 20), paramos los latidos
                if (saludActual.Value > 20 && audioCorazon.isPlaying)
                {
                    audioCorazon.Stop();
                }
            }
        }
    }

    public void RecibirDaño(int daño)
    {
        if (!IsServer || estaMuerto) return;

        saludActual.Value -= daño;

        // Reseteamos la regeneración al recibir un golpe
        contadorRegen = 0f;
        acumuladorSalud = 0f;

        if (saludActual.Value <= 0)
        {
            saludActual.Value = 0;
            Morir();
        }
    }

    private void ManejarEfectosVisuales()
    {
        // 1. Desaparecer el destello rojo
        if (destelloRojo != null && destelloRojo.color.a > 0)
        {
            Color c = destelloRojo.color;
            c.a -= Time.deltaTime * velocidadFadeDestello;
            destelloRojo.color = c;
        }

        // 2. Desaparecer las gotas de sangre (Solo si la salud es > 20)
        if (saludActual.Value > 20)
        {
            foreach (Image gota in gotasDeSangre)
            {
                if (gota != null && gota.color.a > 0)
                {
                    Color c = gota.color;
                    c.a -= Time.deltaTime * velocidadFadeGotas;
                    gota.color = c;
                }
            }
        }
    }

    private void Morir()
    {
        estaMuerto = true;

        // Apagamos el corazón al morir para que no moleste en la pantalla de Game Over
        if (audioCorazon != null) audioCorazon.Stop();

        Debug.Log($"<color=red>¡EL JUGADOR {OwnerClientId} HA MUERTO!</color>");

        // Parche rápido visual (Tumbamos al jugador)
        transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }
}