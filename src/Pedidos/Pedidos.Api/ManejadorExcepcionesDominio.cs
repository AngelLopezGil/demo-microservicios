using Microsoft.AspNetCore.Diagnostics;
using Pedidos.Domain.Excepciones;

namespace Pedidos.Api;

public class ManejadorExcepcionesDominio : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not ExcepcionDeDominio)
            return false;

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(
            new { error = exception.Message }, cancellationToken);
        return true;
    }
}