using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using TMPro;

namespace Ajedrez.Debugging.Enfrentamiento
{
    public class RemotePlayerUI : MonoBehaviour
    {
        [SerializeField] [FormerlySerializedAs("jugadorEnfrentamientoManager")] private RemotePlayerManager remotePlayerManager;
        [SerializeField] [FormerlySerializedAs("colorVerde")] private Color connectedColor;
        [SerializeField] [FormerlySerializedAs("TMP_nombre")] private TMP_Text nameText;
        [SerializeField] [FormerlySerializedAs("TMP_conectado")] private TMP_Text connectedText;
        [SerializeField] [FormerlySerializedAs("TMP_tablero")] private TMP_Text boardText;
        [SerializeField] [FormerlySerializedAs("TMP_colorPiezas")] private TMP_Text pieceColorText;
        [SerializeField] [FormerlySerializedAs("TMP_tiempoPensar")] private TMP_Text thinkingTimeText;
        [SerializeField] [FormerlySerializedAs("TMP_movimientoRecibido")] private TMP_Text receivedMoveText;
        [SerializeField] [FormerlySerializedAs("TMP_movimientoEnviado")] private TMP_Text sentMoveText;
        [SerializeField] [FormerlySerializedAs("textoDebug")] private TMP_Text debugText;
        [SerializeField] [FormerlySerializedAs("enlaceCommitGithub")] private string githubCommitLink;

        // Start is called before the first frame update
        void Start()
        {
            // Set up window settings
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            Application.runInBackground = true;
        }

        public void ShowConnected()
        {
            connectedText.text = "Connected";
            connectedText.color = connectedColor;
        }

        public void ShowName(string name)
        {
            nameText.text = name;
        }

        public void ShowBoard(string fen)
        {
            boardText.text = fen;
        }

        public void ShowPieceColor(string pieceColor)
        {
            pieceColorText.text = pieceColor;
        }

        public void ShowThinkingTime(string thinkingTime)
        {
            thinkingTimeText.text = thinkingTime;
        }

        public void ShowReceivedMove(string receivedMove)
        {
            receivedMoveText.text = receivedMove;
        }

        public void ShowSentMove(string sentMove)
        {
            sentMoveText.text = sentMove;
        }

        public void OpenCommit()
        {
            Application.OpenURL(githubCommitLink);
        }

        public void WriteDebug(string message)
        {
            debugText.text += message;
        }
    }
}
