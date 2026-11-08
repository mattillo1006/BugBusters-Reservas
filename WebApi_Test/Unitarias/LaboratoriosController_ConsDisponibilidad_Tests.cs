using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReservasLaboratorios.Api.Models;
using WebAPI.Controllers;
using WebAPI.Data;
using WebAPI.DTOs;
using WebAPI.Models;
using Xunit;

namespace WebApi_Test.Unitarias;

public class LaboratoriosController_Disponibilidad_Tests
{
    private static readonly DateTime FechaFutura = DateTime.Today.AddDays(7);

    private static ReservasDBContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<ReservasDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ReservasDBContext(options);
    }

    private static async Task<Laboratorio> AgregarLaboratorio(
        ReservasDBContext context,
        string estado = "Habilitado")
    {
        var laboratorio = new Laboratorio
        {
            Nombre = "Lab de pruebas",
            Ubicacion = "Edificio A",
            Capacidad = 20,
            Estado = estado,
            HoraApertura = TimeSpan.FromHours(7),
            HoraCierre = TimeSpan.FromHours(22),
            Active = true
        };

        context.Laboratorios.Add(laboratorio);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return laboratorio;
    }

    private static async Task AgregarReservaActiva(
        ReservasDBContext context,
        int laboratorioId,
        TimeSpan inicio,
        TimeSpan fin)
    {
        context.Reservas.Add(new Reserva
        {
            LaboratorioId = laboratorioId,
            UsuarioId = 2,
            Fecha = FechaFutura,
            HoraInicio = inicio,
            HoraFin = fin,
            Estado = "Activa",
            Active = true,
            FechaRegistro = DateTime.Now
        });

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ConsultarDisponibilidad_LaboratorioInexistente_RetornaNotFound()
    {
        // Arrange: se prepara un controlador sin laboratorios registrados.
        using var context = CrearContexto();
        var controller = new LaboratoriosController(context);

        // Act: se consulta un laboratorio inexistente.
        var result = await controller.ConsultarDisponibilidad(
            999, FechaFutura, TimeSpan.FromHours(9), TimeSpan.FromHours(11));

        // Assert: la API informa que no encuentra el laboratorio.
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task ConsultarDisponibilidad_HoraFinalNoPosterior_RetornaBadRequest()
    {
        // Arrange: se prepara un laboratorio habilitado.
        using var context = CrearContexto();
        var laboratorio = await AgregarLaboratorio(context);
        var controller = new LaboratoriosController(context);

        // Act: se consulta un intervalo con la misma hora de inicio y finalización.
        var result = await controller.ConsultarDisponibilidad(
            laboratorio.LaboratorioId, FechaFutura, TimeSpan.FromHours(10), TimeSpan.FromHours(10));

        // Assert: la API rechaza el orden inválido de las horas.
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task ConsultarDisponibilidad_FechaPasada_RetornaBadRequest()
    {
        // Arrange: se prepara un laboratorio habilitado.
        using var context = CrearContexto();
        var laboratorio = await AgregarLaboratorio(context);
        var controller = new LaboratoriosController(context);

        // Act: se consulta disponibilidad para una fecha pasada.
        var result = await controller.ConsultarDisponibilidad(
            laboratorio.LaboratorioId, DateTime.Today.AddDays(-1), TimeSpan.FromHours(9), TimeSpan.FromHours(11));

        // Assert: la API rechaza consultas en fechas pasadas.
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task ConsultarDisponibilidad_FueraDeHorario_RetornaBadRequest()
    {
        // Arrange: se prepara un laboratorio que abre a las 07:00.
        using var context = CrearContexto();
        var laboratorio = await AgregarLaboratorio(context);
        var controller = new LaboratoriosController(context);

        // Act: se consulta un horario anterior a la apertura.
        var result = await controller.ConsultarDisponibilidad(
            laboratorio.LaboratorioId, FechaFutura, TimeSpan.FromHours(6), TimeSpan.FromHours(8));

        // Assert: la API rechaza horarios fuera de operación.
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task ConsultarDisponibilidad_DuracionMayorACuatroHoras_RetornaBadRequest()
    {
        // Arrange: se prepara un laboratorio habilitado.
        using var context = CrearContexto();
        var laboratorio = await AgregarLaboratorio(context);
        var controller = new LaboratoriosController(context);

        // Act: se consulta un intervalo de cinco horas.
        var result = await controller.ConsultarDisponibilidad(
            laboratorio.LaboratorioId, FechaFutura, TimeSpan.FromHours(9), TimeSpan.FromHours(14));

        // Assert: la API aplica el límite máximo de duración.
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task ConsultarDisponibilidad_LaboratorioFueraDeServicio_NoDisponible()
    {
        // Arrange: se prepara un laboratorio fuera de servicio.
        using var context = CrearContexto();
        var laboratorio = await AgregarLaboratorio(context, "FueraDeServicio");
        var controller = new LaboratoriosController(context);

        // Act: se consulta un horario válido del laboratorio.
        var result = await controller.ConsultarDisponibilidad(
            laboratorio.LaboratorioId, FechaFutura, TimeSpan.FromHours(9), TimeSpan.FromHours(11));

        // Assert: la respuesta es correcta, pero indica que no está disponible.
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<DisponibilidadResponse>(ok.Value);
        Assert.False(body.Disponible);
    }

    [Fact]
    public async Task ConsultarDisponibilidad_HorarioTraslapado_NoDisponible()
    {
        // Arrange: se prepara una reserva activa que ocupa parte del horario consultado.
        using var context = CrearContexto();
        var laboratorio = await AgregarLaboratorio(context);
        await AgregarReservaActiva(context, laboratorio.LaboratorioId, TimeSpan.FromHours(10), TimeSpan.FromHours(12));
        var controller = new LaboratoriosController(context);

        // Act: se consulta un horario que se traslapa con la reserva activa.
        var result = await controller.ConsultarDisponibilidad(
            laboratorio.LaboratorioId, FechaFutura, TimeSpan.FromHours(9), TimeSpan.FromHours(11));

        // Assert: la respuesta indica que el horario está ocupado.
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<DisponibilidadResponse>(ok.Value);
        Assert.False(body.Disponible);
    }

    [Fact]
    public async Task ConsultarDisponibilidad_HorarioLibre_EstaDisponible()
    {
        // Arrange: se prepara un laboratorio habilitado sin reservas activas.
        using var context = CrearContexto();
        var laboratorio = await AgregarLaboratorio(context);
        var controller = new LaboratoriosController(context);

        // Act: se consulta un horario válido y libre.
        var result = await controller.ConsultarDisponibilidad(
            laboratorio.LaboratorioId, FechaFutura, TimeSpan.FromHours(9), TimeSpan.FromHours(11));

        // Assert: la respuesta indica que el horario está disponible.
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<DisponibilidadResponse>(ok.Value);
        Assert.True(body.Disponible);
    }
}
