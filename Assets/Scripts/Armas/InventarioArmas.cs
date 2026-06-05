using UnityEngine;
using System.Collections;

public class InventarioArmas : MonoBehaviour
{
    [Header("Referencias")]
    public ControladorArmasFPS controladorFPS;
    public Transform contenedorArmas;

    [Header("Multijugador")]
    [Tooltip("Arrastra aquí la raíz de tu jugador que tiene el script SincronizacionTerceraPersona")]
    public SincronizacionTerceraPersona sincronizadorTP;

    [Header("Arsenal (Máximo 2)")]
    public DatosArma[] armasEquipadas = new DatosArma[2];

    [Header("Arma Inicial")]
    public EstadisticasArma armaPorDefecto;

    [Header("Animación de Cambio")]
    public float distanciaBajarCambio = 0.8f;
    public float duracionTotalCambio = 0.8f;

    public int indiceArmaActiva = 0;
    private bool estaCambiandoArma = false;

    void Start()
    {
        // Intento de auto-asignación por si se te olvida ponerlo en el Inspector
        if (sincronizadorTP == null)
        {
            sincronizadorTP = GetComponentInParent<SincronizacionTerceraPersona>();
        }

        // Al empezar la partida, limpiamos y damos el arma inicial
        ResetearInventario();
    }

    void Update()
    {
        if (Input.GetAxis("Mouse ScrollWheel") != 0f && !estaCambiandoArma && !controladorFPS.estaRecargando)
        {
            CambiarArmaSiguiente();
        }
    }

    // --- NUEVA FUNCIÓN: LIMPIEZA TOTAL ---
    public void ResetearInventario()
    {
        for (int i = 0; i < armasEquipadas.Length; i++)
        {
            if (armasEquipadas[i] != null)
            {
                Destroy(armasEquipadas[i].gameObject);
                armasEquipadas[i] = null;
            }
        }

        indiceArmaActiva = 0;

        if (armaPorDefecto != null)
        {
            RecibirNuevaArma(armaPorDefecto);
        }
        else
        {
            ActualizarVisibilidadArmas();
            ConfigurarArmaEnControlador();
        }
    }

    public void RecibirNuevaArma(EstadisticasArma statsNuevaArma)
    {
        if (statsNuevaArma.prefabArma == null) return;

        int huecoDestino = indiceArmaActiva;
        for (int i = 0; i < armasEquipadas.Length; i++)
        {
            if (armasEquipadas[i] == null)
            {
                huecoDestino = i;
                break;
            }
        }

        if (armasEquipadas[huecoDestino] != null) Destroy(armasEquipadas[huecoDestino].gameObject);

        GameObject nuevaArmaObj = Instantiate(statsNuevaArma.prefabArma, contenedorArmas);
        nuevaArmaObj.transform.localPosition = statsNuevaArma.position;
        nuevaArmaObj.transform.localRotation = Quaternion.Euler(statsNuevaArma.rotation);

        DatosArma nuevosDatos = nuevaArmaObj.GetComponent<DatosArma>();
        armasEquipadas[huecoDestino] = nuevosDatos;

        StartCoroutine(RutinaCambioArma(huecoDestino));
    }

    public bool TieneArma(EstadisticasArma armaAComprobar)
    {
        foreach (var arma in armasEquipadas)
        {
            if (arma != null && arma.estadisticas.nombreArma == armaAComprobar.nombreArma) return true;
        }
        return false;
    }

    private void CambiarArmaSiguiente()
    {
        int nuevoIndice = indiceArmaActiva == 0 ? 1 : 0;

        if (armasEquipadas[nuevoIndice] != null)
        {
            StartCoroutine(RutinaCambioArma(nuevoIndice));
        }
    }

    private IEnumerator RutinaCambioArma(int nuevoIndice)
    {
        estaCambiandoArma = true;
        float tiempoMedio = duracionTotalCambio / 2f;

        Vector3 posOriginalContenedor = Vector3.zero;
        Vector3 posAbajoContenedor = posOriginalContenedor - new Vector3(0, distanciaBajarCambio, 0);

        float t = 0;
        while (t < tiempoMedio)
        {
            t += Time.deltaTime;
            contenedorArmas.localPosition = Vector3.Lerp(posOriginalContenedor, posAbajoContenedor, t / tiempoMedio);
            yield return null;
        }

        indiceArmaActiva = nuevoIndice;
        ActualizarVisibilidadArmas();
        ConfigurarArmaEnControlador();

        t = 0;
        while (t < tiempoMedio)
        {
            t += Time.deltaTime;
            contenedorArmas.localPosition = Vector3.Lerp(posAbajoContenedor, posOriginalContenedor, t / tiempoMedio);
            yield return null;
        }

        contenedorArmas.localPosition = posOriginalContenedor;
        estaCambiandoArma = false;
    }

    private void ActualizarVisibilidadArmas()
    {
        for (int i = 0; i < armasEquipadas.Length; i++)
        {
            if (armasEquipadas[i] != null)
            {
                armasEquipadas[i].gameObject.SetActive(i == indiceArmaActiva);
            }
        }
    }

    private void ConfigurarArmaEnControlador()
    {
        if (controladorFPS != null && armasEquipadas[indiceArmaActiva] != null)
        {
            controladorFPS.armaActual = armasEquipadas[indiceArmaActiva];
            controladorFPS.RecalcularPuntoDeMira();
            controladorFPS.ActualizarAgarresIK();

            // ========================================================
            // --- FIX: AVISAMOS A LA RED DEL CAMBIO DE ARMA ---
            // ========================================================
            if (sincronizadorTP != null)
            {
                sincronizadorTP.NotificarCambioArma(armasEquipadas[indiceArmaActiva].estadisticas.nombreArma);
            }
        }
    }

    public void RellenarMunicion(EstadisticasArma statsBuscadas)
    {
        foreach (DatosArma arma in armasEquipadas)
        {
            if (arma != null && arma.estadisticas == statsBuscadas)
            {
                arma.balasReserva = arma.estadisticas.balasCargador * 9;
                return;
            }
        }
    }

    public void QuitarArma(EstadisticasArma statsBuscadas)
    {
        for (int i = 0; i < armasEquipadas.Length; i++)
        {
            if (armasEquipadas[i] != null && armasEquipadas[i].estadisticas == statsBuscadas)
            {
                Destroy(armasEquipadas[i].gameObject);
                armasEquipadas[i] = null;

                int otroIndice = (i == 0) ? 1 : 0;

                if (armasEquipadas[otroIndice] != null)
                {
                    StartCoroutine(RutinaCambioArma(otroIndice));
                }
                break;
            }
        }
    }
}