using System;
using UnityEngine;
using Ajedrez.IA;

namespace Ajedrez.Core
{
    public class ConfiguracionIA
    {
        public const int TIEMPO_DINAMICO = -1;

        public enum TipoDificultad : byte
        {
            Facil,
            Media,
            Maxima,
            Personalizada
        }

        public TipoDificultad Dificultad { get; private set; }

        public Busqueda.TipoBusqueda LimiteBusqueda { get; private set; }
        public int Limite { get; private set; }

        public bool UsarLibroAperturas { get; private set; }
        public TextAsset LibroAperturas { get; private set; }
        public int MaxMovimientoLibro { get; private set; }

        // ✅ Constructores privados para evitar usos incorrectos
        private ConfiguracionIA() { }

        // 🔹 Preconfigurados
        public static ConfiguracionIA CrearFacil(TextAsset libro)
        {
            return new ConfiguracionIA
            {
                Dificultad = TipoDificultad.Facil,
                LimiteBusqueda = Busqueda.TipoBusqueda.PorProfundidad,
                Limite = 2,
                UsarLibroAperturas = true,
                LibroAperturas = libro,
                MaxMovimientoLibro = 2
            };
        }

        public static ConfiguracionIA CrearMedia(TextAsset libro)
        {
            return new ConfiguracionIA
            {
                Dificultad = TipoDificultad.Media,
                LimiteBusqueda = Busqueda.TipoBusqueda.PorProfundidad,
                Limite = 4,
                UsarLibroAperturas = true,
                LibroAperturas = libro,
                MaxMovimientoLibro = 4
            };
        }

        public static ConfiguracionIA CrearMaxima(TextAsset libro)
        {
            return new ConfiguracionIA
            {
                Dificultad = TipoDificultad.Maxima,
                LimiteBusqueda = Busqueda.TipoBusqueda.PorTiempo,
                Limite = TIEMPO_DINAMICO,
                UsarLibroAperturas = true,
                LibroAperturas = libro,
                MaxMovimientoLibro = 8
            };
        }

        // 🔹 Personalizado
        public static ConfiguracionIA CrearPersonalizada(
            Busqueda.TipoBusqueda limiteBusqueda,
            int limite,
            bool usarLibroAperturas,
            int maxMovimientoLibro,
            TextAsset libro)
        {
            if (usarLibroAperturas && libro == null)
                throw new ArgumentException("Se requiere un libro si 'usarLibroAperturas' es verdadero.");

            return new ConfiguracionIA
            {
                Dificultad = TipoDificultad.Personalizada,
                LimiteBusqueda = limiteBusqueda,
                Limite = limite,
                UsarLibroAperturas = usarLibroAperturas,
                LibroAperturas = libro,
                MaxMovimientoLibro = maxMovimientoLibro
            };
        }
    }
}
