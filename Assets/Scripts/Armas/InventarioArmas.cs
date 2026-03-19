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

        // 1. DESTRUIR EL ARMA VIEJA (Si tenemos las manos llenas)
        if (armasEquipadas[indiceArmaActiva] != null)
        {
            Debug.Log($"Destruyendo {armasEquipadas[indiceArmaActiva].gameObject.name} para hacer hueco.");
            Destroy(armasEquipadas[indiceArmaActiva].gameObject);
        }

        // 2. INSTANCIAR EL ARMA NUEVA
        // La creamos y la hacemos hija del contenedor (para que se mueva con la cámara)
        GameObject nuevaArmaObj = Instantiate(statsNuevaArma.prefabArma, contenedorArmas);

        // 3. APLICAR POSICIÓN Y ROTACIÓN DEL SCRIPTABLE OBJECT
        nuevaArmaObj.transform.localPosition = statsNuevaArma.position;
        nuevaArmaObj.transform.localRotation = Quaternion.Euler(statsNuevaArma.rotation);

        // 4. GUARDAR LOS DATOS EN NUESTRO BOLSILLO (Inventario)
        DatosArma nuevosDatos = nuevaArmaObj.GetComponent<DatosArma>();
        armasEquipadas[indiceArmaActiva] = nuevosDatos;

        // 5. EQUIPARLA VISUAL Y FÍSICAMENTE
        ActualizarVisibilidadArmas();
        ConfigurarArmaEnControlador();

        Debug.Log($"¡Has recibido un/a {statsNuevaArma.nombreArma}!");
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
}