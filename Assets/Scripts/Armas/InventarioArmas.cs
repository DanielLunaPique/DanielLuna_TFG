using UnityEngine;
using System.Collections;

public class InventarioArmas : MonoBehaviour
{
    [Header("Referencias")]
    public ControladorArmasFPS controladorFPS;
    public Transform contenedorArmas;

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
        ActualizarVisibilidadArmas();
        ConfigurarArmaEnControlador();

        if (armaPorDefecto != null) RecibirNuevaArma(armaPorDefecto);
    }

    void Update()
    {
        if (Input.GetAxis("Mouse ScrollWheel") != 0f && !estaCambiandoArma && !controladorFPS.estaRecargando)
        {
            CambiarArmaSiguiente();
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

        // Trabajamos con el CONTENEDOR, no con el arma
        Vector3 posOriginalContenedor = Vector3.zero;
        Vector3 posAbajoContenedor = posOriginalContenedor - new Vector3(0, distanciaBajarCambio, 0);

        // 1. BAJAMOS EL CONTENEDOR CON EL ARMA VIEJA
        float t = 0;
        while (t < tiempoMedio)
        {
            t += Time.deltaTime;
            contenedorArmas.localPosition = Vector3.Lerp(posOriginalContenedor, posAbajoContenedor, t / tiempoMedio);
            yield return null;
        }

        // 2. EL SWAP MÁGICO (Cambiamos las armas mientras están abajo y no se ven)
        indiceArmaActiva = nuevoIndice;
        ActualizarVisibilidadArmas();
        ConfigurarArmaEnControlador();

        // 3. SUBIMOS EL CONTENEDOR CON EL ARMA NUEVA
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