using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections.Generic;

public class SaludJugador : NetworkBehaviour
{
    [Header("Configuración de Vida")]
    public int saludMaxima = 50;
    public NetworkVariable<int> saludActual = new NetworkVariable<int>(50, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public bool estaMuerto = false;
    private int indiceEspectador = 0;

    [Header("Configuración de Regeneración")]
    public float tiempoEsperaRegen = 5f;
    public float puntosPorSegundo = 10f;
    private float contadorRegen = 0f;
    private float acumuladorSalud = 0f;

    [Header("Efectos Visuales (HUD)")]
    public Image destelloRojo;
    public float velocidadFadeDestello = 3f;
    public Image[] gotasDeSangre;
    public float velocidadFadeGotas = 0.5f;

    [Header("Efectos de Audio")]
    public AudioSource audioCorazon;

    private UIManager uiManagerLocal;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            saludActual.Value = saludMaxima;
            estaMuerto = false;
        }

        // Buscamos el UIManager estrictamente dentro de ESTE jugador, no en toda la escena
        uiManagerLocal = GetComponentInChildren<UIManager>(true);

        saludActual.OnValueChanged += AlCambiarSalud;
    }

    public override void OnNetworkDespawn()
    {
        saludActual.OnValueChanged -= AlCambiarSalud;
    }

    public void RecibirDaño(int daño)
    {
        if (!IsServer || estaMuerto) return;

        saludActual.Value -= daño;
        contadorRegen = 0f;
        acumuladorSalud = 0f;

        if (saludActual.Value <= 0)
        {
            saludActual.Value = 0;
            estaMuerto = true;
            GameManager.Instance.ComprobarEstadoEquipo();
        }
    }

    private void AlCambiarSalud(int vidaAnterior, int vidaNueva)
    {
        // Efectos Visuales (Solo para el dueño)
        if (IsOwner && vidaNueva < vidaAnterior && !estaMuerto)
        {
            GetComponent<SistemaVoces>().ReproducirFrase(SistemaVoces.TipoVoz.Herido);

            if (uiManagerLocal != null && uiManagerLocal.menuTiendaAbierto)
            {
                // Cerramos el menú a la fuerza
                uiManagerLocal.CerrarMenuTiendaMedica();

                // Le volvemos a poner el texto de la 'E' por si quiere volver a intentarlo
                uiManagerLocal.MostrarTextoInteraccion("Mantén [E] para Tienda Médica");

                Debug.LogWarning("¡Un zombie te ha interrumpido mientras intentabas revivir!");
            }

            if (destelloRojo != null)
            {
                Color c = destelloRojo.color;
                c.a = 0.35f;
                destelloRojo.color = c;
            }

            if (vidaNueva <= 20)
            {
                MostrarGotas(gotasDeSangre.Length);
                if (audioCorazon != null && !audioCorazon.isPlaying) audioCorazon.Play();
            }
            else MostrarGotas(gotasDeSangre.Length / 2);
        }

        // Lógica Universal de Muerte
        if (vidaNueva <= 0 && vidaAnterior > 0)
        {
            EjecutarMuerteLocal();
        }
    }

    private void EjecutarMuerteLocal()
    {
        estaMuerto = true;
        if (audioCorazon != null) audioCorazon.Stop();

        ConvertirseEnFantasma();

        if (IsOwner)
        {
            CambiarEspectador(0);
        }
    }

    private void ConvertirseEnFantasma()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        NetworkMovement mov = GetComponent<NetworkMovement>();
        if (mov != null) mov.enabled = false;

        ControladorArmasFPS armas = GetComponentInChildren<ControladorArmasFPS>(true);
        if (armas != null) armas.enabled = false;

        ControladorCamaraFPS camFPS = GetComponentInChildren<ControladorCamaraFPS>(true);
        if (camFPS != null) camFPS.enabled = false;

        // APAGADO NUCLEAR: Apagamos todos los renderers (animados y estáticos)
        foreach (Renderer malla in GetComponentsInChildren<Renderer>(true))
        {
            malla.enabled = false;
        }

        if (mov != null)
        {
            if (mov.objetoBrazosFPS != null) mov.objetoBrazosFPS.SetActive(false);
            if (mov.armaManos != null) mov.armaManos.SetActive(false);
        }

    }

    // ==========================================
    // SISTEMA DE REVIVIR

    [ClientRpc]
    public void EjecutarRevivirClientRpc(Vector3 posSpawn, Quaternion rotSpawn)
    {
        estaMuerto = false;
        
        // 1. Reconstruimos el cuerpo físico para todas las pantallas
        RestaurarCuerpoFisico(posSpawn, rotSpawn);

        // 2. Si soy YO el revivido, me lanzo mi cinemática de despertar
        if (IsOwner)
        {
            StartCoroutine(CinematicaDespertar());
        }
    }

    private void RestaurarCuerpoFisico(Vector3 pos, Quaternion rot)
    {
        // Para teletransportar un CharacterController sin que falle, HAY QUE APAGARLO PRIMERO
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            transform.position = pos;
            transform.rotation = rot;
            cc.enabled = true;
        }

        // Encendemos colisiones
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        // Encendemos su modelo 3D
        foreach (Renderer malla in GetComponentsInChildren<Renderer>(true))
        {
            malla.enabled = true;
        }
    }

    private System.Collections.IEnumerator CinematicaDespertar()
    {
        // 1. Apagamos la pantalla negra de "Espectando"
        if (uiManagerLocal != null) uiManagerLocal.OcultarHUDModuloEspectador();

        // 2. Rescatamos nuestras cámaras de la cabeza del otro jugador
        NetworkMovement miMov = GetComponent<NetworkMovement>();
        Transform miCuello = transform.Find("Camera Root");
        
        if (miMov != null && miCuello != null)
        {
            if (miMov.camaraPrincipal != null)
            {
                miMov.camaraPrincipal.transform.SetParent(miCuello);
                miMov.camaraPrincipal.transform.localPosition = Vector3.zero; 
                miMov.camaraPrincipal.transform.localRotation = Quaternion.identity;
            }
            if (miMov.camaraArma != null)
            {
                miMov.camaraArma.transform.SetParent(miCuello);
                miMov.camaraArma.transform.localPosition = Vector3.zero;
                miMov.camaraArma.transform.localRotation = Quaternion.identity;
            }
        }

        // 3. Encendemos los brazos de primera persona
        if (miMov != null)
        {
            if (miMov.objetoBrazosFPS != null) miMov.objetoBrazosFPS.SetActive(true);
            if (miMov.armaManos != null) miMov.armaManos.SetActive(true);
        }

        // 4. Lanzamos la animación de Mixamo en nuestro cuerpo 
        // (Asegúrate de tener el Trigger "Revive" en el Animator de 3ª persona)
        Animator anim = GetComponentInChildren<Animator>(true);
        if (anim != null) anim.SetTrigger("Revive");

        // 5. ¡CONGELAMOS AL JUGADOR MIENTRAS SE LEVANTA!
        if (miMov != null) miMov.enabled = false;
        
        ControladorArmasFPS armas = GetComponentInChildren<ControladorArmasFPS>(true);
        if (armas != null) armas.enabled = false;
        
        // Esperamos a que la animación de Mixamo termine de reproducirse
        yield return new WaitForSeconds(2.5f); // <-- Ajusta este tiempo si tu animación dura más o menos

        // 6. ¡DEVOLVEMOS EL CONTROL TOTAL PARA SALIR DE LA TIENDA!
        if (miMov != null) miMov.enabled = true;
        if (armas != null) armas.enabled = true;

        // 7. Le decimos al inventario que vuelva a sacar el arma visible
        InventarioArmas miInventario = GetComponentInChildren<InventarioArmas>(true);
        if (miInventario != null) miInventario.RecibirNuevaArma(miInventario.armaPorDefecto); // O la función que uses

        ControladorCamaraFPS camFPS = GetComponentInChildren<ControladorCamaraFPS>(true);
        if (camFPS != null) camFPS.enabled = true;
    }

    private void CambiarEspectador(int direccion)
    {
        List<NetworkMovement> vivos = new List<NetworkMovement>();
        foreach (SaludJugador j in FindObjectsOfType<SaludJugador>())
        {
            if (j != this && !j.estaMuerto) vivos.Add(j.GetComponent<NetworkMovement>());
        }

        if (vivos.Count == 0)
        {
            if (uiManagerLocal != null) uiManagerLocal.OcultarHUDModuloEspectador();
            transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            return;
        }

        indiceEspectador = (indiceEspectador + direccion + vivos.Count) % vivos.Count;
        NetworkMovement target = vivos[indiceEspectador];

        if (uiManagerLocal != null) uiManagerLocal.MostrarHUDModuloEspectador("Jugador " + target.OwnerClientId);

        Transform cabeza = target.transform.Find("Camera Root");
        if (cabeza == null) cabeza = target.transform;

        NetworkMovement miMov = GetComponent<NetworkMovement>();

        if (cabeza != null && miMov != null && miMov.camaraPrincipal != null)
        {
            // 1. Movemos la cámara principal
            miMov.camaraPrincipal.transform.SetParent(cabeza);
            miMov.camaraPrincipal.SetActive(true);
            miMov.camaraPrincipal.transform.localPosition = new Vector3(0.7f, 1.5f, -2.5f);
            miMov.camaraPrincipal.transform.localRotation = Quaternion.identity;

            // 2. NUEVO: Movemos la Weapon Camera porque TU UI está dentro de ella
            if (miMov.camaraArma != null)
            {
                miMov.camaraArma.transform.SetParent(cabeza);
                miMov.camaraArma.SetActive(true); // La obligamos a encenderse por si acaso
                miMov.camaraArma.transform.localPosition = new Vector3(0.7f, 1f, -1.5f);
                miMov.camaraArma.transform.localRotation = Quaternion.identity;
            }
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        ManejarEfectosVisuales();

        if (IsServer) ProcesarRegeneracionServidor();

        if (estaMuerto)
        {
            if (Input.GetMouseButtonDown(0)) CambiarEspectador(1);
            else if (Input.GetMouseButtonDown(1)) CambiarEspectador(-1);
        }
    }

    private void MostrarGotas(int cantidadAEncender)
    {
        for (int i = 0; i < gotasDeSangre.Length; i++)
        {
            if (gotasDeSangre[i] != null)
            {
                Color c = gotasDeSangre[i].color;
                c.a = (i < cantidadAEncender) ? 1f : c.a;
                gotasDeSangre[i].color = c;
            }
        }
    }

    private void ProcesarRegeneracionServidor()
    {
        if (estaMuerto || saludActual.Value >= saludMaxima) return;
        contadorRegen += Time.deltaTime;
        if (contadorRegen >= tiempoEsperaRegen)
        {
            acumuladorSalud += puntosPorSegundo * Time.deltaTime;
            if (acumuladorSalud >= 1f)
            {
                int puntos = Mathf.FloorToInt(acumuladorSalud);
                saludActual.Value = Mathf.Min(saludActual.Value + puntos, saludMaxima);
                acumuladorSalud -= puntos;
                if (saludActual.Value > 20 && audioCorazon.isPlaying) audioCorazon.Stop();
            }
        }
    }

    private void ManejarEfectosVisuales()
    {
        if (destelloRojo != null && destelloRojo.color.a > 0)
        {
            Color c = destelloRojo.color;
            c.a -= Time.deltaTime * velocidadFadeDestello;
            destelloRojo.color = c;
        }

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
}