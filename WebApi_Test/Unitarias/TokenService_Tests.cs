using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Moq;
using WebAPI.Models;
using WebAPI.Services;

namespace WebApi_Test.Pruebas_Unitarias;

public class TokenService_Tests
{
    [Fact]
    public void GenerarToken_ConfiguracionValida_IncluyeClaimsDelUsuario()
    {
        // Arrange: se simula la configuración JWT y un usuario autenticable.
        var service = CrearServicio(expiracion: "30");
        var usuario = CrearUsuario();

        // Act: se genera y lee el token sin validar su firma todavía.
        var tokenTexto = service.GenerarToken(usuario);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenTexto);

        // Assert: el token conserva la identidad y el rol del usuario.
        Assert.Equal("Pruebas", token.Issuer);
        Assert.Equal("Pruebas", token.Audiences.Single());
        Assert.Equal(usuario.UsuarioId.ToString(), token.Claims.Single(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal(usuario.NombreUsuario, token.Claims.Single(c => c.Type == ClaimTypes.Name).Value);
        Assert.Equal(usuario.NombreCompleto, token.Claims.Single(c => c.Type == ClaimTypes.GivenName).Value);
        Assert.Equal("Administrador", token.Claims.Single(c => c.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public void GenerarToken_ExpiracionConfigurada_RespetaMinutosIndicados()
    {
        // Arrange: se indica una expiración de treinta minutos mediante el mock.
        var service = CrearServicio(expiracion: "30");
        var antesDeGenerar = DateTime.UtcNow;

        // Act: se genera el token para un usuario válido.
        var tokenTexto = service.GenerarToken(CrearUsuario());
        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenTexto);

        // Assert: la fecha de expiración queda aproximadamente treinta minutos después.
        Assert.InRange(token.ValidTo, antesDeGenerar.AddMinutes(29), antesDeGenerar.AddMinutes(31));
    }

    [Fact]
    public void GenerarToken_SinClave_LanzaErrorDeConfiguracion()
    {
        // Arrange: se simula una sección Jwt que no contiene la clave de firma.
        var service = CrearServicio(clave: null);

        // Act y Assert: el servicio informa una configuración incompleta.
        var error = Assert.Throws<InvalidOperationException>(() => service.GenerarToken(CrearUsuario()));
        Assert.Contains("Key", error.Message);
    }

    private static TokenService CrearServicio(string? clave = "ClaveDePrueba_SuficientementeLarga_123456789", string? expiracion = "60")
    {
        var configuracionMock = new Mock<IConfiguration>();
        var jwtMock = new Mock<IConfigurationSection>();
        var expiracionMock = new Mock<IConfigurationSection>();

        configuracionMock
            .Setup(configuracion => configuracion.GetSection("Jwt"))
            .Returns(jwtMock.Object);

        jwtMock
            .Setup(seccion => seccion[It.IsAny<string>()])
            .Returns((string claveConfiguracion) => claveConfiguracion switch
            {
                "Key" => clave,
                "Issuer" => "Pruebas",
                "Audience" => "Pruebas",
                _ => null
            });

        jwtMock
            .Setup(seccion => seccion.GetSection("ExpiresInMinutes"))
            .Returns(expiracionMock.Object);
        expiracionMock.SetupGet(seccion => seccion.Value).Returns(expiracion);

        return new TokenService(configuracionMock.Object);
    }

    private static Usuario CrearUsuario() => new()
    {
        UsuarioId = 7,
        NombreUsuario = "usuario.prueba",
        NombreCompleto = "Usuario de Prueba",
        Rol = new Rol { NombreRol = "Administrador" }
    };
}
