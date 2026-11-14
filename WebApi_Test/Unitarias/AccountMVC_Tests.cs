using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using SistemaDeReservas.Controllers;
using SistemaDeReservas.Models;

namespace WebApi_Test.Pruebas_Unitarias;

public class AccountMvc_Tests
{
    [Fact]
    public async Task Login_ModeloInvalido_NoLlamaALaApi()
    {
        // Arrange: se prepara un modelo inválido y una fábrica HTTP simulada.
        var factoryMock = new Mock<IHttpClientFactory>();
        var controller = new AccountController(factoryMock.Object);
        controller.ModelState.AddModelError("Username", "Obligatorio");

        // Act: se intenta iniciar sesión con datos inválidos.
        var result = await controller.Login(new LoginViewModel());

        // Assert: se vuelve a mostrar la vista sin hacer llamadas a la WebAPI.
        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<LoginViewModel>(view.Model);
        factoryMock.Verify(factory => factory.CreateClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Login_CredencialesInvalidas_MuestraError()
    {
        // Arrange: se simula una respuesta 401 de la WebAPI.
        var controller = CrearControlador(new HttpResponseMessage(HttpStatusCode.Unauthorized));

        // Act: se intenta iniciar sesión con credenciales rechazadas.
        var result = await controller.Login(new LoginViewModel
        {
            Username = "usuario",
            Password = "clave-invalida"
        });

        // Assert: el frontend conserva la vista y muestra un mensaje comprensible.
        Assert.IsType<ViewResult>(result);
        Assert.Equal("Usuario o contraseña incorrectos", controller.ModelState[string.Empty]!.Errors.Single().ErrorMessage);
    }

    [Fact]
    public async Task Login_SinConexion_MuestraError()
    {
        // Arrange: el manejador falso simula que la WebAPI no está disponible.
        var controller = CrearControlador(new HttpRequestException("No disponible"));

        // Act: se intenta iniciar sesión mientras falla la conexión HTTP.
        var result = await controller.Login(new LoginViewModel
        {
            Username = "usuario",
            Password = "clave"
        });

        // Assert: el usuario permanece en login y recibe el error de conexión.
        Assert.IsType<ViewResult>(result);
        Assert.Equal("No se pudo conectar con el servidor", controller.ModelState[string.Empty]!.Errors.Single().ErrorMessage);
    }

    [Fact]
    public async Task Login_RespuestaValida_CreaSesionYRedirigeInicio()
    {
        // Arrange: se simula una respuesta válida de la API y el servicio de cookies.
        var respuesta = JsonContent.Create(new LoginResponse(7, "usuario", "Usuario Prueba", "Usuario", "jwt-prueba"));
        var autenticacionMock = new Mock<IAuthenticationService>();
        ClaimsPrincipal? sesionCreada = null;
        autenticacionMock
            .Setup(servicio => servicio.SignInAsync(
                It.IsAny<HttpContext>(),
                CookieAuthenticationDefaults.AuthenticationScheme,
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties?>()))
            .Callback<HttpContext, string?, ClaimsPrincipal, AuthenticationProperties?>((_, _, principal, _) => sesionCreada = principal)
            .Returns(Task.CompletedTask);

        var controller = CrearControlador(new HttpResponseMessage(HttpStatusCode.OK) { Content = respuesta }, autenticacionMock.Object);

        // Act: se inicia sesión con una respuesta correcta y sin una URL local de retorno.
        var result = await controller.Login(new LoginViewModel
        {
            Username = "usuario",
            Password = "clave"
        }, returnUrl: "https://sitio-externo.example");

        // Assert: se crea la cookie con el token y se evita redirigir fuera del sitio.
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
        Assert.Equal("jwt-prueba", sesionCreada!.FindFirst("AccessToken")!.Value);
        autenticacionMock.Verify(servicio => servicio.SignInAsync(
            It.IsAny<HttpContext>(),
            CookieAuthenticationDefaults.AuthenticationScheme,
            It.IsAny<ClaimsPrincipal>(),
            It.IsAny<AuthenticationProperties?>()), Times.Once);
    }

    private static AccountController CrearControlador(object respuesta, IAuthenticationService? autenticacion = null)
    {
        var handler = new RespuestaHttpFalsa(respuesta);
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://webapi-pruebas/") };

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(factory => factory.CreateClient("WebAPI")).Returns(client);

        var serviciosMock = new Mock<IServiceProvider>();
        serviciosMock
            .Setup(servicios => servicios.GetService(typeof(IAuthenticationService)))
            .Returns(autenticacion);

        var urlMock = new Mock<IUrlHelper>();
        urlMock.Setup(url => url.IsLocalUrl(It.IsAny<string?>())).Returns(false);

        var httpContext = new DefaultHttpContext { RequestServices = serviciosMock.Object };
        var tempDataMock = new Mock<ITempDataProvider>();
        tempDataMock
            .Setup(proveedor => proveedor.LoadTempData(It.IsAny<HttpContext>()))
            .Returns(new Dictionary<string, object>());

        return new AccountController(factoryMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            Url = urlMock.Object,
            TempData = new TempDataDictionary(httpContext, tempDataMock.Object)
        };
    }

    private sealed class RespuestaHttpFalsa(object respuesta) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => respuesta switch
            {
                HttpRequestException error => Task.FromException<HttpResponseMessage>(error),
                HttpResponseMessage response => Task.FromResult(response),
                _ => throw new InvalidOperationException("Respuesta de prueba no válida.")
            };
    }
}
