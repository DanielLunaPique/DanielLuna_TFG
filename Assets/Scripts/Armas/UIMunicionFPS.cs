using UnityEngine;
using TMPro; // Necesario para usar TextMeshPro

public class UIMunicionFPS : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El script que sabe qué arma tenemos equipada")]
    public ControladorArmasFPS controladorFPS;

    [Tooltip("El componente de texto de la UI")]
    public TextMeshProUGUI textoMunicion;

    void Update()
    {
        // Si no tenemos arma o nos falta alguna referencia, ocultamos el texto
        if (controladorFPS == null || controladorFPS.armaActual == null || textoMunicion == null)
        {
            if (textoMunicion != null) textoMunicion.text = "";
            return;
        }

        DatosArma arma = controladorFPS.armaActual;

        // Escribimos el formato clásico: "30 / 90"
        textoMunicion.text = arma.balasActuales.ToString() + " / " + arma.balasReserva.ToString();

        // Detalle visual: Si el cargador está vacío, el texto se pone rojo
        if (arma.balasActuales <= 0)
        {
            textoMunicion.color = Color.red;
        }
        else
        {
            textoMunicion.color = Color.white;
        }
    }
}