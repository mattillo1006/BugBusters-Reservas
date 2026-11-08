using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReservasLaboratorios.Api.Models;
using WebAPI.Controllers;
using WebAPI.Data;
using WebAPI.DTOs;
using WebAPI.Models;
using Xunit;

namespace WebApi_Test.Unitarias;

public class LaboratoriosController_Get_Tests
{
    private static readonly DateTime FechaBase = DateTime.Today.AddDays(10);

    private static ReservasDBContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<ReservasDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ReservasDBContext(options);
    }

    private static async Task<Laboratorio> AgregarLaboratorio(
        ReservasDBContext context,
        string nombre,
        bool active = true)
    {
        var laboratorio = new Laboratorio
        {
            Nombre = nombre,
            Ubicacion = "Edificio A",
            Capacidad = 20,
            Estado = "Habilitado",
            HoraApertura = TimeSpan.FromHours(7),
            HoraCierre = TimeSpan.FromHours(22),
            Active = active
        };

        context.Laboratorios.Add(laboratorio);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        return laboratorio;
    }

    private static async Task AgregarReserva(
        ReservasDBContext context,
        int laboratorioId,
        DateTime fecha,
        string estado = "Activa")
    {
        context.Reservas.Add(new Reserva
        {
            LaboratorioId = laboratorioId,
            UsuarioId = 1,
            Fecha = fecha,
            HoraInicio = TimeSpan.FromHours(9),
            HoraFin = TimeSpan.FromHours(11),
            Estado = estado,
            Active = true,
            FechaRegistro = DateTime.Now
        });

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetAll_SoloLaboratoriosActivos_RetornaOk()
    {
        // Arrange: se preparan un laboratorio activo y otro inactivo.
        using var context = CrearContexto();
        var activo = await AgregarLaboratorio(context, "Laboratorio activo");
        await AgregarLaboratorio(context, "Laboratorio inactivo", active: false);
        var controller = new LaboratoriosController(context);

        // Act: se solicita la lista de laboratorios.
        var result = await controller.GetAll();

        // Assert: la respuesta contiene únicamente el laboratorio activo y sus datos.
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var laboratorios = Assert.IsAssignableFrom<IEnumerable<LaboratorioResponse>>(ok.Value).ToList();
        var laboratorio = Assert.Single(laboratorios);
        Assert.Equal(activo.LaboratorioId, laboratorio.LaboratorioId);
        Assert.Equal("Laboratorio activo", laboratorio.Nombre);
        Assert.Equal(TimeSpan.FromHours(7), laboratorio.HoraApertura);
    }

    [Fact]
    public async Task GetAll_SinLaboratoriosActivos_RetornaListaVacia()
    {
        // Arrange: se prepara únicamente un laboratorio inactivo.
        using var context = CrearContexto();
        await AgregarLaboratorio(context, "Laboratorio inactivo", active: false);
        var controller = new LaboratoriosController(context);

        // Act: se solicita la lista de laboratorios.
        var result = await controller.GetAll();

        // Assert: la respuesta correcta no contiene elementos.
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var laboratorios = Assert.IsAssignableFrom<IEnumerable<LaboratorioResponse>>(ok.Value);
        Assert.Empty(laboratorios);
    }

    [Fact]
    public async Task GetById_LaboratorioActivo_RetornaOk()
    {
        // Arrange: se prepara un laboratorio activo.
        using var context = CrearContexto();
        var laboratorioEsperado = await AgregarLaboratorio(context, "Laboratorio consultado");
        var controller = new LaboratoriosController(context);

        // Act: se consulta el laboratorio por su identificador.
        var result = await controller.GetById(laboratorioEsperado.LaboratorioId);

        // Assert: la respuesta contiene el laboratorio solicitado.
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var laboratorio = Assert.IsType<LaboratorioResponse>(ok.Value);
        Assert.Equal(laboratorioEsperado.LaboratorioId, laboratorio.LaboratorioId);
        Assert.Equal("Laboratorio consultado", laboratorio.Nombre);
    }

    [Fact]
    public async Task GetById_LaboratorioInexistente_RetornaNotFound()
    {
        // Arrange: se prepara un controlador sin laboratorios registrados.
        using var context = CrearContexto();
        var controller = new LaboratoriosController(context);

        // Act: se consulta un identificador inexistente.
        var result = await controller.GetById(999);

        // Assert: la API informa que no encuentra el laboratorio.
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetById_LaboratorioInactivo_RetornaNotFound()
    {
        // Arrange: se prepara un laboratorio inactivo.
        using var context = CrearContexto();
        var laboratorio = await AgregarLaboratorio(context, "Laboratorio inactivo", active: false);
        var controller = new LaboratoriosController(context);

        // Act: se consulta el laboratorio inactivo.
        var result = await controller.GetById(laboratorio.LaboratorioId);

        // Assert: la API no expone laboratorios inactivos.
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetReservasRango_SoloReservasActivasDelLaboratorioYRango_RetornaOk()
    {
        // Arrange: se preparan reservas que deben y no deben aparecer en el resultado.
        using var context = CrearContexto();
        var laboratorio = await AgregarLaboratorio(context, "Laboratorio principal");
        var otroLaboratorio = await AgregarLaboratorio(context, "Otro laboratorio");
        await AgregarReserva(context, laboratorio.LaboratorioId, FechaBase);
        await AgregarReserva(context, laboratorio.LaboratorioId, FechaBase, "Cancelada");
        await AgregarReserva(context, otroLaboratorio.LaboratorioId, FechaBase);
        await AgregarReserva(context, laboratorio.LaboratorioId, FechaBase.AddDays(-1));
        await AgregarReserva(context, laboratorio.LaboratorioId, FechaBase.AddDays(1));
        var controller = new LaboratoriosController(context);

        // Act: se consulta únicamente la fecha central del rango.
        var result = await controller.GetReservasRango(laboratorio.LaboratorioId, FechaBase, FechaBase);

        // Assert: solo se devuelve la reserva activa del laboratorio y fecha solicitados.
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var reservas = Assert.IsAssignableFrom<IEnumerable<ReservaOcupadaResponse>>(ok.Value).ToList();
        var reserva = Assert.Single(reservas);
        Assert.Equal(FechaBase.Date, reserva.Fecha.Date);
        Assert.Equal(TimeSpan.FromHours(9), reserva.HoraInicio);
    }

    [Fact]
    public async Task GetReservasRango_FechasLimiteInclusivas_RetornaAmbasReservas()
    {
        // Arrange: se preparan reservas exactamente en los límites del rango.
        using var context = CrearContexto();
        var laboratorio = await AgregarLaboratorio(context, "Laboratorio principal");
        var desde = FechaBase;
        var hasta = FechaBase.AddDays(4);
        await AgregarReserva(context, laboratorio.LaboratorioId, desde);
        await AgregarReserva(context, laboratorio.LaboratorioId, hasta);
        var controller = new LaboratoriosController(context);

        // Act: se consulta el rango que incluye ambas fechas.
        var result = await controller.GetReservasRango(laboratorio.LaboratorioId, desde, hasta);

        // Assert: se devuelven las reservas en ambos extremos del rango.
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var reservas = Assert.IsAssignableFrom<IEnumerable<ReservaOcupadaResponse>>(ok.Value).ToList();
        Assert.Equal(2, reservas.Count);
        Assert.Contains(reservas, r => r.Fecha.Date == desde.Date);
        Assert.Contains(reservas, r => r.Fecha.Date == hasta.Date);
    }

    [Fact]
    public async Task GetReservasRango_SinReservas_RetornaListaVacia()
    {
        // Arrange: se prepara un laboratorio sin reservas activas en el rango.
        using var context = CrearContexto();
        var laboratorio = await AgregarLaboratorio(context, "Laboratorio sin reservas");
        var controller = new LaboratoriosController(context);

        // Act: se consulta un rango sin reservas.
        var result = await controller.GetReservasRango(
            laboratorio.LaboratorioId, FechaBase, FechaBase.AddDays(4));

        // Assert: la respuesta correcta no contiene reservas.
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var reservas = Assert.IsAssignableFrom<IEnumerable<ReservaOcupadaResponse>>(ok.Value);
        Assert.Empty(reservas);
    }
}
