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
            return;
        }

        arma.balasActuales--;
        tiempoProximoDisparo = Time.time + stats.cadenciaDisparo;

        float dispersionTotal = (arma.dispersionBase * controladorFPS.multiplicadorDispersion) + controladorFPS.penalizacionDisparo;
        Vector3 direccionBase = camaraPrincipal.transform.forward;
        Vector3 direccionConDispersion = direccionBase + (Random.insideUnitSphere * dispersionTotal);

        Ray rayo = new Ray(camaraPrincipal.transform.position, direccionConDispersion.normalized);

        RaycastHit[] impactos = Physics.RaycastAll(rayo, distanciaDisparo, ~capaIgnorar);

        // Si hemos chocado con al menos una cosa...
        if (impactos.Length > 0)
        {
            // 1. RaycastAll devuelve las cosas desordenadas. Tenemos que ordenarlas por distancia al jugador.
            System.Array.Sort(impactos, (x, y) => x.distance.CompareTo(y.distance));

            int zombiesAtravesados = 0;
            int maxZombiesAtravesables = 6; // El primero (100%) + 5 detrás (75%)
            ulong miDNI = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
            bool hemosDadoAUnZombie = false;

            // 2. Recorremos todo lo que ha atravesado la bala, del más cercano al más lejano
            foreach (RaycastHit impacto in impactos)
            {
                //Ahora buscamos la ParteDelCuerpo, no al Zombie entero
                ParteDelCuerpo hitbox = impacto.collider.GetComponent<ParteDelCuerpo>();

                if (hitbox != null)
                {
                    if (zombiesAtravesados < maxZombiesAtravesables)
                    {
                        // Si es el primero, daño 100%. Si es el segundo o más, daño 75%.
                        float porcentajePenetracion = (zombiesAtravesados == 0) ? 1.0f : 0.75f;
                        int dañoCalculado = Mathf.RoundToInt(stats.daño * porcentajePenetracion);

                        // Le pasamos el daño a la Hitbox, y ella se encarga de multiplicarlo si es la cabeza o el pie
                        hitbox.RecibirDisparo(dañoCalculado, miDNI);
                        hemosDadoAUnZombie = true;

                        zombiesAtravesados++;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    // Si NO es un zombie (es una pared, suelo, etc.)
                    if (prefabAgujeroBala != null)
                    {
                        Vector3 posicionAgujero = impacto.point + (impacto.normal * 0.001f);
                        Quaternion rotacionAgujero = Quaternion.LookRotation(-impacto.normal);
                        Instantiate(prefabAgujeroBala, posicionAgujero, rotacionAgujero);
                    }

                    break;
                }
            }

            // Si al final del rayo le hemos dado a al menos un zombie, mostramos la cruceta de hit.
            if (hemosDadoAUnZombie)
            {
                HitmarkerFPS hitmarker = GetComponentInChildren<HitmarkerFPS>();
                if (hitmarker != null) hitmarker.MostrarHitmarker();
            }
        }

        if (controladorFPS != null)
        {
            controladorFPS.AplicarRetroceso();
        }

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