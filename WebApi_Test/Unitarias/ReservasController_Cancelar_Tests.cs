using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReservasLaboratorios.Api.Models;
using WebAPI.Controllers;
using WebAPI.Data;
using WebAPI.DTOs;
using WebAPI.Models;
using Xunit;

namespace WebApi_Test.Pruebas_Unitarias
{
    public class ReservasController_Cancelar_Tests
    {
        private static ReservasDBContext CrearContexto()
        {
            var options = new DbContextOptionsBuilder<ReservasDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ReservasDBContext(options);
        }

        // El controlador lee el id del usuario del token, así que se simula un usuario autenticado
        private static ReservasController CrearControlador(ReservasDBContext context, int? usuarioId)
        {
            var controller = new ReservasController(context);
            var identity = usuarioId is null
                ? new ClaimsIdentity()
                : new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, usuarioId.Value.ToString()) }, "Test");

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
            return controller;
        }

        private static async Task<Reserva> AgregarReserva(ReservasDBContext context, int usuarioId,
            DateTime fecha, TimeSpan inicio, TimeSpan fin, string estado = "Activa")
        {
            var laboratorio = new Laboratorio
            {
                Nombre = "Lab de prueba",
                Ubicacion = "Edificio A",
                Capacidad = 20,
                Estado = "Habilitado",
                HoraApertura = TimeSpan.FromHours(7),
                HoraCierre = TimeSpan.FromHours(22),
                Active = true
            };
            context.Laboratorios.Add(laboratorio);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var reserva = new Reserva
            {
                LaboratorioId = laboratorio.LaboratorioId,
                UsuarioId = usuarioId,
                Fecha = fecha,
                HoraInicio = inicio,
                HoraFin = fin,
                Estado = estado,
                FechaRegistro = DateTime.Now,
                Active = true
            };
            context.Reservas.Add(reserva);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return reserva;
        }

        private static DateTime FechaFutura => DateTime.Today.AddDays(7);

        [Fact]
        public async Task Cancelar_HappyCase()
        {
            // Arrange: se prepara una reserva futura del usuario autenticado.
            using var context = CrearContexto();
            var reserva = await AgregarReserva(context, 1, FechaFutura, TimeSpan.FromHours(9), TimeSpan.FromHours(11));
            var controller = CrearControlador(context, 1);

            // Act: se cancela la reserva con un motivo válido.
            var result = await controller.Cancelar(reserva.ReservaId, new CancelarReservaRequest { Motivo = "  No puedo asistir  " });

            // Assert: se actualiza la reserva y se registra la cancelación.
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var body = Assert.IsType<CancelarReservaResponse>(ok.Value);
            Assert.Equal("Cancelada", body.Estado);

            Assert.Equal("Cancelada", (await context.Reservas.SingleAsync(TestContext.Current.CancellationToken)).Estado);

            var cancelacion = await context.Cancelaciones.SingleAsync(TestContext.Current.CancellationToken);
            Assert.Equal(reserva.ReservaId, cancelacion.ReservaId);
            Assert.Equal(1, cancelacion.CanceladaPorId);
            Assert.Equal("No puedo asistir", cancelacion.Motivo);
        }

        [Fact]
        public async Task Cancelar_SinMotivo_Funciona()
        {
            // Arrange: se prepara una reserva futura del usuario autenticado.
            using var context = CrearContexto();
            var reserva = await AgregarReserva(context, 1, FechaFutura, TimeSpan.FromHours(9), TimeSpan.FromHours(11));
            var controller = CrearControlador(context, 1);

            // Act: se cancela la reserva sin indicar motivo.
            var result = await controller.Cancelar(reserva.ReservaId, null);

            // Assert: la cancelación se realiza y conserva el motivo nulo.
            Assert.IsType<OkObjectResult>(result.Result);
            Assert.Null((await context.Cancelaciones.SingleAsync(TestContext.Current.CancellationToken)).Motivo);
        }

        [Fact]
        public async Task Cancelar_ReservaDeOtroUsuario_NoEncontrada()
        {
            // Arrange: se prepara una reserva cuyo propietario es otro usuario.
            using var context = CrearContexto();
            var reserva = await AgregarReserva(context, 1, FechaFutura, TimeSpan.FromHours(9), TimeSpan.FromHours(11));
            var controller = CrearControlador(context, 2); // no es la dueña

            // Act: un usuario distinto intenta cancelar la reserva.
            var result = await controller.Cancelar(reserva.ReservaId, null);

            // Assert: la API no expone la reserva ni modifica su estado.
            Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal("Activa", (await context.Reservas.SingleAsync(TestContext.Current.CancellationToken)).Estado);
            Assert.Empty(context.Cancelaciones);
        }

        [Fact]
        public async Task Cancelar_ReservaInexistente_NoEncontrada()
        {
            // Arrange: se prepara un usuario autenticado sin reservas.
            using var context = CrearContexto();
            var controller = CrearControlador(context, 1);

            // Act: se intenta cancelar un identificador inexistente.
            var result = await controller.Cancelar(999, null);

            // Assert: la API informa que no encuentra la reserva.
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task Cancelar_YaCancelada_Conflicto()
        {
            // Arrange: se prepara una reserva que ya está cancelada.
            using var context = CrearContexto();
            var reserva = await AgregarReserva(context, 1, FechaFutura, TimeSpan.FromHours(9), TimeSpan.FromHours(11), "Cancelada");
            var controller = CrearControlador(context, 1);

            // Act: se intenta cancelar nuevamente la misma reserva.
            var result = await controller.Cancelar(reserva.ReservaId, null);

            // Assert: la API evita una segunda cancelación.
            Assert.IsType<ConflictObjectResult>(result.Result);
        }

        [Fact]
        public async Task Cancelar_ReservaPasada_Conflicto()
        {
            // Arrange: se prepara una reserva que ya inició o finalizó.
            using var context = CrearContexto();
            var reserva = await AgregarReserva(context, 1, DateTime.Today.AddDays(-1), TimeSpan.FromHours(9), TimeSpan.FromHours(11));
            var controller = CrearControlador(context, 1);

            // Act: se intenta cancelar una reserva pasada.
            var result = await controller.Cancelar(reserva.ReservaId, null);

            // Assert: la API conserva activa la reserva pasada.
            Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.Equal("Activa", (await context.Reservas.SingleAsync(TestContext.Current.CancellationToken)).Estado);
        }

        [Fact]
        public async Task Cancelar_MotivoDemasiadoLargo_BadRequest()
        {
            // Arrange: se prepara un motivo que supera el límite permitido.
            using var context = CrearContexto();
            var reserva = await AgregarReserva(context, 1, FechaFutura, TimeSpan.FromHours(9), TimeSpan.FromHours(11));
            var controller = CrearControlador(context, 1);

            // Act: se intenta cancelar con un motivo inválido.
            var result = await controller.Cancelar(reserva.ReservaId, new CancelarReservaRequest { Motivo = new string('a', 256) });

            // Assert: la API rechaza la solicitud y no modifica la reserva.
            Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Activa", (await context.Reservas.SingleAsync(TestContext.Current.CancellationToken)).Estado);
        }

        [Fact]
        public async Task Cancelar_SinToken_NoAutorizado()
        {
            // Arrange: se prepara una reserva y un contexto sin autenticación.
            using var context = CrearContexto();
            var reserva = await AgregarReserva(context, 1, FechaFutura, TimeSpan.FromHours(9), TimeSpan.FromHours(11));
            var controller = CrearControlador(context, null);

            // Act: se intenta cancelar sin token.
            var result = await controller.Cancelar(reserva.ReservaId, null);

            // Assert: la API rechaza la solicitud por falta de autenticación.
            Assert.IsType<UnauthorizedResult>(result.Result);
        }
    }
}
