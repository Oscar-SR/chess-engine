from manim import *

class Numero(Scene):
    def construct(self):
        self.camera.background_color = "#333333"

        # Texto inicial
        value = 0
        number_text = Text(f"{value} ms", font="Figtree Black", font_size=72, color=WHITE)
        self.add(number_text)

        # Función de actualización: reemplaza el contenido del texto
        def update_text(mob, alpha):
            new_value = int(alpha * 92)
            new_text = Text(f"{new_value} ms", font="Figtree Black", font_size=72, color=WHITE)
            new_text.move_to(mob.get_center())
            mob.become(new_text)

        self.play(UpdateFromAlphaFunc(number_text, update_text), run_time=1)
        self.wait(0.5)
