using UnityEngine;
using Chess.Core;

namespace Chess.Debugging.Versus
{
    [System.Serializable]
    public class VersusMessage
    {
        // Types of messages that can be sent
        public enum MessageType { None, RegisterPlayer, MakeMove, NewGame }

        public MessageType messageType;

        // Data for the RegisterPlayer message
        public string playerName;

        // Data for the movement message
        public string moveName;
        public int iterativeSearchDepth;

        // Data for the NewGame message
        public string initialFen;
        public Piece.Color pieceColor;
        public int maxThinkingTimeMs;

        // Method to create a player registration message
        public static VersusMessage CreateRegisterPlayerMessage(string playerName)
        {
            VersusMessage message = new VersusMessage()
            {
                messageType = MessageType.RegisterPlayer,
                playerName = playerName
            };

            return message;
        }

        // Method to create a new game message
        public static VersusMessage CreateNewGameMessage(string fen, Piece.Color color, int maxThinkingTimeMs)
        {
            VersusMessage message = new VersusMessage()
            {
                messageType = MessageType.NewGame,
                initialFen = fen,
                pieceColor = color,
                maxThinkingTimeMs = maxThinkingTimeMs
            };

            return message;
        }

        // Method to create a move message
        public static VersusMessage CreateMoveMessage(string move)
        {
            VersusMessage message = new VersusMessage()
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
        public static VersusMessage CreateFromJson(string json)
        {
            return JsonUtility.FromJson<VersusMessage>(json);
        }
    }
}