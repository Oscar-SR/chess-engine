using System;
using UnityEngine;
using Ajedrez.IA;

namespace Ajedrez.Core
{
    public class AIConfiguration
    {
        public const int DYNAMIC_TIME = -1;

        public enum DifficultyType : byte
        {
            Easy,
            Medium,
            Maximum,
            Custom
        }

        public DifficultyType Difficulty { get; private set; }

        public Busqueda.TipoBusqueda SearchLimit { get; private set; }
        public int Limit { get; private set; }

        public bool UseOpeningBook { get; private set; }
        public TextAsset OpeningBook { get; private set; }
        public int MaxBookMovement { get; private set; }

        // ✅ Constructores privados para evitar usos incorrectos
        private AIConfiguration() { }

        // 🔹 Preconfigurados
        public static AIConfiguration CreateEasy(TextAsset book)
        {
            return new AIConfiguration
            {
                Difficulty = DifficultyType.Easy,
                SearchLimit = Busqueda.TipoBusqueda.PorProfundidad,
                Limit = 2,
                UseOpeningBook = true,
                OpeningBook = book,
                MaxBookMovement = 2
            };
        }

        public static AIConfiguration CreateMedium(TextAsset book)
        {
            return new AIConfiguration
            {
                Difficulty = DifficultyType.Medium,
                SearchLimit = Busqueda.TipoBusqueda.PorProfundidad,
                Limit = 4,
                UseOpeningBook = true,
                OpeningBook = book,
                MaxBookMovement = 4
            };
        }

        public static AIConfiguration CreateMaximum(TextAsset book)
        {
            return new AIConfiguration
            {
                Difficulty = DifficultyType.Maximum,
                SearchLimit = Busqueda.TipoBusqueda.PorTiempo,
                Limit = DYNAMIC_TIME,
                UseOpeningBook = true,
                OpeningBook = book,
                MaxBookMovement = 8
            };
        }

        // 🔹 Personalizado
        public static AIConfiguration CreateCustom
        (
            Busqueda.TipoBusqueda searchLimit,
            int limit,
            bool useOpeningBook,
            int maxBookMovement,
            TextAsset book
        ) {
            if (useOpeningBook && book == null)
                throw new ArgumentException("A book is required if 'useOpeningBook' is true.");

            return new AIConfiguration
            {
                Difficulty = DifficultyType.Custom,
                SearchLimit = searchLimit,
                Limit = limit,
                UseOpeningBook = useOpeningBook,
                OpeningBook = book,
                MaxBookMovement = maxBookMovement
            };
        }

        public override string ToString()
        {
            string bookName = OpeningBook != null ? OpeningBook.name : "null";

            return $"AIConfiguration:\n" +
                   $"  Difficulty: {Difficulty}\n" +
                   $"  SearchLimit: {SearchLimit}\n" +
                   $"  Limit: {Limit}\n" +
                   $"  UseOpeningBook: {UseOpeningBook}\n" +
                   $"  OpeningBook: {bookName}\n" +
                   $"  MaxBookMovement: {MaxBookMovement}";
        }
    }
}
