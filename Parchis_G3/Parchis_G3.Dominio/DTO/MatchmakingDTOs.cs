using System;
using System.Collections.Generic;
using System.Text;

namespace Parchis_G3.Dominio.DTO;

// Lo que devuelve el servidor cuando un jugador pide entrar a una sala
public class ResultadoMatchmakingDTO
{
    public int ParId { get; set; }  // La partida asignada
    public int JpId { get; set; }  // El ID del jugador dentro de esa partida
    public string ColorAsignado { get; set; } = string.Empty;
    public int PosicionEnPartida { get; set; }  // 1 a 4
    public int JugadoresActuales { get; set; }  // Cuántos hay ahora
    public int MonedasRestantes { get; set; }  // Saldo tras pagar la entrada
    public bool PartidaIniciada { get; set; }  // True si arrancó de una
    public int SegundosRestantes { get; set; }  // Cuenta regresiva para el inicio
    public List<JugadorEsperaDTO> Jugadores { get; set; } = new();
}

// Cada jugador que aparece en la sala de espera
public class JugadorEsperaDTO
{
    public int JpId { get; set; }
    public int? UsuId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int Posicion { get; set; }
    public bool EsBot { get; set; }
    public int Avatar { get; set; }
}

// Estado de la sala de espera (se manda por SignalR cada vez que
// alguien nuevo se une, para que todos vean la lista actualizada)
public class EstadoSalaEsperaDTO
{
    public int ParId { get; set; }
    public int SalId { get; set; }
    public string SalaNombre { get; set; } = string.Empty;
    public int JugadoresActuales { get; set; }
    public int SegundosRestantes { get; set; }
    public bool PartidaIniciada { get; set; }
    public List<JugadorEsperaDTO> Jugadores { get; set; } = new();
}
