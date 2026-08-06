using Parchis_G3.Dominio.InterfacesAD;
using Parchis_G3.Utilitarios;

namespace Parchis_G3.Dominio.InterfacesLN;

public interface ISeguridadLN
{
    // ¿La cuenta está bloqueada por demasiados intentos fallidos?
    // Devuelve el mensaje de bloqueo, o null si puede intentar.
    string? VerificarBloqueoLogin(string correo, IUnidadTrabajoEF unidadTrabajo);

    // Registra un intento fallido. Si llega al límite, bloquea.
    void RegistrarIntentoFallido(string correo, string? ip, IUnidadTrabajoEF unidadTrabajo);

    // Login correcto: resetea el contador de intentos fallidos.
    void RegistrarLoginExitoso(int usuId, string correo, string? ip, IUnidadTrabajoEF unidadTrabajo);

    // Registra cualquier evento de seguridad en la tabla de auditoría.
    void RegistrarEvento(string evento, string? correo, int? usuId, string? ip, string? detalle, IUnidadTrabajoEF unidadTrabajo);
}
