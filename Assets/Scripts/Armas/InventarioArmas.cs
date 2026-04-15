using UnityEngine;


public class InventarioArmas : MonoBehaviour
{
    [Header("Referencias")]
    public ControladorArmasFPS controladorFPS;

    [Tooltip("El objeto padre donde aparecerán las armas (ej. el pivote de la cámara o de los brazos)")]
    public Transform contenedorArmas;

    [Header("Arsenal (Máximo 2)")]
    public DatosArma[] armasEquipadas = new DatosArma[2];

    [Header("Arma Inicial")]
    public EstadisticasArma armaPorDefecto;

    // Lo hacemos público para poder verlo en el inspector mientras testeamos
    public int indiceArmaActiva = 0;


    void Start()
    {
        // Al empezar la partida, apagamos todo y preparamos lo que haya
        ActualizarVisibilidadArmas();
        ConfigurarArmaEnControlador();

        if (armaPorDefecto != null)
        {
            RecibirNuevaArma(armaPorDefecto);
        }
    }

    void Update()
    {
        if (Input.GetAxis("Mouse ScrollWheel") != 0f)
        {
            CambiarArmaSiguiente();
        }
    }

    // --- COMPRAR/RECIBIR UN ARMA ---
    public void RecibirNuevaArma(EstadisticasArma statsNuevaArma)
    {
        if (statsNuevaArma.prefabArma == null)
        {
            Debug.LogError("Las estadísticas no tienen un prefab asignado.");
            return;
        }

        // Por defecto, asumimos que vamos a reemplazar el arma que tenemos en las manos
        int huecoDestino = indiceArmaActiva;

        // 1. BUSCAR UN HUECO LIBRE PRIMERO (Por si solo tenemos 1 arma)
        for (int i = 0; i < armasEquipadas.Length; i++)
        {
            if (armasEquipadas[i] == null)
            {
                huecoDestino = i; // ¡Hemos encontrado un bolsillo vacío!
                break; // Paramos de buscar
            }
        }

        // 2. DESTRUIR EL ARMA VIEJA (Solo si el hueco destino ya estaba ocupado)
        if (armasEquipadas[huecoDestino] != null)
        {
            Debug.Log($"Destruyendo {armasEquipadas[huecoDestino].gameObject.name} para hacer hueco.");
            Destroy(armasEquipadas[huecoDestino].gameObject);
        }

        // 3. INSTANCIAR EL ARMA NUEVA
        GameObject nuevaArmaObj = Instantiate(statsNuevaArma.prefabArma, contenedorArmas);

        // 4. APLICAR POSICIÓN Y ROTACIÓN DEL SCRIPTABLE OBJECT
        nuevaArmaObj.transform.localPosition = statsNuevaArma.position;
        nuevaArmaObj.transform.localRotation = Quaternion.Euler(statsNuevaArma.rotation);

        // 5. GUARDAR LOS DATOS EN EL HUECO DESTINO
        DatosArma nuevosDatos = nuevaArmaObj.GetComponent<DatosArma>();
        armasEquipadas[huecoDestino] = nuevosDatos;

        // 6. CAMBIAR NUESTRO ÍNDICE ACTIVO A ESTE NUEVO HUECO (Para tenerla en las manos al instante)
        indiceArmaActiva = huecoDestino;

        // 7. EQUIPARLA VISUAL Y FÍSICAMENTE
        ActualizarVisibilidadArmas();
        ConfigurarArmaEnControlador();

        Debug.Log($"¡Has recibido un/a {statsNuevaArma.nombreArma} en el hueco {huecoDestino}!");
    }
    public bool TieneArma(EstadisticasArma armaAComprobar)
    {
        // Asumiendo que tienes un array o lista de armas equipadas. 
        // Adapta "misArmas" al nombre de tu variable donde guardas las armas actuales.
        foreach (var arma in armasEquipadas)
        {
            if (arma != null && arma.estadisticas.nombreArma == armaAComprobar.nombreArma)
            {
                return true; // Ya la tiene
            }
        }
        return false; // No la tiene
    }

    // --- SISTEMA DE CAMBIO DE ARMA ---
    private void CambiarArmaSiguiente()
    {
        int nuevoIndice = indiceArmaActiva == 0 ? 1 : 0;

        // SOLO cambiamos si realmente tenemos un arma en ese hueco secundario
        if (armasEquipadas[nuevoIndice] != null)
        {
            indiceArmaActiva = nuevoIndice;
            ActualizarVisibilidadArmas();
            ConfigurarArmaEnControlador();
        }
    }

    // --- FUNCIONES DE LIMPIEZA INTERNA ---
    private void ActualizarVisibilidadArmas()
    {
        for (int i = 0; i < armasEquipadas.Length; i++)
        {
            if (armasEquipadas[i] != null)
            {
                // Solo se activa la que coincide con nuestro índice actual
                armasEquipadas[i].gameObject.SetActive(i == indiceArmaActiva);
            }
        }
    }

    private void ConfigurarArmaEnControlador()
    {
        if (controladorFPS != null && armasEquipadas[indiceArmaActiva] != null)
        {
            controladorFPS.armaActual = armasEquipadas[indiceArmaActiva];

            // Recalculamos la mira y el IK como ya tenías programado de forma excelente
            controladorFPS.RecalcularPuntoDeMira();
            controladorFPS.ActualizarAgarresIK();
        }
    }

    public void RellenarMunicion(EstadisticasArma statsBuscadas)
    {
        foreach (DatosArma arma in armasEquipadas)
        {
            // Buscamos si tenemos el arma exacta de la pared
            if (arma != null && arma.estadisticas == statsBuscadas)
            {
                // Llenamos la reserva al máximo 
                arma.balasReserva = arma.estadisticas.balasCargador * 9;
                Debug.Log($"¡Munición al máximo para {statsBuscadas.nombreArma}!");
                return;
            }
        }
    }
}