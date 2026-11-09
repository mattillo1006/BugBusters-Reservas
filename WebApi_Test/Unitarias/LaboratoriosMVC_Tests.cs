using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Moq;
using SistemaDeReservas.Controllers;
using Xunit;

namespace WebApi_Test;

public class LaboratoriosMvcController_Mock_Tests
{
    [Fact]
    public async Task CambiarEstado_RespuestaForbidden_MuestraMensajeDePermisos()
    {
        // Arrange: se simula que la WebAPI responde 403 al intentar cambiar el estado.
        using var client = new HttpClient(new RespuestaHttpFalsa(HttpStatusCode.Forbidden))
        {
            BaseAddress = new Uri("http://webapi-pruebas/")
        };
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(factory => factory.CreateClient("WebAPI")).Returns(client);

        var httpContext = new DefaultHttpContext();
        var tempDataProviderMock = new Mock<ITempDataProvider>();
        tempDataProviderMock
            .Setup(provider => provider.LoadTempData(It.IsAny<HttpContext>()))
            .Returns(new Dictionary<string, object>());

        var controller = new LaboratoriosController(factoryMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, tempDataProviderMock.Object)
        };

        // Act: se solicita cambiar el estado de un laboratorio.
        var result = await controller.CambiarEstado(
            id: 3,
            estado: "FueraDeServicio",
            laboratorioOrigenId: 1,
            semana: new DateTime(2026, 11, 9));

        // Assert: se presenta el mensaje de permisos y se redirige a disponibilidad.
        Assert.Equal("No tienes permisos para cambiar el estado de este laboratorio.", controller.TempData["Error"]);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Disponibilidad", redirect.ActionName);
        Assert.Equal(1, redirect.RouteValues!["id"]);
        Assert.Equal("2026-11-09", redirect.RouteValues["semana"]);
        factoryMock.Verify(factory => factory.CreateClient("WebAPI"), Times.Once);
    }

    private sealed class RespuestaHttpFalsa : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;

        public RespuestaHttpFalsa(HttpStatusCode statusCode) => _statusCode = statusCode;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(_statusCode));
    }
}
