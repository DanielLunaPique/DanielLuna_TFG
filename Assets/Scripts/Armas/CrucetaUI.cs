using UnityEngine;

public class CrucetaUI : MonoBehaviour
{
    public ControladorArmasFPS controlador;

    [Header("Piezas de la Cruceta")]
    public RectTransform lineaArriba;
    public RectTransform lineaAbajo;
    public RectTransform lineaIzquierda;
    public RectTransform lineaDerecha;

    [Header("Ajustes Visuales")]
    public float separacionBase = 20f;
    public float multiplicadorVisual = 2000f; // Ajusta esto si la cruceta se abre mucho o poco
    public float velocidadSuavizado = 15f;

    private float separacionActual;

    void Update()
    {
        if (controlador == null || controlador.armaActual == null) return;

        // Calculamos cuánto debería abrirse
        float dispersion = (controlador.armaActual.dispersionBase * controlador.multiplicadorDispersion) + controlador.penalizacionDisparo;

        // Ocultar cruceta al apuntar
        float opacidad = controlador.multiplicadorDispersion <= 0.1f ? 0f : 1f;
        SetAlpha(opacidad);

        float separacionObjetivo = separacionBase + (dispersion * multiplicadorVisual);

        // Movimiento suave de las líneas
        separacionActual = Mathf.Lerp(separacionActual, separacionObjetivo, Time.deltaTime * velocidadSuavizado);

        // Aplicamos la posición a los RectTransforms
        lineaArriba.anchoredPosition = new Vector2(0, separacionActual);
        lineaAbajo.anchoredPosition = new Vector2(0, -separacionActual);
        lineaIzquierda.anchoredPosition = new Vector2(-separacionActual, 0);
        lineaDerecha.anchoredPosition = new Vector2(separacionActual, 0);
    }

    void SetAlpha(float alpha)
    {
        // Un truquito rápido para ocultarla al apuntar (Ads)
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        cg.alpha = Mathf.Lerp(cg.alpha, alpha, Time.deltaTime * 10f);
    }
}