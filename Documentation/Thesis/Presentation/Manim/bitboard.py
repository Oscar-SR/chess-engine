from manim import *

class Bitboard(Scene):
    def construct(self):
        self.camera.background_color = WHITE

        bitboard_value = int(
            "0000000011111111000000000000000000000000000000000000000000000000", 2
        )
        bit_string = format(bitboard_value, "064b")

        # Mostrar número decimal primero
        decimal_text = Text(str(bitboard_value), font_size=40, color=BLACK)
        decimal_text.to_edge(UP).shift(DOWN * 0.7)
        self.add(decimal_text)
        self.wait(1.5)

        # Transformar decimal a binario (mostrar todos los bits en fila)
        bin_chars = [
            Text(char, font_size=24, color=BLUE if char == "1" else RED)
            for char in bit_string
        ]
        bin_text = VGroup(*bin_chars)
        bin_text.arrange(RIGHT, buff=0.05)
        bin_text.to_edge(UP).shift(DOWN)
        bin_text.z_index = 1

        self.play(ReplacementTransform(decimal_text, bin_text))
        self.wait(1)

        # DIVIDIR

        # Crear el tablero original
        board = self.create_chess_board(scale=0.5)
        board.z_index = -1
        self.add(board)
        self.wait(0.5)

        # Mover los bits a sus posiciones en el tablero
        target_positions = []
        for i in range(64):
            row = i // 8
            col = i % 8
            pos = self.square_positions[(7 - row, col)]
            target_positions.append((i, pos))

        self.play(*[bin_text[i].animate.move_to(pos) for i, pos in target_positions], run_time=2)
        self.wait(0.5)

        # Crear piezas
        pieces = Group()
        for index, pos in target_positions:
            bit_char = bin_text[index]
            if bit_char.text == "1":
                piece = ImageMobject("assets/Peón blanco.png").scale(0.06).move_to(pos)
                piece.z_index = 2
                pieces.add(piece)

        self.play(FadeIn(pieces))
        self.wait(1)

        # DIVIDIR

        self.play(FadeOut(bin_text))
        self.wait(2)

        self.play(board.animate.shift(LEFT * 4.5), pieces.animate.shift(LEFT * 4.5), run_time=2)

        # Tablero central
        new_board = self.create_chess_board(scale=0.5)
        new_board.z_index = -1
        self.add(new_board)

        center_pos = self.square_positions[(3, 3)]

        black_rook = ImageMobject("assets/Torre negra.png").scale(0.06).move_to(center_pos)
        black_rook.z_index = 2
        self.play(FadeIn(black_rook))
        self.wait(2)

        # Bits sobre tablero izquierdo
        bit_text_left = VGroup(*[
            Text(char, font_size=24, color=BLUE if char == "1" else RED)
            for char in bit_string
        ])
        bit_text_left.arrange(RIGHT, buff=0.05)

        for i, pos in enumerate(target_positions):
            bit_text_left[i].move_to(pos[1])
            bit_text_left[i].shift(LEFT * 4.5)
        bit_text_left.z_index = 3
        self.play(FadeIn(bit_text_left))

        # Mostrar máscara de movimientos válidos para torre
        moves_mask = []
        for row in range(8):
            for col in range(8):
                if row == 4 or col == 3:
                    moves_mask.append('1')
                else:
                    moves_mask.append('0')

        moves_text = VGroup(*[
            Text(char, font_size=24, color=BLUE if char == "1" else RED)
            for char in moves_mask
        ])
        moves_text.arrange(RIGHT, buff=0.05)

        for i in range(64):
            row = i // 8
            col = i % 8
            pos = self.square_positions[(7 - row, col)]
            moves_text[i].move_to(pos)
        moves_text.z_index = 3
        self.play(FadeIn(moves_text))
        self.wait(2)

        # Signos AND y =
        and_sign = Text("∧", font_size=50, color=BLACK)
        and_sign.move_to(LEFT * 2.25)
        and_sign.z_index = 4
        or_sign = Text("=", font_size=50, color=BLACK)
        or_sign.move_to(RIGHT * 2.25)
        or_sign.z_index = 4

        # Tercer tablero (derecha)
        third_board = self.create_chess_board(scale=0.5)
        third_board.shift(RIGHT * 4.5)
        third_board.z_index = -1
        self.play(FadeIn(and_sign), FadeIn(or_sign), FadeIn(third_board))
        self.wait(1)

        # Peones en el tercer tablero
        pieces_third = Group()
        tercer_peon = None  # Aquí guardarás el tercer peón
        for index, pos in enumerate(target_positions):
            bit_char = bit_string[index]
            if bit_char == '1':
                piece_pos = [pos[1][0] + 4.5, pos[1][1], 0]
                piece = ImageMobject("assets/Peón blanco.png").scale(0.06).move_to(piece_pos)
                piece.z_index = 2
                pieces_third.add(piece)

                # Guardar el tercer peón con base en el orden de aparición
                if pieces_third.__len__() == 4:
                    tercer_peon = piece

        # Torre en el tablero derecho
        center_pos_third = [center_pos[0] + 4.5, center_pos[1], 0]
        black_rook_third = ImageMobject("assets/Torre negra.png").scale(0.06).move_to(center_pos_third)
        black_rook_third.z_index = 3

        self.add(pieces_third, black_rook_third)
        self.wait(2)

        # Mostrar bits resultado: solo tercer "1" azul, resto ceros rojos
        third_bits = []
        count_ones = 0
        for i, bit in enumerate(bit_string):
            if bit == '1':
                count_ones += 1
            if count_ones == 4:
                third_one_index = i
                break

        third_bits_text = VGroup()
        for i in range(64):
            if i == third_one_index:
                char = '1'
                color = BLUE
            else:
                char = '0'
                color = RED

            row = i // 8
            col = i % 8
            pos = self.square_positions[(7 - row, col)]
            shifted_pos = [pos[0] + 4.5, pos[1], 0]
            bit_mobj = Text(char, font_size=24, color=color).move_to(shifted_pos)
            third_bits_text.add(bit_mobj)

        third_bits_text.z_index = 4
        self.play(FadeIn(third_bits_text))
        self.wait(2)

        self.play(
            *[FadeOut(p) for p in pieces_third if p != tercer_peon],
            FadeOut(black_rook_third)
        )

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
