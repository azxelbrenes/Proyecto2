export interface Celda {
  fila:    number;   // 1 a 15
  columna: number;   // 1 a 15
}

// Punto de entrada al anillo de cada color.
// Están separados por 17 casillas (68 ÷ 4 = 17).
export const OFFSET_COLOR: { [key: string]: number } = {
  'ROJO':     0,
  'AZUL':     17,
  'VERDE':    34,
  'AMARILLO': 51
};

// ================================================================
// EL ANILLO — 68 casillas en sentido horario
// ================================================================
// Empieza en la salida del ROJO (fila 13, columna 7) y recorre
// todo el perímetro de la cruz en sentido horario.
export const ANILLO: Celda[] = [
  // ── Salida ROJO: sube por la columna 7 (0-6) ─────────────────
  { fila: 13, columna: 7 },  // 0  ← SALIDA ROJO
  { fila: 12, columna: 7 },  // 1
  { fila: 11, columna: 7 },  // 2
  { fila: 10, columna: 7 },  // 3
  { fila: 9,  columna: 7 },  // 4
  { fila: 9,  columna: 6 },  // 5  ← gira hacia la izquierda
  { fila: 9,  columna: 5 },  // 6

  // ── Brazo izquierdo: fila 9 hacia la izquierda (7-10) ────────
  { fila: 9,  columna: 4 },  // 7
  { fila: 9,  columna: 3 },  // 8
  { fila: 9,  columna: 2 },  // 9
  { fila: 9,  columna: 1 },  // 10 ← esquina izquierda

  // ── Sube por la columna 1 (11-12) ────────────────────────────
  { fila: 8,  columna: 1 },  // 11
  { fila: 7,  columna: 1 },  // 12

  // ── Brazo izquierdo: fila 7 hacia la derecha (13-16) ─────────
  { fila: 7,  columna: 2 },  // 13
  { fila: 7,  columna: 3 },  // 14
  { fila: 7,  columna: 4 },  // 15
  { fila: 7,  columna: 5 },  // 16

  // ── Salida AZUL: sube por la columna 7 (17-23) ───────────────
  { fila: 7,  columna: 6 },  // 17 ← SALIDA AZUL
  { fila: 6,  columna: 7 },  // 18
  { fila: 5,  columna: 7 },  // 19
  { fila: 4,  columna: 7 },  // 20
  { fila: 3,  columna: 7 },  // 21
  { fila: 2,  columna: 7 },  // 22
  { fila: 1,  columna: 7 },  // 23 ← esquina superior

  // ── Cruza arriba (24-25) ─────────────────────────────────────
  { fila: 1,  columna: 8 },  // 24
  { fila: 1,  columna: 9 },  // 25

  // ── Baja por la columna 9 (26-30) ────────────────────────────
  { fila: 2,  columna: 9 },  // 26
  { fila: 3,  columna: 9 },  // 27
  { fila: 4,  columna: 9 },  // 28
  { fila: 5,  columna: 9 },  // 29
  { fila: 6,  columna: 9 },  // 30

  // ── Brazo derecho: fila 7 hacia la derecha (31-33) ───────────
  { fila: 7,  columna: 10 }, // 31
  { fila: 7,  columna: 11 }, // 32
  { fila: 7,  columna: 12 }, // 33

  // ── Salida VERDE (34-37) ─────────────────────────────────────
  { fila: 7,  columna: 13 }, // 34 ← SALIDA VERDE
  { fila: 7,  columna: 14 }, // 35
  { fila: 7,  columna: 15 }, // 36 ← esquina derecha
  { fila: 8,  columna: 15 }, // 37

  // ── Baja y vuelve por la fila 9 (38-44) ──────────────────────
  { fila: 9,  columna: 15 }, // 38
  { fila: 9,  columna: 14 }, // 39
  { fila: 9,  columna: 13 }, // 40
  { fila: 9,  columna: 12 }, // 41
  { fila: 9,  columna: 11 }, // 42
  { fila: 9,  columna: 10 }, // 43
  { fila: 10, columna: 9 },  // 44

  // ── Baja por la columna 9 (45-49) ────────────────────────────
  { fila: 11, columna: 9 },  // 45
  { fila: 12, columna: 9 },  // 46
  { fila: 13, columna: 9 },  // 47
  { fila: 14, columna: 9 },  // 48
  { fila: 15, columna: 9 },  // 49 ← esquina inferior derecha

  // ── Cruza abajo (50) ─────────────────────────────────────────
  { fila: 15, columna: 8 },  // 50

  // ── Salida AMARILLO: sube por la columna 7 (51-54) ───────────
  { fila: 15, columna: 7 },  // 51 ← SALIDA AMARILLO
  { fila: 14, columna: 7 },  // 52
  { fila: 14, columna: 6 },  // 53
  { fila: 14, columna: 5 },  // 54

  // ── Brazo inferior: fila 15 hacia la izquierda (55-58) ───────
  { fila: 15, columna: 6 },  // 55
  { fila: 15, columna: 5 },  // 56
  { fila: 15, columna: 4 },  // 57
  { fila: 15, columna: 3 },  // 58

  // ── Cierre: vuelve hacia la salida del ROJO (59-67) ──────────
  { fila: 14, columna: 4 },  // 59
  { fila: 14, columna: 3 },  // 60
  { fila: 13, columna: 6 },  // 61
  { fila: 12, columna: 6 },  // 62
  { fila: 11, columna: 6 },  // 63
  { fila: 10, columna: 6 },  // 64
  { fila: 10, columna: 5 },  // 65
  { fila: 11, columna: 5 },  // 66
  { fila: 12, columna: 5 }   // 67
];

// ================================================================
// RECTAS FINALES — el pasillo de color del medio
// ================================================================
// Es la columna/fila 8 de cada brazo. Ningún rival puede pisarla.
// Van desde el borde exterior hacia el centro.
export const RECTA_FINAL: { [key: string]: Celda[] } = {
  // ROJO: sube desde abajo por la columna 8
  'ROJO': [
    { fila: 14, columna: 8 },
    { fila: 13, columna: 8 },
    { fila: 12, columna: 8 },
    { fila: 11, columna: 8 },
    { fila: 10, columna: 8 },
    { fila: 9,  columna: 8 }
  ],
  // AZUL: avanza desde la izquierda por la fila 8
  'AZUL': [
    { fila: 8,  columna: 2 },
    { fila: 8,  columna: 3 },
    { fila: 8,  columna: 4 },
    { fila: 8,  columna: 5 },
    { fila: 8,  columna: 6 },
    { fila: 8,  columna: 7 }
  ],
  // VERDE: baja desde arriba por la columna 8
  'VERDE': [
    { fila: 2,  columna: 8 },
    { fila: 3,  columna: 8 },
    { fila: 4,  columna: 8 },
    { fila: 5,  columna: 8 },
    { fila: 6,  columna: 8 },
    { fila: 7,  columna: 8 }
  ],
  // AMARILLO: avanza desde la derecha por la fila 8
  'AMARILLO': [
    { fila: 8,  columna: 14 },
    { fila: 8,  columna: 13 },
    { fila: 8,  columna: 12 },
    { fila: 8,  columna: 11 },
    { fila: 8,  columna: 10 },
    { fila: 8,  columna: 9  }
  ]
};

// ================================================================
// CASAS — donde esperan las fichas antes de salir
// ================================================================
// Cada casa ocupa una esquina de 6×6. Las 4 fichas se colocan
// en un cuadrado dentro de esa zona.
export const CASAS: { [key: string]: Celda[] } = {
  // ROJO: esquina inferior izquierda
  'ROJO': [
    { fila: 11, columna: 2 }, { fila: 11, columna: 4 },
    { fila: 13, columna: 2 }, { fila: 13, columna: 4 }
  ],
  // AZUL: esquina superior izquierda
  'AZUL': [
    { fila: 3,  columna: 2 }, { fila: 3,  columna: 4 },
    { fila: 5,  columna: 2 }, { fila: 5,  columna: 4 }
  ],
  // VERDE: esquina superior derecha
  'VERDE': [
    { fila: 3,  columna: 12 }, { fila: 3,  columna: 14 },
    { fila: 5,  columna: 12 }, { fila: 5,  columna: 14 }
  ],
  // AMARILLO: esquina inferior derecha
  'AMARILLO': [
    { fila: 11, columna: 12 }, { fila: 11, columna: 14 },
    { fila: 13, columna: 12 }, { fila: 13, columna: 14 }
  ]
};

// Las 4 casillas de salida son seguras: ahí no hay capturas
export const CASILLAS_SEGURAS = [0, 17, 34, 51];

// Casillas de seguro adicionales (las que llevan estrella en el
// tablero real, a mitad de cada tramo)
export const CASILLAS_SEGURO_EXTRA = [7, 24, 41, 58];

// ================================================================
// CALCULAR LA POSICIÓN VISUAL DE UNA FICHA
// ================================================================
// Traduce la posición lógica del backend (0-69) a una celda
// del grid de 15×15.
export function obtenerCelda(
  posicion: number,
  color: string,
  numeroFicha: number
): Celda {

  // ── En casa (posición 0) ─────────────────────────────────────
  if (posicion === 0) {
    const casa = CASAS[color] ?? CASAS['ROJO'];
    return casa[(numeroFicha - 1) % 4];
  }

  // ── Coronada (69) → el centro del tablero ────────────────────
  if (posicion >= 69) {
    return { fila: 8, columna: 8 };
  }

  // ── Recta final (65-68) ──────────────────────────────────────
  if (posicion >= 65) {
    const recta  = RECTA_FINAL[color] ?? RECTA_FINAL['ROJO'];
    const indice = posicion - 65;
    return recta[Math.min(indice, recta.length - 1)];
  }

  // ── Anillo (1-64) ────────────────────────────────────────────
  // La posición 1 es la casilla de salida del jugador.
  // Sumamos su offset para saber en qué casilla física está.
  const offset        = OFFSET_COLOR[color] ?? 0;
  const casillaAnillo = (offset + (posicion - 1)) % ANILLO.length;

  return ANILLO[casillaAnillo];
}

// ¿Esta casilla del anillo es una salida de color?
export function esCasillaSegura(indiceAnillo: number): boolean {
  return CASILLAS_SEGURAS.includes(indiceAnillo);
}

// ¿Esta casilla lleva estrella de seguro?
export function esSeguroExtra(indiceAnillo: number): boolean {
  return CASILLAS_SEGURO_EXTRA.includes(indiceAnillo);
}
