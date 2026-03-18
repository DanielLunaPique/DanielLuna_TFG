using Unity.VisualScripting;
using UnityEngine;
using static EstadisticasArma;

public class SistemaDisparoFPS : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Arrastra aquí el script ControladorArmasFPS para saber qué arma tenemos en las manos")]
    public ControladorArmasFPS controladorFPS;
    [Tooltip("La cámara desde la que salen los disparos (Main Camera)")]
    public Camera camaraPrincipal;

    [Header("Configuración de Impactos")]
    [Tooltip("Selecciona la capa 'Jugador'. El raycast ignorará esta capa.")]
    public LayerMask capaIgnorar;
    public float distanciaDisparo = 200f;

    // Temporizador para la cadencia
    private float tiempoProximoDisparo = 0f;
    private bool disparandoRafaga = false;

    [Tooltip("Arrastra aquí tu Prefab de agujero de bala")]
    public GameObject prefabAgujeroBala;

    [Tooltip("Arrastra aquí tu Main Camera que tiene el nuevo script")]
    public ControladorCamaraFPS controladorCamara;

    void Update()
    {
        // Si no tenemos arma, no hacemos nada
        if (controladorFPS == null || controladorFPS.armaActual == null) return;

        if (controladorFPS.estaRecargando || disparandoRafaga) return;

        DatosArma arma = controladorFPS.armaActual;
        EstadisticasArma stats = arma.estadisticas;

        // Comprobamos el modo de disparo de nuestro ScriptableObject
        if (stats.modoDisparo == TipoDisparo.Automatico)
        {
            // GetMouseButton (Mantenido pulsado)
            if (Input.GetMouseButton(0) && Time.time >= tiempoProximoDisparo)
            {
                IntentarDisparar(arma, stats);
            }
        }
        else if (stats.modoDisparo == TipoDisparo.Semiautomatico)
        {
            // GetMouseButtonDown (Un solo clic)
            if (Input.GetMouseButtonDown(0) && Time.time >= tiempoProximoDisparo)
            {
                IntentarDisparar(arma, stats);
            }
        }

        else if (stats.modoDisparo == TipoDisparo.Rafaga) // <--- NUEVO MODO
        {
            if (Input.GetMouseButtonDown(0) && Time.time >= tiempoProximoDisparo)
            {
                // En vez de disparar directamente, arrancamos la secuencia de ráfaga
                StartCoroutine(DispararRafaga(arma, stats));
            }
        }
    }

    private System.Collections.IEnumerator DispararRafaga(DatosArma arma, EstadisticasArma stats)
    {
        disparandoRafaga = true; // Bloqueamos el arma

        for (int i = 0; i < arma.estadisticas.balasPorRafaga; i++)
        {
            // Si nos quedamos sin balas a mitad de la ráfaga (ej: quedaban 2 y la ráfaga es de 3), paramos de disparar.
            if (arma.balasActuales <= 0) break;

            // Disparamos una bala normal (esto aplica el retroceso de arma, cámara, agujeros, etc.)
            IntentarDisparar(arma, stats);

            // Pausamos la corrutina los milisegundos que dijimos antes de disparar la siguiente
            yield return new WaitForSeconds(arma.estadisticas.cadenciaDisparo);
        }

        // Una vez terminada la ráfaga, aplicamos la cadencia normal para evitar que el jugador "spamee" clics
        tiempoProximoDisparo = Time.time + stats.cadenciaDisparo;

        disparandoRafaga = false; // Desbloqueamos el arma
    }

    void IntentarDisparar(DatosArma arma, EstadisticasArma stats)
    {
        if (arma.balasActuales <= 0)
        {
            // Aquí en el futuro pondremos el sonido de *Click* de cargador vacío
            return;
        }

        // Restamos bala y aplicamos cadencia
        arma.balasActuales--;
        tiempoProximoDisparo = Time.time + stats.cadenciaDisparo;


        // 1. Calculamos la dispersión total
        float dispersionTotal = (arma.dispersionBase * controladorFPS.multiplicadorDispersion) + controladorFPS.penalizacionDisparo;

        // 2. Dirección base de la cámara
        Vector3 direccionBase = camaraPrincipal.transform.forward;

        // 3. Le sumamos un vector aleatorio dentro de una esfera, multiplicado por la dispersión
        Vector3 direccionConDispersion = direccionBase + (Random.insideUnitSphere * dispersionTotal);

        // 4. Creamos el Raycast con la nueva dirección torcida
        Ray rayo = new Ray(camaraPrincipal.transform.position, direccionConDispersion.normalized);
        RaycastHit impacto;

        // Lanzamos el rayo. El símbolo ~ significa "Invierte la máscara" (Choca con todo MENOS con esta capa)
        // Lanzamos el rayo
        if (Physics.Raycast(rayo, out impacto, distanciaDisparo, ~capaIgnorar))
        {
            Zombie enemigo = impacto.collider.GetComponent<Zombie>();

            if (enemigo != null)
            {
                // Conseguimos nuestro "DNI" de jugador (nuestro ID en el servidor)
                ulong miDNI = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
                enemigo.TakeDamageServerRpc(stats.daño, miDNI);

                HitmarkerFPS hitmarker = GetComponentInChildren<HitmarkerFPS>();
                if (hitmarker != null) hitmarker.MostrarHitmarker();
            }

            // 1. INSTANCIAR AGUJERO DE BALA
            else if (prefabAgujeroBala != null)
            {
                // Un truco profesional: Separamos el agujero un milímetro de la pared (normal * 0.001f) 
                // para que la textura de la pared y la del agujero no se peleen visualmente (Z-Fighting)
                Vector3 posicionAgujero = impacto.point + (impacto.normal * 0.001f);

                // Rotamos el agujero para que se pegue plano contra la pared, suelo o techo
                Quaternion rotacionAgujero = Quaternion.LookRotation(-impacto.normal);

                Instantiate(prefabAgujeroBala, posicionAgujero, rotacionAgujero);
            }
        }

        if (controladorFPS != null)
        {
            controladorFPS.AplicarRetroceso();
        }

        // 3. APLICAMOS EL RETROCESO ESTILO CoD A LA CÁMARA
        if (controladorCamara != null)
        {
            controladorCamara.RecibirRetroceso(
                arma.recoilCamaraArriba,
                arma.recoilCamaraLado,
                arma.velocidadRetrocesoCamara,
                arma.topeRetrocesoVertical,
                arma.fuerzaTironRegreso
            );
        }
    }
}