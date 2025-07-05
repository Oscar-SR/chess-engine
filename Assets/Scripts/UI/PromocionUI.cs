using UnityEngine;
using UnityEngine.UI;
using System;
using Ajedrez.Core;
using Ajedrez.Utilities;

namespace Ajedrez.UI
{
    public class PromocionUI : MonoBehaviour
    {
        [SerializeField] private GameObject panelPromocionBlancas;
        [SerializeField] private GameObject panelPromocionNegras;
        private GameObject panelPromocionActual;
        private Action<int> callbackSeleccion;
        
        public void SeleccionarReina() => Seleccionar(Movimiento.PROMOVER_A_REINA);
        public void SeleccionarCaballo() => Seleccionar(Movimiento.PROMOVER_A_CABALLO);
        public void SeleccionarTorre() => Seleccionar(Movimiento.PROMOVER_A_TORRE);
        public void SeleccionarAlfil() => Seleccionar(Movimiento.PROMOVER_A_ALFIL);

        public void Init(bool blancasAbajo = true)
        {
            if (!blancasAbajo)
            {
                // Intercambiar posición de los paneles de promoción
                Vector3 temp = panelPromocionBlancas.transform.position;
                panelPromocionBlancas.transform.position = panelPromocionNegras.transform.position;
                panelPromocionNegras.transform.position = temp;
            }
        }

        public void Mostrar(Pieza.Color color, Action<int> onSeleccion)
        {
            callbackSeleccion = onSeleccion;

            if (AjedrezUtils.MismoColor(color, Pieza.Color.Blancas))
            {
                panelPromocionActual = panelPromocionBlancas;
            }
            else
            {
                panelPromocionActual = panelPromocionNegras;
            }
            panelPromocionActual.SetActive(true);
        }

        private void Ocultar()
        {
            panelPromocionActual.SetActive(false);
        }

        private void Seleccionar(int tipo)
        {
            Ocultar();
            callbackSeleccion?.Invoke(tipo);
        }
    }
}