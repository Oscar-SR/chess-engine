from manim import *

class Busqueda(Scene):
    def construct(self):
        self.camera.background_color = "#FDFDFD"

        # Título
        title = Text("Ejecución de Búsqueda Iterativa", color=BLACK, font_size=36)
        title.to_edge(UP)
        self.play(Write(title))

        # Variables de estado
        estado_texto = Text("tiempo_inicio = ahora\nmejor_movimiento = None", font_size=28, color=BLACK)
        estado_texto.next_to(title, DOWN, buff=1)
        self.play(Write(estado_texto))
        self.wait(0.5)

        # Simulación del bucle de profundización
        depth_steps = []
        for i in range(1, 5):  # Simulamos hasta profundidad 4
            step = Text(f"Profundidad = {i}", font_size=28, color=BLACK)
            if i == 1:
                step.next_to(estado_texto, DOWN, buff=1)
            else:
                step.next_to(depth_steps[-1], DOWN, buff=0.5)
            depth_steps.append(step)
            self.play(FadeIn(step))
            self.wait(0.4)

            call = Text(f"Llamar Buscar(posición, {i})", font_size=24, color=BLUE)
            call.next_to(step, DOWN, buff=0.3)
            self.play(FadeIn(call))
            self.wait(0.4)

            update = Text("mejor_movimiento ← nuevo", font_size=24, color=GREEN)
            update.next_to(call, DOWN, buff=0.3)
            self.play(FadeIn(update))
            self.wait(0.5)

        # Resultado final
        final = Text("Devolver mejor_movimiento", font_size=28, color=RED)
        final.next_to(depth_steps[-1], DOWN, buff=2)
        self.play(Write(final))
        self.wait(1)
