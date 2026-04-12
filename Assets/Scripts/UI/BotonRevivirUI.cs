using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems; // Para los clics y el hover

// Añadimos IPointerEnterHandler y IPointerExitHandler para el Hover
public class BotonRevivirUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Referencias Visuales")]
    public Image barraProgreso;
    public TextMeshProUGUI textoInfo;

    [Header("Configuración de Tiempo")]
    public float tiempoParaRevivir = 5f;
    public AnimationCurve curvaDeLlenado;

    [HideInInspector] public ulong idJugadorMuerto;
    [HideInInspector] public int costeRevivir;

    private UIManager uiManagerLocal;
    private bool estaPulsado = false;
    private float progresoTiempo = 0f;

    // Guardaremos el tamaño original para el Hover
    private Vector3 escalaOriginal;

    private void Awake()
    {
        // Guardamos su tamaño nada más empezar
        escalaOriginal = transform.localScale;
    }

    public void ConfigurarBoton(ulong idJugador, string nombreJugador, int coste, UIManager manager)
    {
        idJugadorMuerto = idJugador;
        costeRevivir = coste;
        uiManagerLocal = manager;

        if (textoInfo != null) textoInfo.text = $"Revivir a {nombreJugador} - {coste} pts";

        estaPulsado = false;
        progresoTiempo = 0f;
        if (barraProgreso != null) barraProgreso.fillAmount = 0f;

        // Asegurarnos de que el tamaño es el normal al encenderse
        transform.localScale = escalaOriginal;

        gameObject.SetActive(true);
    }

    // --- EFECTO HOVER ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Cuando el ratón entra, lo hacemos un 5% más grande
        transform.localScale = escalaOriginal * 1.05f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Cuando el ratón sale, vuelve a su tamaño normal y cancelamos el clic por si acaso
        transform.localScale = escalaOriginal;
        estaPulsado = false;
        progresoTiempo = 0f;
        if (barraProgreso != null) barraProgreso.fillAmount = 0f;
    }

    // --- EFECTO CLIC ---
    public void OnPointerDown(PointerEventData eventData)
    {
        estaPulsado = true;
        // Opcional: Hacerlo un pelín más pequeño al pulsar para dar sensación de "botón físico"
        transform.localScale = escalaOriginal * 0.98f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        estaPulsado = false;
        progresoTiempo = 0f;
        if (barraProgreso != null) barraProgreso.fillAmount = 0f;

        // Al soltar, vuelve al tamaño de Hover (si sigue encima)
        transform.localScale = escalaOriginal * 1.05f;
    }

    private void Update()
    {
        if (estaPulsado)
        {
            progresoTiempo += Time.deltaTime / tiempoParaRevivir;

            if (barraProgreso != null)
            {
                barraProgreso.fillAmount = curvaDeLlenado.Evaluate(progresoTiempo);
            }

            if (progresoTiempo >= 1f)
            {
                estaPulsado = false;
                progresoTiempo = 0f;
                transform.localScale = escalaOriginal; // Lo devolvemos a la normalidad

                if (uiManagerLocal != null)
                {
                    uiManagerLocal.EjecutarCompraRevivir(idJugadorMuerto, costeRevivir);
                }
            }
        }
    }
}