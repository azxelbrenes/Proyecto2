using System;
using System.Collections.Generic;
using System.Text;

namespace Parchis_G3.Utilitarios;
    public class Respuesta
    {
        public bool blnIndicadorTransaccion { get; set; }
        public string strMensajeRespuesta { get; set; } = string.Empty;
        public string strTituloRespuesta { get; set; } = string.Empty;
        public byte enuTipoMensaje { get; set; }
        public string? xCantidadItems { get; set; }
        public string? bfaltante { get; set; }

        public Respuesta()
        {
            blnIndicadorTransaccion = false;
            strTituloRespuesta = "Error";
            strMensajeRespuesta = "Ocurrió un error.";
            enuTipoMensaje = 4;
        }

        public static Respuesta Exito(string mensaje = "Operación exitosa")
        {
            return new Respuesta
            {
                blnIndicadorTransaccion = true,
                strTituloRespuesta = "Transacción Exitosa",
                strMensajeRespuesta = mensaje,
                enuTipoMensaje = 1
            };
        }

        public static Respuesta Informativo(string mensaje = "Información procesada correctamente")
        {
            return new Respuesta
            {
                blnIndicadorTransaccion = true,
                strTituloRespuesta = "Información",
                strMensajeRespuesta = mensaje,
                enuTipoMensaje = 2
            };
        }

        public static Respuesta Validacion(string mensaje = "Error de validación")
        {
            return new Respuesta
            {
                blnIndicadorTransaccion = false,
                strTituloRespuesta = "Validación",
                strMensajeRespuesta = mensaje,
                enuTipoMensaje = 3
            };
        }

        public static Respuesta Error(string mensaje = "Ocurrió un error")
        {
            return new Respuesta
            {
                blnIndicadorTransaccion = false,
                strTituloRespuesta = "Error",
                strMensajeRespuesta = mensaje,
                enuTipoMensaje = 4
            };
        }
    }

    public class Respuesta<T>
    {
        public bool blnIndicadorTransaccion { get; set; }
        public string strMensajeRespuesta { get; set; } = string.Empty;
        public string strTituloRespuesta { get; set; } = string.Empty;
        public byte enuTipoMensaje { get; set; }
        public T? ValorRetorno { get; set; }
        public string? xCantidadItems { get; set; }
        public string? bfaltante { get; set; }

        public Respuesta()
        {
            blnIndicadorTransaccion = false;
            strTituloRespuesta = "Error";
            strMensajeRespuesta = "Ocurrió un error.";
            enuTipoMensaje = 4;
        }

        public static Respuesta<T> Exito(T data, string mensaje = "Operación exitosa")
        {
            return new Respuesta<T>
            {
                blnIndicadorTransaccion = true,
                strTituloRespuesta = "Transacción Exitosa",
                strMensajeRespuesta = mensaje,
                enuTipoMensaje = 1,
                ValorRetorno = data
            };
        }

        public static Respuesta<T> Informativo(T data, string mensaje = "Información procesada correctamente")
        {
            return new Respuesta<T>
            {
                blnIndicadorTransaccion = true,
                strTituloRespuesta = "Información",
                strMensajeRespuesta = mensaje,
                enuTipoMensaje = 2,
                ValorRetorno = data
            };
        }

        public static Respuesta<T> Validacion(string mensaje = "Error de validación")
        {
            return new Respuesta<T>
            {
                blnIndicadorTransaccion = false,
                strTituloRespuesta = "Validación",
                strMensajeRespuesta = mensaje,
                enuTipoMensaje = 3,
                ValorRetorno = default
            };
        }

        public static Respuesta<T> Error(string mensaje = "Ocurrió un error")
        {
            return new Respuesta<T>
            {
                blnIndicadorTransaccion = false,
                strTituloRespuesta = "Error",
                strMensajeRespuesta = mensaje,
                enuTipoMensaje = 4,
                ValorRetorno = default
            };
        }
    }
}
