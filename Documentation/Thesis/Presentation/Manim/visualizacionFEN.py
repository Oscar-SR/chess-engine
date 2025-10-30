from manim import *

class FEN(Scene):
    def construct(self):
        self.camera.background_color = WHITE
        fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR"

        # Crear el tablero con cuadrados en lugar de imagen
        board = self.create_chess_board(scale=0.75)
        board.z_index = -1  # Para que quede detrás de las piezas y texto
        self.add(board)

        # Calcular posiciones para piezas (alineadas con el tablero)
        self.calculate_square_positions(scale=0.75)

        # Mostrar la cadena FEN debajo del tablero, en negro
        fen_text = Text(fen, font_size=28, color=BLACK)
        fen_text.to_edge(DOWN)
        self.play(Write(fen_text))
        self.wait(1)

        # Animar la aparición de piezas e ir resaltando la cadena FEN
        self.animate_fen_step_by_step(fen, fen_text)

    def create_chess_board(self, scale=1.0):
        squares = VGroup()
        light_color = "#F3D9B5"
        dark_color = "#BA8A62"
        colors = [light_color, dark_color]
        self.square_positions = {}

        for row in range(8):
            for col in range(8):
                square = Square(side_length=0.9 * scale)
                square.set_fill(colors[(row + col) % 2], opacity=1)
                square.set_stroke(BLACK, width=1)
                x = col * 0.9 * scale - 3.15 * scale
                y = row * -0.9 * scale + 2.8 * scale
                square.move_to([x, y, 0])
                squares.add(square)
                self.square_positions[(row, col)] = [x, y, 0]

        return squares

    def calculate_square_positions(self, scale=1.0):
        # Este método no es estrictamente necesario ya que la posición se guarda en create_chess_board,
        # pero lo mantenemos para compatibilidad o ajustes posteriores.
        pass

    def animate_fen_step_by_step(self, fen_string, fen_text_obj):
        piece_names = {
            "r": "Torre negra", "n": "Caballo negro", "b": "Alfil negro",
            "q": "Reina negra", "k": "Rey negro", "p": "Peón negro",
            "R": "Torre blanca", "N": "Caballo blanco", "B": "Alfil blanco",
            "Q": "Reina blanca", "K": "Rey blanco", "P": "Peón blanco",
        }

        row = 0
        col = 0
        char_index = 0

        for char in fen_string:
            if char == "/":
                row += 1
                col = 0
                char_index += 1
                continue

            original_char = fen_text_obj[char_index]
            highlight = original_char.copy().set_color(YELLOW)
            highlight.move_to(original_char.get_center())
            self.play(Transform(original_char, highlight), run_time=0.2)

            if char.isdigit():
                col += int(char)
            elif char in piece_names:
                image_name = piece_names[char]
                image_path = f"assets/{image_name}.png"
                pos = self.square_positions[(row, col)]
                piece = ImageMobject(image_path).scale(0.1).move_to(pos)
                piece.z_index = 1
                self.play(FadeIn(piece), run_time=0.3)
                col += 1

            self.wait(0.2)

            # Restaurar a negro
            reset_char = highlight.copy().set_color(BLACK)
            self.play(Transform(original_char, reset_char), run_time=0.1)

            char_index += 1
