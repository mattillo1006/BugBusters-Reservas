using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WebAPI;
using WebAPI.Data;
using WebAPI.Services;
using WebAPI.DTOs;
using WebAPI.Models;
using BCrypt.Net;
using Xunit;

namespace WebApi_Test
{
    // Las pruebas en este caso estaran hechas 
    // en base al controlador encargado de manejar
    // el login de la pagina (AuthController - WebAPI)
    public class AuthController_Tests
    {
        [Fact]
        public async Task LoginTest_HappyCase()
        {
            // Arrange: Se prepara la solicitud y el controlador que se necesitan
            var options = new DbContextOptionsBuilder<ReservasDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new ReservasDBContext(options);

            var rol = new Rol
            {
                NombreRol = "Administrador",
                Descripcion = "Admin para pruebas"
            };

            context.Roles.Add(rol);
            await context.SaveChangesAsync();

            var usuario = new Usuario
            {
                NombreUsuario = "LabAdmi",
                NombreCompleto = "Administrador de prueba",
                Correo = "LabAdmi@gmail.com",
                Contraseña = BCrypt.Net.BCrypt.HashPassword("clave123"),
                Activo = true,
                FechaRegistro = DateTime.Now,
                RolId = rol.RolId,
                Rol = rol
            };

            context.Usuarios.Add(usuario);
            await context.SaveChangesAsync();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "ClavedePrueba_SuficientementeLarga_123456789",
                    ["Jwt:Issuer"] = "Pruebas",
                    ["Jwt:Audience"] = "Pruebas"
                })
                .Build();
            var tokenService = new TokenService(configuration);

            var controller = new AuthController(context, tokenService);

            var request = new LoginRequest
            {
                NombreUsuario = "LabAdmi",
                Password = "clave123"
            };

            // Act: Se ejecuta la consulta (Login)

            var result = await controller.Login(request);

            // Asser: Se comprueba que este devuelve lo esperado:
            // Codigo 200 - Solicitud Correcta

            var okResult = Assert.IsType<OkObjectResult>(result);

            var response = Assert.IsType<LoginResponse>(okResult.Value);

            Assert.Equal("LabAdmi", response.NombreUsuario);
            Assert.Equal("Administrador", response.Rol);
            Assert.False(string.IsNullOrWhiteSpace(response.Token));


        }
        [Fact]
        public async Task LoginTest_EmptyName()
        {
            // Arrange: Se prepara la solicitud y el controlador que se necesitan
            var options = new DbContextOptionsBuilder<ReservasDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new ReservasDBContext(options);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "ClavedePrueba_SuficientementeLarga_123456789",
                    ["Jwt:Issuer"] = "Pruebas",
                    ["Jwt:Audience"] = "Pruebas"
                })
                .Build();
            var tokenService = new TokenService(configuration);

            var controller = new AuthController(context, tokenService);

            var request = new LoginRequest
            {
                NombreUsuario = "",
                Password = "clave123"
            };

            // Act: Se ejecuta la consulta (Login)

            var result = await controller.Login(request);

            // Asser: Se comprueba que este devuelve lo esperado:
            // BadRequest 400 - Esto por falta de datos en la solicitud

            var SemiBadResult = Assert.IsType<BadRequestObjectResult>(result);

            Assert.Equal(400, SemiBadResult.StatusCode);


        }
        [Fact]
        public async Task LoginTest_EmptyPassword()
        {
            // Arrange: Se prepara la solicitud y el controlador que se 
            // necesitan
            var options = new DbContextOptionsBuilder<ReservasDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new ReservasDBContext(options);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "ClavedePrueba_SuficientementeLarga_123456789",
                    ["Jwt:Issuer"] = "Pruebas",
                    ["Jwt:Audience"] = "Pruebas"
                })
                .Build();
            var tokenService = new TokenService(configuration);

            var controller = new AuthController(context, tokenService);

            var request = new LoginRequest
            {
                NombreUsuario = "LabAdmi",
                Password = ""
            };

            // Act: Se ejecuta la consulta (Login)

            var result = await controller.Login(request);

            // Asser: Se comprueba que este devuelve lo esperado:
            // BadRequest 400 - Esto por falta de datos en la solicitud

            var SemiBadResult = Assert.IsType<BadRequestObjectResult>(result);

            Assert.Equal(400, SemiBadResult.StatusCode);

        }
        [Fact]
        public async Task LoginTest_WrongCredentials()
        {
            // Arrange: Se prepara la solicitud y el controlador que se necesitan
            var options = new DbContextOptionsBuilder<ReservasDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new ReservasDBContext(options);

            var rol = new Rol
            {
                NombreRol = "Administrador",
                Descripcion = "Admin para pruebas"
            };

            context.Roles.Add(rol);
            await context.SaveChangesAsync();

            var usuario = new Usuario
            {
                NombreUsuario = "LabAdmi",
                NombreCompleto = "Administrador de prueba",
                Correo = "LabAdmi@gmail.com",
                Contraseña = BCrypt.Net.BCrypt.HashPassword("clave123"),
                Activo = true,
                FechaRegistro = DateTime.Now,
                RolId = rol.RolId,
                Rol = rol
            };

            context.Usuarios.Add(usuario);
            await context.SaveChangesAsync();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "ClavedePrueba_SuficientementeLarga_123456789",
                    ["Jwt:Issuer"] = "Pruebas",
                    ["Jwt:Audience"] = "Pruebas"
                })
                .Build();
            var tokenService = new TokenService(configuration);

            var controller = new AuthController(context, tokenService);

            // Punto de la Prueba: En el request que se ingresa el usuario correcto pero la contra
            // erronea
            var request = new LoginRequest
            {

                NombreUsuario = "LabAdmi",
                Password = "claveMala>:)"
            };

            // Act: Se ejecuta la consulta (Login)

            var result = await controller.Login(request);

            // Asser: Se comprueba que este devuelve lo esperado:
            // Codigo 4001 - Solicitud Incorrecta por Credenciales Invalidas

            var BadResult = Assert.IsType<UnauthorizedObjectResult>(result);

            Assert.Equal(401, BadResult.StatusCode);


        }
        [Fact]
        public async Task LoginTest_InactiveUser()
        {
            // Arrange: Se prepara la solicitud y el controlador que se necesitan
            var options = new DbContextOptionsBuilder<ReservasDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new ReservasDBContext(options);

            var rol = new Rol
            {
                NombreRol = "Usuario",
                Descripcion = "Usuario para pruebas"
            };

            context.Roles.Add(rol);
            await context.SaveChangesAsync();

            var usuario = new Usuario
            {
                NombreUsuario = "LabUser",
                NombreCompleto = "User de prueba",
                Correo = "LabUser@gmail.com",
                Contraseña = BCrypt.Net.BCrypt.HashPassword("clave123"),
                // Punto de la prueba
                Activo = false,
                FechaRegistro = DateTime.Now,
                RolId = rol.RolId,
                Rol = rol
            };

            context.Usuarios.Add(usuario);
            await context.SaveChangesAsync();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "ClavedePrueba_SuficientementeLarga_123456789",
                    ["Jwt:Issuer"] = "Pruebas",
                    ["Jwt:Audience"] = "Pruebas"
                })
                .Build();
            var tokenService = new TokenService(configuration);

            var controller = new AuthController(context, tokenService);

            var request = new LoginRequest
            {
                NombreUsuario = "LabUser",
                Password = "clave123"
            };

            // Act: Se ejecuta la consulta (Login)

            var result = await controller.Login(request);

            // Asser: Se comprueba que este devuelve lo esperado:
            // Codigo 401 - Solicitud Incorrecta por que el usuario esta Inactivo

            var BadResult = Assert.IsType<UnauthorizedObjectResult>(result);

            Assert.Equal(401, BadResult.StatusCode);
        }
        [Fact]
        public async Task LoginTest_NonExistingUser()
        {
            // Arrange: Se prepara la solicitud y el controlador que se necesitan
            var options = new DbContextOptionsBuilder<ReservasDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new ReservasDBContext(options);

            var rol = new Rol
            {
                NombreRol = "Usuario",
                Descripcion = "Usuario para pruebas"
            };

            context.Roles.Add(rol);
            await context.SaveChangesAsync();

            var usuario = new Usuario
            {
                NombreUsuario = "LabUser",
                NombreCompleto = "User de prueba",
                Correo = "LabUser@gmail.com",
                Contraseña = BCrypt.Net.BCrypt.HashPassword("clave123"),
                // Punto de la prueba
                Activo = true,
                FechaRegistro = DateTime.Now,
                RolId = rol.RolId,
                Rol = rol
            };

            context.Usuarios.Add(usuario);
            await context.SaveChangesAsync();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "ClavedePrueba_SuficientementeLarga_123456789",
                    ["Jwt:Issuer"] = "Pruebas",
                    ["Jwt:Audience"] = "Pruebas"
                })
                .Build();
            var tokenService = new TokenService(configuration);

            var controller = new AuthController(context, tokenService);

            var request = new LoginRequest
            {
                NombreUsuario = "LabTeacher",
                Password = "Lab123"
            };

            // Act: Se ejecuta la consulta (Login)

            var result = await controller.Login(request);

            // Asser: Se comprueba que este devuelve lo esperado:
            // Codigo 401 - Solicitud de un usuario que no existe

            var BadResult = Assert.IsType<UnauthorizedObjectResult>(result);

            Assert.Equal(401, BadResult.StatusCode);


        }
        // WO = With Only
        [Fact]
        public async Task LoginTest_UserWOSpaces()
        {
            // Arrange: Se prepara la solicitud y el controlador que se necesitan
            var options = new DbContextOptionsBuilder<ReservasDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new ReservasDBContext(options);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "ClavedePrueba_SuficientementeLarga_123456789",
                    ["Jwt:Issuer"] = "Pruebas",
                    ["Jwt:Audience"] = "Pruebas"
                })
                .Build();
            var tokenService = new TokenService(configuration);

            var controller = new AuthController(context, tokenService);

            var request = new LoginRequest
            {
                NombreUsuario = "   ",
                Password = "Lab123"
            };

            // Act: Se ejecuta la consulta (Login)

            var result = await controller.Login(request);

            // Asser: Se comprueba que este devuelve lo esperado:
            // Codigo 401 - Solicitud con formato equivocado - User solo con espacios

            var BadResult = Assert.IsType<BadRequestObjectResult>(result);

            Assert.Equal(400, BadResult.StatusCode);


        }
        // WO = With Only
        [Fact]
        public async Task LoginTest_PasswordWOSpaces()
        {
            // Arrange: Se prepara la solicitud y el controlador que se necesitan
            var options = new DbContextOptionsBuilder<ReservasDBContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var context = new ReservasDBContext(options);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "ClavedePrueba_SuficientementeLarga_123456789",
                    ["Jwt:Issuer"] = "Pruebas",
                    ["Jwt:Audience"] = "Pruebas"
                })
                .Build();
            var tokenService = new TokenService(configuration);

            var controller = new AuthController(context, tokenService);

            var request = new LoginRequest
            {
                NombreUsuario = "LabUser",
                Password = "    "
            };

            // Act: Se ejecuta la consulta (Login)

            var result = await controller.Login(request);

            // Asser: Se comprueba que este devuelve lo esperado:
            // Codigo 401 - Solicitud con formato equivocado - Password solo con espacios

            var BadResult = Assert.IsType<BadRequestObjectResult>(result);

            Assert.Equal(400, BadResult.StatusCode);


        }
    }
}
