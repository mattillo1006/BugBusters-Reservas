using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReservasLaboratorios.Api.Models;
using WebAPI.Controllers;
using WebAPI.Data;
using WebAPI.DTOs;
using WebAPI.Models;

namespace WebApi_IntegrationTests;

public class ReservasSql_Tests(SqlServerFixture sql) : IClassFixture<SqlServerFixture>
{
    [Fact]
    public async Task Crear_Traslape_RetornaConflicto()
    {
        // Arrange: se guardan un laboratorio y una reserva activa en SQL Server real.
        await using var context = CrearContexto();
        var laboratorio = new Laboratorio
        {
            Nombre = "Lab SQL",
            Ubicacion = "Edificio A",
            Capacidad = 20,
            Estado = "Habilitado",
            HoraApertura = TimeSpan.FromHours(7),
            HoraCierre = TimeSpan.FromHours(22),
            Active = true
        };
        context.Laboratorios.Add(laboratorio);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        context.Reservas.Add(new Reserva
        {
            LaboratorioId = laboratorio.LaboratorioId,
            UsuarioId = 2,
            Fecha = DateTime.Today.AddDays(7),
            HoraInicio = TimeSpan.FromHours(10),
            HoraFin = TimeSpan.FromHours(12),
            Estado = "Activa",
            FechaRegistro = DateTime.Now,
            Active = true
        });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var controller = CrearControlador(context, usuarioId: 1);
        var request = new CrearReservaRequest
        {
            LaboratorioId = laboratorio.LaboratorioId,
            Fecha = DateTime.Today.AddDays(7),
            HoraInicio = TimeSpan.FromHours(9),
            HoraFin = TimeSpan.FromHours(11)
        };

        // Act: se solicita un horario que coincide parcialmente con la reserva existente.
        var result = await controller.Crear(request);

        // Assert: SQL Server y la lógica de la API detectan el traslape y no insertan otra reserva.
        Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(1, await context.Reservas.CountAsync(TestContext.Current.CancellationToken));
    }

    private ReservasDBContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<ReservasDBContext>()
            .UseSqlServer(sql.ConnectionString)
            .Options;

        return new ReservasDBContext(options);
    }

    private static ReservasController CrearControlador(ReservasDBContext context, int usuarioId)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString())],
            "Test");

        return new ReservasController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
    }
}
