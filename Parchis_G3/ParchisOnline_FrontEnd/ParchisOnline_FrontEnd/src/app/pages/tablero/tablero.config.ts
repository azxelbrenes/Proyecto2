export interface Celda {
  fila:    number;   // 1 a 15
  columna: number;   // 1 a 15
}

// Offset de entrada al anillo según el color
export const OFFSET_COLOR: { [key: string]: number } = {
  'ROJO':     0,
  'AZUL':     16,
  'VERDE':    32,
  'AMARILLO': 48
};


// EL ANILLO — 64 casillas en sentido horario

// Empezamos en la casilla de salida del ROJO (columna 7, fila 13)
// y damos la vuelta completa en sentido horario.
//
// El grid es 15×15. Los brazos de la cruz ocupan las filas/columnas
// 7, 8 y 9. Las casas van en las esquinas de 6×6.
export const ANILLO: Celda[] = [
  // ── Brazo inferior, subiendo por la izquierda (0-6) ──────────
  { fila: 13, columna: 7 },  // 0  ← salida ROJO
  { fila: 12, columna: 7 },  // 1
  { fila: 11, columna: 7 },  // 2
  { fila: 10, columna: 7 },  // 3
  { fila: 9,  columna: 7 },  // 4
  { fila: 9,  columna: 6 },  // 5  ← gira a la izquierda
  { fila: 9,  columna: 5 },  // 6

  // ── Brazo izquierdo (7-12) ───────────────────────────────────
  { fila: 9,  columna: 4 },  // 7
  { fila: 9,  columna: 3 },  // 8
  { fila: 9,  columna: 2 },  // 9
  { fila: 9,  columna: 1 },  // 10
  { fila: 8,  columna: 1 },  // 11 ← sube
  { fila: 7,  columna: 1 },  // 12

  // ── Brazo izquierdo, subiendo (13-19) ────────────────────────
  { fila: 7,  columna: 2 },  // 13
  { fila: 7,  columna: 3 },  // 14
  { fila: 7,  columna: 4 },  // 15
  { fila: 7,  columna: 5 },  // 16 ← salida AZUL
  { fila: 7,  columna: 6 },  // 17
  { fila: 6,  columna: 7 },  // 18
  { fila: 5,  columna: 7 },  // 19

  // ── Brazo superior (20-25) ───────────────────────────────────
  { fila: 4,  columna: 7 },  // 20
  { fila: 3,  columna: 7 },  // 21
  { fila: 2,  columna: 7 },  // 22
  { fila: 1,  columna: 7 },  // 23
  { fila: 1,  columna: 8 },  // 24 ← cruza arriba
  { fila: 1,  columna: 9 },  // 25

  // ── Brazo superior, bajando por la derecha (26-32) ───────────
  { fila: 2,  columna: 9 },  // 26
  { fila: 3,  columna: 9 },  // 27
  { fila: 4,  columna: 9 },  // 28
  { fila: 5,  columna: 9 },  // 29
  { fila: 6,  columna: 9 },  // 30
  { fila: 7,  columna: 10 }, // 31
  { fila: 7,  columna: 11 }, // 32 ← salida VERDE

  // ── Brazo derecho (33-38) ────────────────────────────────────
  { fila: 7,  columna: 12 }, // 33
  { fila: 7,  columna: 13 }, // 34
  { fila: 7,  columna: 14 }, // 35
  { fila: 7,  columna: 15 }, // 36
  { fila: 8,  columna: 15 }, // 37 ← baja
  { fila: 9,  columna: 15 }, // 38

  // ── Brazo derecho, bajando (39-45) ───────────────────────────
  { fila: 9,  columna: 14 }, // 39
  { fila: 9,  columna: 13 }, // 40
  { fila: 9,  columna: 12 }, // 41
  { fila: 9,  columna: 11 }, // 42
  { fila: 9,  columna: 10 }, // 43
  { fila: 10, columna: 9 },  // 44
  { fila: 11, columna: 9 },  // 45

  // ── Brazo inferior (46-51) ───────────────────────────────────
  { fila: 12, columna: 9 },  // 46
  { fila: 13, columna: 9 },  // 47
  { fila: 14, columna: 9 },  // 48 ← salida AMARILLO
  { fila: 15, columna: 9 },  // 49
  { fila: 15, columna: 8 },  // 50 ← cruza abajo
  { fila: 15, columna: 7 },  // 51

  // ── Cierre del anillo (52-63) ────────────────────────────────
  { fila: 14, columna: 7 },  // 52
  { fila: 14, columna: 6 },  // 53
  { fila: 13, columna: 6 },  // 54
  { fila: 12, columna: 6 },  // 55
  { fila: 11, columna: 6 },  // 56
  { fila: 10, columna: 6 },  // 57
  { fila: 10, columna: 5 },  // 58
  { fila: 11, columna: 5 },  // 59
  { fila: 12, columna: 5 },  // 60
  { fila: 13, columna: 5 },  // 61
  { fila: 14, columna: 5 },  // 62
  { fila: 15, columna: 5 }   // 63
];

// ================================================================
// RECTAS FINALES — 4 casillas privadas por color
// ================================================================
// Después de dar la vuelta completa, cada jugador entra a su
// pasillo privado que lleva al centro. Ningún rival puede pisarlo.
export const RECTA_FINAL: { [key: string]: Celda[] } = {
  'ROJO': [
    { fila: 12, columna: 8 },
    { fila: 11, columna: 8 },
    { fila: 10, columna: 8 },
    { fila: 9,  columna: 8 }
  ],
  'AZUL': [
    { fila: 8,  columna: 2 },
    { fila: 8,  columna: 3 },
    { fila: 8,  columna: 4 },
    { fila: 8,  columna: 5 }
  ],
  'VERDE': [
    { fila: 4,  columna: 8 },
    { fila: 5,  columna: 8 },
    { fila: 6,  columna: 8 },
    { fila: 7,  columna: 8 }
  ],
  'AMARILLO': [
    { fila: 8,  columna: 14 },
    { fila: 8,  columna: 13 },
    { fila: 8,  columna: 12 },
    { fila: 8,  columna: 11 }
  ]
};

// ================================================================
// CASAS — donde esperan las fichas antes de salir
// ================================================================
// Cada casa tiene 4 posiciones, una por ficha.
export const CASAS: { [key: string]: Celda[] } = {
  'ROJO': [
    { fila: 11, columna: 2 }, { fila: 11, columna: 4 },
    { fila: 13, columna: 2 }, { fila: 13, columna: 4 }
  ],
  'AZUL': [
    { fila: 3,  columna: 2 }, { fila: 3,  columna: 4 },
    { fila: 5,  columna: 2 }, { fila: 5,  columna: 4 }
  ],
  'VERDE': [
    { fila: 3,  columna: 12 }, { fila: 3,  columna: 14 },
    { fila: 5,  columna: 12 }, { fila: 5,  columna: 14 }
  ],
  'AMARILLO': [
    { fila: 11, columna: 12 }, { fila: 11, columna: 14 },
    { fila: 13, columna: 12 }, { fila: 13, columna: 14 }
  ]
};

// Casillas seguras: las 4 de salida. Ahí no hay capturas.
export const CASILLAS_SEGURAS = [0, 16, 32, 48];

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

  // ── Coronada (posición 69) → centro del tablero ──────────────
  if (posicion >= 69) {
    return { fila: 8, columna: 8 };
  }

  // ── Recta final (65-68) ──────────────────────────────────────
  if (posicion >= 65) {
    const recta  = RECTA_FINAL[color] ?? RECTA_FINAL['ROJO'];
    const indice = posicion - 65;
    return recta[Math.min(indice, 3)];
  }

  // ── Anillo (1-64) ────────────────────────────────────────────
  // La posición 1 es la casilla de salida del jugador.
  // Sumamos su offset para saber en qué casilla física está.
  const offset        = OFFSET_COLOR[color] ?? 0;
  const casillaAnillo = (offset + (posicion - 1)) % 64;

  return ANILLO[casillaAnillo];
}

// ¿Esta casilla del anillo es segura?
export function esCasillaSegura(indiceAnillo: number): boolean {
  return CASILLAS_SEGURAS.includes(indiceAnillo);
}
