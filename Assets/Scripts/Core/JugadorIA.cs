using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ajedrez.Managers;
using Ajedrez.IA;
using Ajedrez.Debugging;
using System.Diagnostics;

namespace Ajedrez.Core
{
    public class JugadorIA : Jugador
    {
        private ConfiguracionIA configuracionIA;
        private Tablero tablero;
        private Busqueda busqueda;
        private LibroAperturas libroAperturas;
        private GestorTiempo gestorTiempo;

        public JugadorIA(string nombre, Pieza.Color colorPiezas, Tablero tablero, Reloj reloj, ConfiguracionIA configuracionIA)
            : base(nombre, colorPiezas)
        {
            this.tablero = tablero;
            this.configuracionIA = configuracionIA;

            if (reloj != null)
                gestorTiempo = new GestorTiempo(reloj, colorPiezas);

            ConfigurarIA();
        }

        public JugadorIA(Pieza.Color colorPiezas, Tablero tablero, Reloj reloj, ConfiguracionIA configuracionIA)
            : base("IA " + configuracionIA.Dificultad, colorPiezas)
        {
            this.tablero = tablero;
            this.configuracionIA = configuracionIA;

            if (reloj != null)
                gestorTiempo = new GestorTiempo(reloj, colorPiezas);

            ConfigurarIA();
        }

        private void ConfigurarIA()
        {
            if (configuracionIA.UsarLibroAperturas)
                libroAperturas = new LibroAperturas(configuracionIA.LibroAperturas.text);

            busqueda = new Busqueda(tablero, configuracionIA.LimiteBusqueda, configuracionIA.Limite);
        }

        public ConfiguracionIA ConfiguracionIA
        {
            get
            {
                return configuracionIA;
            }
            set
            {
                configuracionIA = value;
                base.Nombre = "IA " + configuracionIA.Dificultad;
                ConfigurarIA();
            }
        }

        public override Pieza.Color ColorPiezas
        {
            get => base.ColorPiezas;
            set
            {
                if (gestorTiempo != null)
                    gestorTiempo.ColorPiezas = value;

                base.ColorPiezas = value;
            }
        }

        public Busqueda Busqueda
        {
            get
            {
                return busqueda;
            }
        }

        public GestorTiempo GestorTiempo
        {
            set
            {
                gestorTiempo = value;
            }
        }

        public Tablero Tablero
        {
            set
            {
                tablero = value;
                busqueda = new Busqueda(tablero, configuracionIA.LimiteBusqueda, configuracionIA.Limite);
            }
        }

        public async Task<Movimiento> HallarMejorMovimiento(List<Movimiento> movimientosLegales = null)
        {
            Movimiento mejorMovimiento;

            if (configuracionIA.UsarLibroAperturas && tablero.NumMovimientosTotales <= configuracionIA.MaxMovimientoLibro && libroAperturas.TryGetValue(tablero.ToFEN(incluirPeonAlPaso: false), out string movimientoLAN))
            {
                mejorMovimiento = new Movimiento(movimientoLAN, tablero);
            }
            else
            {
                CancellationTokenSource cancelTokenSource = new CancellationTokenSource();

                if (configuracionIA.LimiteBusqueda == Busqueda.TipoBusqueda.PorTiempo)
                {
                    // Lanzar una tarea que cancela la búsqueda después del tiempo requerido de búsqueda
                    int tiempoBusqueda = configuracionIA.Limite == ConfiguracionIA.TIEMPO_DINAMICO ? gestorTiempo.CalcularTiempoBusqueda(tablero.NumMovimientosTotales) : configuracionIA.Limite;
                    Task delayTask = Task.Delay(tiempoBusqueda, cancelTokenSource.Token)
                    .ContinueWith(_ =>
                    {
                        if (!cancelTokenSource.IsCancellationRequested)
                            busqueda.TerminarBusqueda();
                    });
                }

                // Ejecutar la búsqueda como una tarea en segundo plano y esperar a que termine
                mejorMovimiento = await Task.Run(() =>
                {
                    return busqueda.EmpezarBusqueda(movimientosLegales);
                });

                // Cancelar temporizador si la búsqueda termina antes
                cancelTokenSource.Cancel();

                // Escribir el resultado del diagnóstico en el debugger
                BusquedaDebugger.Instancia?.DebugBusqueda(busqueda.Diagnostico.ToString());
            }

            return mejorMovimiento;
        }
    }
}