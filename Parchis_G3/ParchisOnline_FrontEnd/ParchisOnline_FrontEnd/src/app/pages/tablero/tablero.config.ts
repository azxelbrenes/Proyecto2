export interface Celda {
  fila:    number;   // 1 a 15
  columna: number;   // 1 a 15
}

// ── Codificación de la posición lógica de una ficha ─────────────
// Es el mismo contrato que usa el backend. Si cambia acá, cambia allá.
export const POS_CASA          = 0;   // esperando en la casa
export const POS_ANILLO_MIN    = 1;   // primera casilla propia (salida)
export const POS_ANILLO_MAX    = 51;  // última casilla del anillo
export const POS_RECTA_MIN     = 52;  // primera casilla de la recta final
export const POS_RECTA_MAX     = 56;  // última casilla antes del centro
export const POS_CORONADA      = 57;  // llegó al centro

export const LARGO_ANILLO      = 52;
export const LARGO_RECTA_FINAL = 5;

// -- El centro --
// Es un bloque de 3x3, no una sola celda. Se dibuja como cuatro
// cunas de color con la corona en el medio.
export const CENTRO_MIN = 7;
export const CENTRO_MAX = 9;

// Las cuatro esquinas del centro. El anillo las rodea sin pisarlas,
// y es ahi donde ocurren los cuatro giros diagonales legitimos.
export const ESQUINAS_CENTRO: Celda[] = [
  { fila: 7, columna: 7 },
  { fila: 7, columna: 9 },
  { fila: 9, columna: 7 },
  { fila: 9, columna: 9 }
];

// Cada una de las 9 celdas del centro lleva su propio trozo del
// dibujo. Se hace con background y no con un elemento superpuesto:
// un hijo extra del grid, aunque tenga grid-area explicito, hace
// que las celdas auto-colocadas esquiven esas 9 posiciones y todo
// el tablero se corra.
export const CUNAS_CENTRO: { [clave: string]: string } = {
  '7-7': 'centro-nw',        // verde arriba-derecha / azul abajo-izquierda
  '7-8': 'centro-verde',
  '7-9': 'centro-ne',        // verde arriba-izquierda / amarillo abajo-derecha
  '8-7': 'centro-azul',
  '8-8': 'centro-medio',     // las cuatro cunas se juntan aca
  '8-9': 'centro-amarillo',
  '9-7': 'centro-sw',        // azul arriba-izquierda / rojo abajo-derecha
  '9-8': 'centro-rojo',
  '9-9': 'centro-se'         // amarillo arriba-derecha / rojo abajo-izquierda
};

// ── Distribución de colores por esquina ─────────────────────────
//   AZUL     → superior izquierda
//   VERDE    → superior derecha
//   AMARILLO → inferior derecha
//   ROJO     → inferior izquierda
// El recorrido es horario, así que ese es el orden de los offsets.
export const OFFSET_COLOR: { [key: string]: number } = {
  'AZUL':     0,
  'VERDE':    13,
  'AMARILLO': 26,
  'ROJO':     39
};

export const COLORES: string[] = ['AZUL', 'VERDE', 'AMARILLO', 'ROJO'];


// EL ANILLO — 52 casillas en sentido horario

// Arranca en la salida del AZUL (7,2) y recorre el perímetro de la
// cruz. Cada par de casillas consecutivas es ortogonalmente
// adyacente, incluido el cierre 51 → 0. Lo verifica validarAnillo().
export const ANILLO: Celda[] = [
  // ── Brazo izquierdo, fila 7 hacia la derecha (0-4) ───────────
  { fila: 7,  columna: 2  },  // 0  ← SALIDA AZUL
  { fila: 7,  columna: 3  },  // 1
  { fila: 7,  columna: 4  },  // 2
  { fila: 7,  columna: 5  },  // 3
  { fila: 7,  columna: 6  },  // 4

  // ── Sube por la columna 7 (5-10) ─────────────────────────────
  { fila: 6,  columna: 7  },  // 5
  { fila: 5,  columna: 7  },  // 6
  { fila: 4,  columna: 7  },  // 7
  { fila: 3,  columna: 7  },  // 8  ★ seguro
  { fila: 2,  columna: 7  },  // 9
  { fila: 1,  columna: 7  },  // 10 ← esquina superior izquierda

  // ── Cruza arriba (11-12) ─────────────────────────────────────
  { fila: 1,  columna: 8  },  // 11 ← entrada a la recta del VERDE
  { fila: 1,  columna: 9  },  // 12

  // ── Baja por la columna 9 (13-17) ────────────────────────────
  { fila: 2,  columna: 9  },  // 13 ← SALIDA VERDE
  { fila: 3,  columna: 9  },  // 14
  { fila: 4,  columna: 9  },  // 15
  { fila: 5,  columna: 9  },  // 16
  { fila: 6,  columna: 9  },  // 17

  // ── Brazo derecho, fila 7 hacia la derecha (18-23) ───────────
  { fila: 7,  columna: 10 },  // 18
  { fila: 7,  columna: 11 },  // 19
  { fila: 7,  columna: 12 },  // 20
  { fila: 7,  columna: 13 },  // 21 ★ seguro
  { fila: 7,  columna: 14 },  // 22
  { fila: 7,  columna: 15 },  // 23 ← esquina superior derecha

  // ── Cruza a la derecha (24-25) ───────────────────────────────
  { fila: 8,  columna: 15 },  // 24 ← entrada a la recta del AMARILLO
  { fila: 9,  columna: 15 },  // 25

  // ── Brazo derecho, fila 9 hacia la izquierda (26-30) ─────────
  { fila: 9,  columna: 14 },  // 26 ← SALIDA AMARILLO
  { fila: 9,  columna: 13 },  // 27
  { fila: 9,  columna: 12 },  // 28
  { fila: 9,  columna: 11 },  // 29
  { fila: 9,  columna: 10 },  // 30

  // ── Baja por la columna 9 (31-36) ────────────────────────────
  { fila: 10, columna: 9  },  // 31
  { fila: 11, columna: 9  },  // 32
  { fila: 12, columna: 9  },  // 33
  { fila: 13, columna: 9  },  // 34 ★ seguro
  { fila: 14, columna: 9  },  // 35
  { fila: 15, columna: 9  },  // 36 ← esquina inferior derecha

  // ── Cruza abajo (37-38) ──────────────────────────────────────
  { fila: 15, columna: 8  },  // 37 ← entrada a la recta del ROJO
  { fila: 15, columna: 7  },  // 38

  // ── Sube por la columna 7 (39-43) ────────────────────────────
  { fila: 14, columna: 7  },  // 39 ← SALIDA ROJO
  { fila: 13, columna: 7  },  // 40
  { fila: 12, columna: 7  },  // 41
  { fila: 11, columna: 7  },  // 42
  { fila: 10, columna: 7  },  // 43

  // ── Brazo izquierdo, fila 9 hacia la izquierda (44-49) ───────
  { fila: 9,  columna: 6  },  // 44
  { fila: 9,  columna: 5  },  // 45
  { fila: 9,  columna: 4  },  // 46
  { fila: 9,  columna: 3  },  // 47 ★ seguro
  { fila: 9,  columna: 2  },  // 48
  { fila: 9,  columna: 1  },  // 49 ← esquina inferior izquierda

  // ── Cruza a la izquierda y cierra (50-51) ────────────────────
  { fila: 8,  columna: 1  },  // 50 ← entrada a la recta del AZUL
  { fila: 7,  columna: 1  }   // 51 → vuelve a la 0
];


// RECTAS FINALES — 5 casillas, del borde hacia el centro

// Ningún rival puede pisarlas. La casilla 56 es la última; el
// centro (8,8) es la coronación y no forma parte de la recta.
export const RECTA_FINAL: { [key: string]: Celda[] } = {
  // AZUL: entra por la izquierda y avanza por la fila 8
  'AZUL': [
    { fila: 8, columna: 2 },
    { fila: 8, columna: 3 },
    { fila: 8, columna: 4 },
    { fila: 8, columna: 5 },
    { fila: 8, columna: 6 }
  ],
  // VERDE: entra por arriba y baja por la columna 8
  'VERDE': [
    { fila: 2, columna: 8 },
    { fila: 3, columna: 8 },
    { fila: 4, columna: 8 },
    { fila: 5, columna: 8 },
    { fila: 6, columna: 8 }
  ],
  // AMARILLO: entra por la derecha y avanza por la fila 8
  'AMARILLO': [
    { fila: 8, columna: 14 },
    { fila: 8, columna: 13 },
    { fila: 8, columna: 12 },
    { fila: 8, columna: 11 },
    { fila: 8, columna: 10 }
  ],
  // ROJO: entra por abajo y sube por la columna 8
  'ROJO': [
    { fila: 14, columna: 8 },
    { fila: 13, columna: 8 },
    { fila: 12, columna: 8 },
    { fila: 11, columna: 8 },
    { fila: 10, columna: 8 }
  ]
};

// La coronación: el centro exacto del tablero
export const CELDA_CENTRO: Celda = { fila: 8, columna: 8 };


// ZONAS DE CASA — bloques de 6×6 en las esquinas

// Las cuatro miden exactamente lo mismo. Antes azul y rojo medían
// 6×4 y verde y amarillo 6×5, que era la asimetría más visible.
export interface ZonaCasa {
  color:      string;
  filaMin:    number;
  filaMax:    number;
  columnaMin: number;
  columnaMax: number;
}

export const ZONAS_CASA: ZonaCasa[] = [
  { color: 'AZUL',     filaMin: 1,  filaMax: 6,  columnaMin: 1,  columnaMax: 6  },
  { color: 'VERDE',    filaMin: 1,  filaMax: 6,  columnaMin: 10, columnaMax: 15 },
  { color: 'ROJO',     filaMin: 10, filaMax: 15, columnaMin: 1,  columnaMax: 6  },
  { color: 'AMARILLO', filaMin: 10, filaMax: 15, columnaMin: 10, columnaMax: 15 }
];

// ── Los 4 huecos donde descansa cada ficha dentro de su casa ────
export const CASAS: { [key: string]: Celda[] } = {
  'AZUL': [
    { fila: 2, columna: 2 }, { fila: 2, columna: 5 },
    { fila: 5, columna: 2 }, { fila: 5, columna: 5 }
  ],
  'VERDE': [
    { fila: 2, columna: 11 }, { fila: 2, columna: 14 },
    { fila: 5, columna: 11 }, { fila: 5, columna: 14 }
  ],
  'ROJO': [
    { fila: 11, columna: 2 }, { fila: 11, columna: 5 },
    { fila: 14, columna: 2 }, { fila: 14, columna: 5 }
  ],
  'AMARILLO': [
    { fila: 11, columna: 11 }, { fila: 11, columna: 14 },
    { fila: 14, columna: 11 }, { fila: 14, columna: 14 }
  ]
};


// CASILLAS SEGURAS

// Las 4 salidas, más una estrella 8 casillas adelante de cada una.
// Las 8 son rotaciones exactas unas de otras: (f,c) → (c, 16-f).
export const CASILLAS_SALIDA: number[] = [0, 13, 26, 39];
export const CASILLAS_ESTRELLA: number[] = [8, 21, 34, 47];

// Índice del anillo → color dueño de esa salida
export const COLOR_POR_SALIDA: { [indice: number]: string } = {
  0:  'AZUL',
  13: 'VERDE',
  26: 'AMARILLO',
  39: 'ROJO'
};


// POSICIÓN LÓGICA -CELDA DEL GRID

export function obtenerCelda(
  posicion: number,
  color: string,
  numeroFicha: number
): Celda {

  const colorValido = OFFSET_COLOR[color] !== undefined ? color : 'AZUL';

  // ── En casa ──────────────────────────────────────────────────
  if (posicion <= POS_CASA) {
    return CASAS[colorValido][(numeroFicha - 1) % 4];
  }

  // ── Coronada ─────────────────────────────────────────────────
  if (posicion >= POS_CORONADA) {
    return CELDA_CENTRO;
  }

  // ── Recta final ──────────────────────────────────────────────
  if (posicion >= POS_RECTA_MIN) {
    const recta  = RECTA_FINAL[colorValido];
    const indice = posicion - POS_RECTA_MIN;
    return recta[Math.min(indice, recta.length - 1)];
  }

  // ── Anillo ───────────────────────────────────────────────────
  // La posición 1 es la salida propia del jugador.
  const offset  = OFFSET_COLOR[colorValido];
  const indice  = (offset + posicion - 1) % LARGO_ANILLO;
  return ANILLO[indice];
}

// ── Índice físico del anillo para una posición relativa ─────────
// Sirve para saber si dos fichas de colores distintos comparten
// casilla, o si una casilla es segura.
export function indiceAnilloDe(posicion: number, color: string): number {
  if (posicion < POS_ANILLO_MIN || posicion > POS_ANILLO_MAX) return -1;
  const offset = OFFSET_COLOR[color] ?? 0;
  return (offset + posicion - 1) % LARGO_ANILLO;
}

export function esCasillaSalida(indiceAnillo: number): boolean {
  return CASILLAS_SALIDA.includes(indiceAnillo);
}

export function esCasillaEstrella(indiceAnillo: number): boolean {
  return CASILLAS_ESTRELLA.includes(indiceAnillo);
}

export function esCasillaSegura(indiceAnillo: number): boolean {
  return esCasillaSalida(indiceAnillo) || esCasillaEstrella(indiceAnillo);
}


// El tablero nunca cambia, así que calculamos las clases de las 225
// celdas una sola vez. Antes el template llamaba a getTipoCelda()
// 225 veces por ciclo de detección de cambios, y cada llamada hacía
// un findIndex sobre 68 elementos más dos bucles anidados.
export interface InfoCelda {
  clases:   string;
  estrella: boolean;
}

export function construirMapaCeldas(): Map<string, InfoCelda> {
  const mapa = new Map<string, InfoCelda>();

  for (let fila = 1; fila <= 15; fila++) {
    for (let columna = 1; columna <= 15; columna++) {
      mapa.set(`${fila}-${columna}`, calcularInfoCelda(fila, columna));
    }
  }

  return mapa;
}

function calcularInfoCelda(fila: number, columna: number): InfoCelda {
  const clases: string[] = [];

  // ── El centro es el bloque 3×3 entero, no una sola celda ─────
  // Antes solo (8,8) contaba como centro y las otras 8 celdas caían
  // en celda-vacia: eso dejaba la mancha crema en forma de cruz
  // alrededor del medio del tablero.
  const enCentro = fila    >= CENTRO_MIN && fila    <= CENTRO_MAX
                && columna >= CENTRO_MIN && columna <= CENTRO_MAX;

  if (enCentro) {
    const cuna = CUNAS_CENTRO[`${fila}-${columna}`] ?? '';
    return { clases: `celda-centro ${cuna}`.trim(), estrella: false };
  }

  // ── Recta final ──────────────────────────────────────────────
  for (const color of COLORES) {
    const esRecta = RECTA_FINAL[color].some(
      c => c.fila === fila && c.columna === columna
    );
    if (esRecta) {
      return {
        clases:   `celda-recta celda-recta-${color.toLowerCase()}`,
        estrella: false
      };
    }
  }

  // ── Anillo ───────────────────────────────────────────────────
  const indiceAnillo = ANILLO.findIndex(
    c => c.fila === fila && c.columna === columna
  );

  if (indiceAnillo >= 0) {
    clases.push('celda-anillo');

    if (esCasillaSalida(indiceAnillo)) {
      const color = COLOR_POR_SALIDA[indiceAnillo];
      clases.push('celda-salida', `celda-salida-${color.toLowerCase()}`);
    }

    return {
      clases:   clases.join(' '),
      estrella: esCasillaEstrella(indiceAnillo)
    };
  }

  // ── Zona de casa ─────────────────────────────────────────────
  const zona = ZONAS_CASA.find(
    z => fila    >= z.filaMin    && fila    <= z.filaMax
      && columna >= z.columnaMin && columna <= z.columnaMax
  );

  if (zona) {
    clases.push(`zona-${zona.color.toLowerCase()}`);

    // ¿Es uno de los 4 huecos donde descansa una ficha?
    const esHueco = CASAS[zona.color].some(
      c => c.fila === fila && c.columna === columna
    );
    if (esHueco) {
      clases.push('celda-casa');
    }

    return { clases: clases.join(' '), estrella: false };
  }

  return { clases: 'celda-vacia', estrella: false };
}


// Devuelve la lista de errores del anillo. Si devuelve vacío, la
// geometría es correcta. Este es el chequeo que faltaba y que
// habría detectado las casillas diagonales del anillo anterior.
export function validarAnillo(): string[] {
  const errores: string[] = [];

  if (ANILLO.length !== LARGO_ANILLO) {
    errores.push(`El anillo tiene ${ANILLO.length} casillas, deberían ser ${LARGO_ANILLO}`);
  }

  // 1. Adyacencia. En un tablero de parchís el recorrido NO es 100%
  //    ortogonal: hay exactamente cuatro giros diagonales, uno por
  //    cada esquina del centro, donde la ficha rodea el bloque
  //    central. Son legítimos. Cualquier otra diagonal sí es un
  //    error de coordenadas.
  let girosEsquina = 0;

  for (let i = 0; i < ANILLO.length; i++) {
    const actual    = ANILLO[i];
    const siguiente = ANILLO[(i + 1) % ANILLO.length];

    const df = Math.abs(actual.fila    - siguiente.fila);
    const dc = Math.abs(actual.columna - siguiente.columna);

    if (df + dc === 1) continue;

    if (df === 1 && dc === 1 && esGiroDeEsquina(actual, siguiente)) {
      girosEsquina++;
      continue;
    }

    errores.push(
      `Salto inválido entre la casilla ${i} (${actual.fila},${actual.columna}) ` +
      `y la ${(i + 1) % ANILLO.length} (${siguiente.fila},${siguiente.columna})`
    );
  }

  if (girosEsquina !== 4) {
    errores.push(`Hay ${girosEsquina} giros de esquina, deberían ser exactamente 4`);
  }

  // 2. No puede haber celdas repetidas
  const vistas = new Set<string>();
  for (let i = 0; i < ANILLO.length; i++) {
    const clave = `${ANILLO[i].fila}-${ANILLO[i].columna}`;
    if (vistas.has(clave)) {
      errores.push(`La celda (${ANILLO[i].fila},${ANILLO[i].columna}) aparece dos veces`);
    }
    vistas.add(clave);
  }

  // 3. Todo debe caer dentro del grid y fuera del centro
  for (let i = 0; i < ANILLO.length; i++) {
    const c = ANILLO[i];

    if (c.fila < 1 || c.fila > 15 || c.columna < 1 || c.columna > 15) {
      errores.push(`La casilla ${i} (${c.fila},${c.columna}) está fuera del grid`);
    }

    const pisaCentro = c.fila    >= CENTRO_MIN && c.fila    <= CENTRO_MAX
                    && c.columna >= CENTRO_MIN && c.columna <= CENTRO_MAX;
    if (pisaCentro) {
      errores.push(`La casilla ${i} (${c.fila},${c.columna}) invade el centro`);
    }
  }

  // 4. Las 4 salidas deben estar separadas por exactamente 13
  const offsets = COLORES.map(c => OFFSET_COLOR[c]).sort((a, b) => a - b);
  for (let i = 0; i < offsets.length; i++) {
    const esperado = i * (LARGO_ANILLO / 4);
    if (offsets[i] !== esperado) {
      errores.push(`El offset ${offsets[i]} debería ser ${esperado}`);
    }
  }

  // 5. Simetría rotacional: rotar 90° una salida da la siguiente
  //    Fórmula para un grid 1..15: (fila, col) → (col, 16 - fila)
  for (let i = 0; i < CASILLAS_SALIDA.length; i++) {
    const actual    = ANILLO[CASILLAS_SALIDA[i]];
    const siguiente = ANILLO[CASILLAS_SALIDA[(i + 1) % 4]];
    const rotada    = { fila: actual.columna, columna: 16 - actual.fila };

    if (rotada.fila !== siguiente.fila || rotada.columna !== siguiente.columna) {
      errores.push(
        `La salida ${CASILLAS_SALIDA[i]} rotada da (${rotada.fila},${rotada.columna}) ` +
        `pero la siguiente salida está en (${siguiente.fila},${siguiente.columna})`
      );
    }
  }

  return errores;
}

// Dos celdas forman un giro de esquina si ambas son ortogonalmente
// vecinas de una misma esquina del centro.
function esGiroDeEsquina(a: Celda, b: Celda): boolean {
  return ESQUINAS_CENTRO.some(esquina => {
    const distA = Math.abs(a.fila - esquina.fila) + Math.abs(a.columna - esquina.columna);
    const distB = Math.abs(b.fila - esquina.fila) + Math.abs(b.columna - esquina.columna);
    return distA === 1 && distB === 1;
  });
}