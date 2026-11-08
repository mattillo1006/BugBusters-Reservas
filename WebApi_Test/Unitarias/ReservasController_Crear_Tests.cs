using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ReservasLaboratorios.Api.Models;
using WebAPI.Controllers;
using WebAPI.Data;
using WebAPI.DTOs;
using WebAPI.Models;
using Xunit;

namespace WebApi_Test.Pruebas_Unitarias;

public class ReservasController_Crear_Tests
{
    private static readonly DateTime FechaFutura = DateTime.Today.AddDays(7);

    private static ReservasDBContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<ReservasDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            // La transacción se valida con SQL Server en pruebas de integración.
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ReservasDBContext(options);
    }

    private static ReservasController CrearControlador(ReservasDBContext context, int? usuarioId)
    {
        var identity = usuarioId is null
            ? new ClaimsIdentity()
            : new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, usuarioId.Value.ToString()) },
                "Test");

        return new ReservasController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
    }

    private static async Task<Laboratorio> AgregarLaboratorio(
        ReservasDBContext context,
        string estado = "Habilitado",
        bool active = true)
    {
        var laboratorio = new Laboratorio
        {
            Nombre = "Lab de pruebas",
            Ubicacion = "Edificio A",
            Capacidad = 20,
            Estado = estado,
            HoraApertura = TimeSpan.FromHours(7),
            HoraCierre = TimeSpan.FromHours(22),
            Active = active
        };

        context.Laboratorios.Add(laboratorio);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return laboratorio;
    }

    private static CrearReservaRequest SolicitudValida(int laboratorioId) => new()
    {
        LaboratorioId = laboratorioId,
        Fecha = FechaFutura,
        HoraInicio = TimeSpan.FromHours(9),
        HoraFin = TimeSpan.FromHours(11)
    };

    [Fact]
    public async Task Crear_SolicitudValida_CreaReserva()
    {
        // Arrange: se prepara un laboratorio habilitado y un usuario autenticado.
        using var context = CrearContexto();
        var laboratorio = await AgregarLaboratorio(context);
        var controller = CrearControlador(context, 12);

        // Act: se intenta registrar una reserva con datos válidos.
        var result = await controller.Crear(SolicitudValida(laboratorio.LaboratorioId));

        // Assert: se crea la reserva y se persiste con los datos solicitados.
        var created = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        var body = Assert.IsType<ReservaResponse>(created.Value);
        Assert.Equal("Activa", body.Estado);
        Assert.Equal(laboratorio.LaboratorioId, body.LaboratorioId);

        var reserva = await context.Reservas.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(12, reserva.UsuarioId);
        Assert.Equal(TimeSpan.FromHours(9), reserva.HoraInicio);
        Assert.Equal(TimeSpan.FromHours(11), reserva.HoraFin);
    }

    [Fact]
    public async Task Crear_SinToken_RetornaNoAutorizado()
    {
        // Arrange: se prepara el controlador sin un usuario autenticado.
        using var context = CrearContexto();
        var controller = CrearControlador(context, null);

        // Act: se intenta crear una reserva sin token.
        var result = await controller.Crear(new CrearReservaRequest());

        // Assert: la API rechaza la solicitud por falta de autenticación.
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task Crear_CamposObligatoriosFaltantes_RetornaBadRequest()
    {
        // Arrange: se prepara un usuario autenticado con una solicitud vacía.
        using var context = CrearContexto();
        var controller = CrearControlador(context, 1);

        // Act: se intenta crear una reserva sin los datos obligatorios.
        var result = await controller.Crear(new CrearReservaRequest());

        // Assert: la API informa que la solicitud no es válida.
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Crear_LaboratorioInexistente_RetornaNotFound()
    {
        // Arrange: se prepara un usuario autenticado sin registrar laboratorios.
        using var context = CrearContexto();
        var controller = CrearControlador(context, 1);

        // Act: se solicita una reserva para un laboratorio que no existe.
        var result = await controller.Crear(SolicitudValida(999));

        // Assert: la API no encuentra el laboratorio solicitado.
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Crear_HoraFinalNoPosterior_RetornaBadRequest()
    {
        // Arrange: se prepara una solicitud cuya hora final coincide con la inicial.
        using var context = CrearContexto();
        var laboratorio = await AgregarLaboratorio(context);
        var request = SolicitudValida(laboratorio.LaboratorioId);
        request.HoraFin = request.HoraInicio;

        // Act: se intenta crear la reserva con un horario inválido.
        var result = await CrearControlador(context, 1).Crear(request);

        // Assert: la API rechaza el orden de las horas.
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Crear_FechaPasada_RetornaBadRequest()
    {
        // Arrange: se prepara una solicitud para una fecha anterior a hoy.
        using var context = CrearContexto();
        var laboratorio = await AgregarLaboratorio(context);
        var request = SolicitudValida(laboratorio.LaboratorioId);
        request.Fecha = DateTime.Today.AddDays(-1);

        // Act: se intenta crear la reserva en una fecha pasada.
        var result = await CrearControlador(context, 1).Crear(request);

        // Assert: la API rechaza fechas pasadas.
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Crear_FueraDeHorario_RetornaBadRequest()
    {
        // Arrange: se prepara una solicitud antes de la hora de apertura.
        using var context = CrearContexto();
        var laboratorio = await AgregarLaboratorio(context);
        var request = SolicitudValida(laboratorio.LaboratorioId);
        request.HoraInicio = TimeSpan.FromHours(6);

        // Act: se intenta crear la reserva fuera del horario del laboratorio.
        var result = await CrearControlador(context, 1).Crear(request);

        // Assert: la API rechaza el horario fuera de operación.
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Crear_DuracionMayorACuatroHoras_RetornaBadRequest()
    {
        // Arrange: se prepara una solicitud con duración superior a cuatro horas.
        using var context = CrearContexto();
        var laboratorio = await AgregarLaboratorio(context);
        var request = SolicitudValida(laboratorio.LaboratorioId);
        request.HoraFin = TimeSpan.FromHours(14);

        // Act: se intenta crear la reserva con duración inválida.
        var result = await CrearControlador(context, 1).Crear(request);

        // Assert: la API aplica el límite máximo de duración.
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Crear_LaboratorioFueraDeServicio_RetornaConflicto()
    {
        // Arrange: se prepara un laboratorio fuera de servicio.
        using var context = CrearContexto();
        var laboratorio = await AgregarLaboratorio(context, estado: "FueraDeServicio");

        // Act: se intenta reservar el laboratorio fuera de servicio.
        var result = await CrearControlador(context, 1).Crear(SolicitudValida(laboratorio.LaboratorioId));

        // Assert: la API informa que el laboratorio no puede reservarse.
        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task Crear_HorarioTraslapado_RetornaConflicto()
    {
        // Arrange: se prepara una reserva activa que se traslapa con la solicitud.
        using var context = CrearContexto();
        var laboratorio = await AgregarLaboratorio(context);
        context.Reservas.Add(new Reserva
        {
            LaboratorioId = laboratorio.LaboratorioId,
            UsuarioId = 2,
            Fecha = FechaFutura,
            HoraInicio = TimeSpan.FromHours(10),
            HoraFin = TimeSpan.FromHours(12),
            Estado = "Activa",
            Active = true,
            FechaRegistro = DateTime.Now
        });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act: se intenta reservar un horario ya ocupado.
        var result = await CrearControlador(context, 1).Crear(SolicitudValida(laboratorio.LaboratorioId));

        // Assert: la API rechaza el traslape y conserva una sola reserva.
        Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Single(context.Reservas);
    }
}
