using UnityEngine;
using UnityEngine.Serialization;
using Ajedrez.Core;

namespace Ajedrez.Debugging.Enfrentamiento
{
    [System.Serializable]
    public class MatchMessage
    {
        // Types of messages that can be sent
        public enum MessageType { None, RegisterPlayer, MakeMove, NewGame }

        [FormerlySerializedAs("tipoMensaje")]
        public MessageType messageType;

        // Data for the RegisterPlayer message
        [FormerlySerializedAs("nombreJugador")]
        public string playerName;

        // Data for the movement message
        [FormerlySerializedAs("nombreMovimiento")]
        public string moveName;
        [FormerlySerializedAs("profundidadBusquedaIterativa")]
        public int iterativeSearchDepth;

        // Data for the NewGame message
        [FormerlySerializedAs("fenInicial")]
        public string initialFen;
        [FormerlySerializedAs("colorPiezas")]
        public Piece.Color pieceColor;
        [FormerlySerializedAs("tiempoMaximoPensamientoMs")]
        public int maxThinkingTimeMs;

        // Method to create a player registration message
        public static MatchMessage CreateRegisterPlayerMessage(string playerName)
        {
            MatchMessage message = new MatchMessage()
            {
                messageType = MessageType.RegisterPlayer,
                playerName = playerName
            };

            return message;
        }

        // Method to create a new game message
        public static MatchMessage CreateNewGameMessage(string fen, Piece.Color color, int maxThinkingTimeMs)
        {
            MatchMessage message = new MatchMessage()
            {
                messageType = MessageType.NewGame,
                initialFen = fen,
                pieceColor = color,
                maxThinkingTimeMs = maxThinkingTimeMs
            };

            return message;
        }

        // Method to create a move message
        public static MatchMessage CreateMoveMessage(string move)
        {
            MatchMessage message = new MatchMessage()
            {
                messageType = MessageType.MakeMove,
                moveName = move
            };

            return message;
        }

        // Convert this message to JSON format
        public string ToJsonString()
        {
            return JsonUtility.ToJson(this);
        }

        // Create a message from a JSON string
        public static MatchMessage CreateFromJson(string json)
        {
            return JsonUtility.FromJson<MatchMessage>(json);
        }
    }
}