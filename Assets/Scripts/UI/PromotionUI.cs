using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using System;
using Ajedrez.Core;
using Ajedrez.Utilities;

namespace Ajedrez.UI
{
    public class PromotionUI : MonoBehaviour
    {
        [SerializeField] [FormerlySerializedAs("panelPromocionBlancas")] private GameObject whitePromotionPanel;
        [SerializeField] [FormerlySerializedAs("panelPromocionNegras")] private GameObject blackPromotionPanel;
        private GameObject currentPromotionPanel;
        private Action<int> selectionCallback;

        public void SelectQueen() => Select(Move.PROMOTE_TO_QUEEN);
        public void SelectKnight() => Select(Move.PROMOTE_TO_KNIGHT);
        public void SelectRook() => Select(Move.PROMOTE_TO_ROOK);
        public void SelectBishop() => Select(Move.PROMOTE_TO_BISHOP);

        public void Init(bool whiteOnBottom = true)
        {
            if (!whiteOnBottom)
            {
                // Swap the position of the promotion panels
                Vector3 temp = whitePromotionPanel.transform.position;
                whitePromotionPanel.transform.position = blackPromotionPanel.transform.position;
                blackPromotionPanel.transform.position = temp;
            }
        }

        public void Show(Piece.Color color, Action<int> onSelection)
        {
            selectionCallback = onSelection;

            if (ChessUtils.SameColor(color, Piece.Color.White))
            {
                currentPromotionPanel = whitePromotionPanel;
            }
            else
            {
                currentPromotionPanel = blackPromotionPanel;
            }
            currentPromotionPanel.SetActive(true);
        }

        private void Hide()
        {
            currentPromotionPanel.SetActive(false);
        }

        private void Select(int promotionType)
        {
            Hide();
            selectionCallback?.Invoke(promotionType);
        }
    }
}
