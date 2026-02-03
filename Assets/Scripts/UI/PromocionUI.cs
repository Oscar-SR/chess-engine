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
        
        public void SeleccionarReina() => Seleccionar(Move.PROMOTE_TO_QUEEN);
        public void SeleccionarCaballo() => Seleccionar(Move.PROMOTE_TO_KNIGHT);
        public void SeleccionarTorre() => Seleccionar(Move.PROMOTE_TO_ROOK);
        public void SeleccionarAlfil() => Seleccionar(Move.PROMOTE_TO_BISHOP);

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

        public void Mostrar(Piece.Color color, Action<int> onSeleccion)
        {
            callbackSeleccion = onSeleccion;

            if (AjedrezUtils.MismoColor(color, Piece.Color.White))
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