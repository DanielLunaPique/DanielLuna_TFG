using UnityEngine;

public class InventarioArmas : MonoBehaviour
{
    [Header("Referencias")]
    public ControladorArmasFPS controladorFPS;

    [Header("Arsenal")]
    public DatosArma[] armasEquipadas = new DatosArma[2];

    private int indiceArmaActiva = -1;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ApagarTodasLasArmas();
        EquiparArma(0);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetAxis("Mouse ScrollWheel") != 0f)
        {
            CambiarArmaSiguiente();
        }
    }

    public void EquiparArma(int indice)
    {
        if (armasEquipadas[indice] == null) return;
        if (indice == indiceArmaActiva) return;

        ApagarTodasLasArmas();

        armasEquipadas[indice].gameObject.SetActive(true);
        indiceArmaActiva = indice;

        if(controladorFPS != null)
        {
            controladorFPS.armaActual = armasEquipadas[indice];
            controladorFPS.RecalcularPuntoDeMira();
            controladorFPS.ActualizarAgarresIK();
        }
    }

    private void CambiarArmaSiguiente()
    {
        // Alternar entre el hueco 0 y el 1
        int nuevoIndice = indiceArmaActiva == 0 ? 1 : 0;
        EquiparArma(nuevoIndice);
    }

    private void ApagarTodasLasArmas()
    {
        foreach(DatosArma arma in armasEquipadas)
        {
            if(arma != null)
            {
                arma.gameObject.SetActive(false);
            }
        }
    }
}
