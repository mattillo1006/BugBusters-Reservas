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

namespace WebApi_Test
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
            using var context = CrearContexto();
            var reserva = await AgregarReserva(context, 1, FechaFutura, TimeSpan.FromHours(9), TimeSpan.FromHours(11));
            var controller = CrearControlador(context, 1);

            var result = await controller.Cancelar(reserva.ReservaId, new CancelarReservaRequest { Motivo = "  No puedo asistir  " });

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var body = Assert.IsType<CancelarReservaResponse>(ok.Value);
            Assert.Equal("Cancelada", body.Estado);

            Assert.Equal("Cancelada", (await context.Reservas.SingleAsync(TestContext.Current.CancellationToken)).Estado);

            var cancelacion = await context.Cancelaciones.SingleAsync(cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(reserva.ReservaId, cancelacion.ReservaId);
            Assert.Equal(1, cancelacion.CanceladaPorId);
            Assert.Equal("No puedo asistir", cancelacion.Motivo);
        }

        [Fact]
        public async Task Cancelar_SinMotivo_Funciona()
        {
            using var context = CrearContexto();
            var reserva = await AgregarReserva(context, 1, FechaFutura, TimeSpan.FromHours(9), TimeSpan.FromHours(11));
            var controller = CrearControlador(context, 1);

            var result = await controller.Cancelar(reserva.ReservaId, null);

            Assert.IsType<OkObjectResult>(result.Result);
            Assert.Null((await context.Cancelaciones.SingleAsync(TestContext.Current.CancellationToken)).Motivo);
        }

        [Fact]
        public async Task Cancelar_ReservaDeOtroUsuario_NoEncontrada()
        {
            using var context = CrearContexto();
            var reserva = await AgregarReserva(context, 1, FechaFutura, TimeSpan.FromHours(9), TimeSpan.FromHours(11));
            var controller = CrearControlador(context, 2); // no es la dueña

            var result = await controller.Cancelar(reserva.ReservaId, null);

            Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal("Activa", (await context.Reservas.SingleAsync(TestContext.Current.CancellationToken)).Estado);
            Assert.Empty(context.Cancelaciones);
        }

        [Fact]
        public async Task Cancelar_ReservaInexistente_NoEncontrada()
        {
            using var context = CrearContexto();
            var controller = CrearControlador(context, 1);

            var result = await controller.Cancelar(999, null);

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task Cancelar_YaCancelada_Conflicto()
        {
            using var context = CrearContexto();
            var reserva = await AgregarReserva(context, 1, FechaFutura, TimeSpan.FromHours(9), TimeSpan.FromHours(11), "Cancelada");
            var controller = CrearControlador(context, 1);

            var result = await controller.Cancelar(reserva.ReservaId, null);

            Assert.IsType<ConflictObjectResult>(result.Result);
        }

        [Fact]
        public async Task Cancelar_ReservaPasada_Conflicto()
        {
            using var context = CrearContexto();
            var reserva = await AgregarReserva(context, 1, DateTime.Today.AddDays(-1), TimeSpan.FromHours(9), TimeSpan.FromHours(11));
            var controller = CrearControlador(context, 1);

            var result = await controller.Cancelar(reserva.ReservaId, null);

            Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.Equal("Activa", (await context.Reservas.SingleAsync(TestContext.Current.CancellationToken)).Estado);
        }

        [Fact]
        public async Task Cancelar_MotivoDemasiadoLargo_BadRequest()
        {
            using var context = CrearContexto();
            var reserva = await AgregarReserva(context, 1, FechaFutura, TimeSpan.FromHours(9), TimeSpan.FromHours(11));
            var controller = CrearControlador(context, 1);

            var result = await controller.Cancelar(reserva.ReservaId, new CancelarReservaRequest { Motivo = new string('a', 256) });

            Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Activa", (await context.Reservas.SingleAsync(TestContext.Current.CancellationToken)).Estado);
        }

        [Fact]
        public async Task Cancelar_SinToken_NoAutorizado()
        {
            using var context = CrearContexto();
            var reserva = await AgregarReserva(context, 1, FechaFutura, TimeSpan.FromHours(9), TimeSpan.FromHours(11));
            var controller = CrearControlador(context, null);

            var result = await controller.Cancelar(reserva.ReservaId, null);

            Assert.IsType<UnauthorizedResult>(result.Result);
        }
    }
}