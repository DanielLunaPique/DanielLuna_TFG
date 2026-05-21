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

    [Header("Componentes de Audio")]
    public AudioSource audioCorazon;
    public AudioSource audioEfectos;

    [Header("Archivos de Audio")]
    [Tooltip("El sonido del latido del corazón (Largo, sin loop)")]
    public AudioClip clipCorazon;

    [Tooltip("El sonido de desgarro/golpe que suena SIEMPRE al recibir daño")]
    public AudioClip sonidoImpacto;

    [Tooltip("Probabilidad de que el personaje hable/gruña usando el SistemaVoces (0.0 a 1.0)")]
    [Range(0f, 1f)] public float probabilidadQueja = 0.5f;

    private UIManager uiManagerLocal;
    private SistemaVoces sistemaVocesLocal;
    private Coroutine coroutineCorazon;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            saludActual.Value = saludMaxima;
            estaMuerto = false;
        }

        uiManagerLocal = GetComponentInChildren<UIManager>(true);
        sistemaVocesLocal = GetComponent<SistemaVoces>();

        saludActual.OnValueChanged += AlCambiarSalud;

        if (audioCorazon != null && clipCorazon != null)
        {
            audioCorazon.clip = clipCorazon;
            audioCorazon.loop = false;
        }
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
            GameManager.Instance.ComprobarEstadoEquipo(transform.position);
        }
    }

    private void AlCambiarSalud(int vidaAnterior, int vidaNueva)
    {
        if (!IsOwner) return;

        // 1. --- LÓGICA DE RECIBIR DAÑO ---
        if (vidaNueva < vidaAnterior && !estaMuerto)
        {
            // A) Efectos de Audio
            if (audioEfectos != null && sonidoImpacto != null)
            {
                audioEfectos.PlayOneShot(sonidoImpacto, 1f);
            }

            if (sistemaVocesLocal != null && Random.value <= probabilidadQueja)
            {
                sistemaVocesLocal.ReproducirFrase(SistemaVoces.TipoVoz.Herido);
            }

            // B) Efectos Visuales (EXACTAMENTE COMO LO TENÍAS TÚ)
            if (uiManagerLocal != null && uiManagerLocal.menuTiendaAbierto)
            {
                uiManagerLocal.CerrarMenuTiendaMedica();
                uiManagerLocal.MostrarTextoInteraccion("Mantén [E] para Tienda Médica");
            }

            if (destelloRojo != null)
            {
                Color c = destelloRojo.color;
                c.a = 0.35f;
                destelloRojo.color = c;
            }

            // La lógica original de la sangre
            if (vidaNueva <= 20)
            {
                MostrarGotas(gotasDeSangre.Length);
            }
            else
            {
                MostrarGotas(gotasDeSangre.Length / 2);
            }
        }

        // 2. --- LÓGICA DEL CORAZÓN (Se evalúa para encender y apagar) ---
        if (vidaNueva > 0 && vidaNueva <= 20 && !estaMuerto)
        {
            if (audioCorazon != null && !audioCorazon.isPlaying && clipCorazon != null)
            {
                if (coroutineCorazon != null) StopCoroutine(coroutineCorazon);
                coroutineCorazon = StartCoroutine(FadeCorazon(true));
            }
        }
        else
        {
            if (audioCorazon != null && audioCorazon.isPlaying)
            {
                if (coroutineCorazon != null) StopCoroutine(coroutineCorazon);
                coroutineCorazon = StartCoroutine(FadeCorazon(false));
            }
        }

        // 3. --- LÓGICA UNIVERSAL DE MUERTE ---
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

        if (IsOwner) CambiarEspectador(0);
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

    private System.Collections.IEnumerator FadeCorazon(bool activar)
    {
        float tiempoTransicion = 0.25f;
        float tiempoPasado = 0f;

        if (activar)
        {
            audioCorazon.volume = 0f;
            audioCorazon.Play();

            while (tiempoPasado < tiempoTransicion)
            {
                tiempoPasado += Time.deltaTime;
                audioCorazon.volume = Mathf.Lerp(0f, 1f, tiempoPasado / tiempoTransicion);
                yield return null;
            }
            audioCorazon.volume = 1f;
        }
        else
        {
            float volumenInicial = audioCorazon.volume;

            while (tiempoPasado < tiempoTransicion)
            {
                tiempoPasado += Time.deltaTime;
                audioCorazon.volume = Mathf.Lerp(volumenInicial, 0f, tiempoPasado / tiempoTransicion);
                yield return null;
            }
            audioCorazon.volume = 0f;
            audioCorazon.Stop();
        }
    }

    // ==========================================
    // SISTEMA DE REVIVIR

    [ClientRpc]
    public void EjecutarRevivirClientRpc(Vector3 posSpawn, Quaternion rotSpawn)
    {
        estaMuerto = false;
        RestaurarCuerpoFisico(posSpawn, rotSpawn);

        if (IsOwner) StartCoroutine(CinematicaDespertar());
    }

    private void RestaurarCuerpoFisico(Vector3 pos, Quaternion rot)
    {
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            transform.position = pos;
            transform.rotation = rot;
            cc.enabled = true;
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        foreach (Renderer malla in GetComponentsInChildren<Renderer>(true))
        {
            malla.enabled = true;
        }
    }

    private System.Collections.IEnumerator CinematicaDespertar()
    {
        if (uiManagerLocal != null) uiManagerLocal.OcultarHUDModuloEspectador();

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

        if (miMov != null)
        {
            if (miMov.objetoBrazosFPS != null) miMov.objetoBrazosFPS.SetActive(true);
            if (miMov.armaManos != null) miMov.armaManos.SetActive(true);
        }

        Animator anim = GetComponentInChildren<Animator>(true);
        if (anim != null) anim.SetTrigger("Revive");

        if (miMov != null) miMov.enabled = false;

        ControladorArmasFPS armas = GetComponentInChildren<ControladorArmasFPS>(true);
        if (armas != null) armas.enabled = false;

        yield return new WaitForSeconds(2.5f);

        if (miMov != null) miMov.enabled = true;
        if (armas != null) armas.enabled = true;

        InventarioArmas miInventario = GetComponentInChildren<InventarioArmas>(true);
        if (miInventario != null) miInventario.RecibirNuevaArma(miInventario.armaPorDefecto);

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
            miMov.camaraPrincipal.transform.SetParent(cabeza);
            miMov.camaraPrincipal.SetActive(true);
            miMov.camaraPrincipal.transform.localPosition = new Vector3(0.7f, 1.5f, -2.5f);
            miMov.camaraPrincipal.transform.localRotation = Quaternion.identity;

            if (miMov.camaraArma != null)
            {
                miMov.camaraArma.transform.SetParent(cabeza);
                miMov.camaraArma.SetActive(true);
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
            }
        }
    }

    private void ManejarEfectosVisuales()
    {
        // Esto también queda intacto como lo tenías
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