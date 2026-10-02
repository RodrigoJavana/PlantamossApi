using Microsoft.AspNetCore.Mvc;
using PlantamossApi.DTOs;
using PlantamossApi.Helpers;

namespace PlantamossApi.Controllers;

/// <summary>
/// Clase base de todos los controladores: atajos para responder errores con la forma del contrato
/// <c>{ message, errors? }</c> y para leer el usuario autenticado del JWT.
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Id del usuario autenticado (claim "sub" del JWT).</summary>
    protected long UsuarioId => long.Parse(User.FindFirst("sub")!.Value);

    protected ObjectResult Error(int status, string mensaje) => StatusCode(status, new ApiError(mensaje));

    protected ObjectResult NoEncontrado(string mensaje) => Error(StatusCodes.Status404NotFound, mensaje);

    protected ObjectResult Prohibido(string mensaje) => Error(StatusCodes.Status403Forbidden, mensaje);

    protected ObjectResult ReglaNegocio(string mensaje) => Error(StatusCodes.Status400BadRequest, mensaje);

    /// <summary>422 con los errores acumulados.</summary>
    protected ObjectResult Invalido(ErroresValidacion errores, string mensaje = "Datos inválidos.") =>
        StatusCode(StatusCodes.Status422UnprocessableEntity, errores.ComoApiError(mensaje));

    /// <summary>422 con un solo error de un campo (el mensaje general es el mismo texto del error).</summary>
    protected ObjectResult Invalido(string campo, string mensaje)
    {
        var errores = new ErroresValidacion();
        errores.Agregar(campo, mensaje);
        return Invalido(errores, mensaje);
    }
}
