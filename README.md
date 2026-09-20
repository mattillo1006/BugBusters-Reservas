# BugBusters-Reservas

Sistema de Reservas de Laboratorios — Módulo de Autenticación (Login)

Este repositorio contiene la primera entrega del proyecto **BugBusters-Reservas**, un sistema para la gestión de reservas de laboratorios. Esta primera parte esta centrada en la funcionalidad del **inicio de sesión (login)**, sobre la cual se construirán las siguientes historias de usuario (gestión de laboratorios, reservas y cancelaciones).

## Tabla de contenido

- [i. Definición de hecho](#i-definición-de-hecho)
- [ii. Notas de la solución](#ii-notas-de-la-solución)
- [iii. Implementación](#iii-implementación)
- [iv. Pruebas](#iv-pruebas)

---

## i. Definición de hecho

Para considerar terminada (Done) la funcionalidad de **Login (AB#1)**, se deben cumplir los siguientes criterios:

- Existe un modelo de datos (`Usuario`, `Rol`) que representa la información necesaria para autenticar a un usuario.
- La base de datos `ReservasLaboratorios` está creada mediante script SQL versionado (`database/01_create_database.sql`), incluyendo las tablas `Roles` y `Usuarios` con sus restricciones (llaves primarias, foráneas, valores únicos y por defecto).
- El proyecto `WebAPI` se conecta correctamente a la base de datos SQL Server mediante Entity Framework Core (`ReservasDBContext`).
- Existe un endpoint REST (`POST /api/Auth/login`) capaz de:
  - Validar que el usuario y la contraseña no vengan vacíos.
  - Verificar las credenciales contra la base de datos.
  - Verificar que el usuario esté activo (`Activo = true`).
  - Retornar la información básica del usuario autenticado (id, nombre de usuario, nombre completo y rol) cuando el login es correcto.
- El endpoint diferencia y responde correctamente los distintos escenarios (petición inválida, credenciales incorrectas, usuario inactivo, login exitoso) con los códigos de estado HTTP adecuados.
- Existen DTOs (`LoginRequest`, `LoginResponse`) que desacoplan el contrato de la API del modelo de datos interno.
- El proyecto compila y expone documentación interactiva de la API (Scalar/OpenAPI) en ambiente de desarrollo.
- Se documentaron y ejecutaron manualmente al menos tres casos de prueba del login: uno positivo, uno neutro y uno negativo (ver sección [iv. Pruebas](#iv-pruebas)).
- Se conecta y se utiliza un API para realizar el proceso de autenticación, aun que por el momento no se utiliza el JWT para un token.

## ii. Notas de la solución

### Arquitectura general

El proyecto está compuesto por **dos aplicaciones .NET 10** dentro de una misma solución (`SistemaDeReservas.slnx`), además también se incluyen lo relacionado a la base de datos.

```
BugBusters-Reservas/
├── WebAPI/                     # Backend - API REST (ASP.NET Core Web API
│   ├── Controllers/
│   │   └── AuthController.cs   # Controlador Principal de mano del EndPoint Auth
│   ├── DTOs/
│   │   ├── LoginRequest.cs
│   │   └── LoginResponse.cs
│   ├── Models/
│   │   ├── Usuario.cs
│   │   └── Rol.cs
│   ├── Data/
│   │   └── ReservasDBContext.cs
│   ├── Program.cs
│   └── appsettings.json
├── SistemaDeReservas/           # Frontend - ASP.NET Core MVC
│   ├── Controllers/HomeController.cs
│   ├── Views/...                # Estas serian los html y css que se utilizan
│   └── Program.cs
├── database/
│   └── 01_create_database.sql   # Script de creación y datos semilla
└── README.md
```

- **`WebAPI`**: expone los servicios vía **API REST**, siguiendo el patrón Controller → DTO → DbContext → Modelo. Es el proyecto responsable de la lógica de autenticación.
- **`SistemaDeReservas`**: proyecto ASP.NET Core MVC que en esta entrega corresponde únicamente al *scaffolding* generado por la plantilla (Home/Privacy/Error). Está pensado como el cliente web que en próximas iteraciones consumirá la `WebAPI` (incluyendo la pantalla de login).
- Ambos proyectos están referenciados en el mismo `.slnx`, lo que permite ejecutarlos y depurarlos desde una sola solución.

### Stack tecnológico

| Componente        | Tecnología                                   |
|-------------------|-----------------------------------------------|
| Framework         | .NET 10 (`net10.0`)                           |
| API               | ASP.NET Core Web API                          |
| Frontend          | ASP.NET Core MVC (Razor Views)                |
| ORM               | Entity Framework Core 10 (`Microsoft.EntityFrameworkCore.SqlServer`) |
| Base de datos     | SQL Server                                     |
| Documentación API | `Microsoft.AspNetCore.OpenApi` + `Scalar.AspNetCore` (UI en `/scalar`) |

### Decisiones de diseño y deuda técnica conocida

- **Contraseñas en texto plano**: en esta primera entrega, `Usuario.PasswordHash` se compara directamente contra el valor enviado (`usuario.PasswordHash != request.Password`), y el script SQL inserta las contraseñas semilla en texto plano (`Laboratorios123#`). Esto es aceptable únicamente como punto de partida funcional; **antes de un ambiente productivo se debe reemplazar por un hash seguro** (por ejemplo, BCrypt o el hasher de ASP.NET Core Identity).
- **Sin emisión de token de sesión**: el login actual retorna los datos del usuario, pero no genera un JWT ni cookie de sesión. Esto se considera parte del alcance de una historia futura (autorización/roles sobre los demás endpoints).
- **DTOs separados del modelo**: se usan `LoginRequest`/`LoginResponse` en lugar de exponer directamente las entidades de EF Core, evitando filtrar campos sensibles (como la contraseña) en la respuesta.

## iii. Implementación

### Requisitos previos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- SQL Server (local, contenedor Docker o instancia remota)
- (Opcional) Visual Studio 2022+ o VS Code con la extensión de C#

### 1. Configurar la base de datos

Ejecutar el script `database/01_create_database.sql` contra una instancia de SQL Server.
El script:
- Crea la base de datos `ReservasLaboratorios` (elimina la existente si ya existía).
- Crea las tablas `Roles`, `Usuarios`, `Laboratorios`, `Reservas` y `Cancelaciones` con sus llaves y restricciones.
- Inserta datos semilla, incluyendo 4 usuarios de prueba (`Admi`, `user01`, `user02`, `user03`), todos con la contraseña `Laboratorios123#`.

### 2. Configurar la cadena de conexión

En `WebAPI/appsettings.json` ajustar la cadena `ConnectionStrings:Default` con el servidor, usuario y contraseña correctos:

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=ReservasLaboratorios;User Id=sa;Password=<tu_password>;Trusted_Connection=False;TrustServerCertificate=True;"
  }
}
```

### 3. Restaurar dependencias y ejecutar la API

```bash
cd WebAPI
dotnet restore
dotnet run
```

Por defecto, el proyecto levanta en `http://localhost:5068` (ver `Properties/launchSettings.json`). En ambiente de desarrollo, se puede explorar y probar la API desde la interfaz de **Scalar**, disponible en:

```
http://localhost:5068/scalar
```

### 4. Endpoint disponible

| Método | Ruta                | Descripción                          |
|--------|---------------------|---------------------------------------|
| POST   | `/api/Auth/login`   | Autentica un usuario y devuelve sus datos básicos |

Ejemplo de petición:

```http
POST /api/Auth/login
Content-Type: application/json
{
  "nombreUsuario": "user01",
  "password": "Laboratorios123#"
}
```

Ejemplo de respuesta exitosa (`200 OK`):

```json
{
  "usuarioId": 2,
  "nombreUsuario": "user01",
  "nombreCompleto": "Matthew",
  "rol": "Usuario"
}
```

### 5. Ejecutar el frontend
Gracias a que se esta trabajando en Visual Studio, se puede lanzar el frontend por aparte del API, para poder visualizar el sitio web, pero teniendo en cuenta que si no se lanza de la mano del API, no habra un carga exitosa del login, ya que no se podra hacer la consulta al endpoint requerido

### Nota de despliegue

- Al tratarse de 2 proyectos dentro de una solución, se debe crear un nuevo perfil de lanzamiento, con esto se refiere a que en Visual Studio, se debe configurar para que cuando se inicie la solución, los 2 proyectos sean lanzados en conjunto, esto porque sino, habrán problemas con la conexión al API por parte del Frontend.

## iv. Pruebas

En este apartado se presentaran diferentes pruebas que se realizaron en el API como del proyecto en conjunto (API + Frontend).

### Caso 1 — Positivo: credenciales válidas de un usuario activo

| Campo                  | Detalle                                                                                                         |
| ---------------------- | --------------------------------------------------------------------------------------------------------------- |
| **Objetivo**           | Verificar que un usuario activo con credenciales correctas pueda iniciar sesión.                                |
| **Precondición**       | Existe el usuario `user01` con contraseña `Laboratorios123#` y `Activo = 1`.                                    |
| **Entrada**            | `{ "nombreUsuario": "user01", "password": "Laboratorios123#" }`                                                 |
| **Resultado esperado** | `200 OK` con un `LoginResponse` que incluye `usuarioId`, `nombreUsuario`, `nombreCompleto` y `rol = "Usuario"`. |
| **Resultado obtenido** | Coincide con lo esperado.                                                                                       |
### Caso 2 — Positivo desde el Frontend: credenciales válidas de un usuario activo

| Campo                  | Detalle                                                                            |
| ---------------------- | ---------------------------------------------------------------------------------- |
| **Objetivo**           | Verificar que un usuario activo con credenciales correctas pueda iniciar sesión.   |
| **Precondición**       | Existe el usuario `user01` con contraseña `Laboratorios123#` y `Activo = 1`.       |
| **Entrada**            | Username: `user01`<br>Contraseña: `Laboratorios123#`                               |
| **Resultado esperado** | El usuario es reenviado a la "pagina" de inicio la cual será a futuro actualizada. |
| **Resultado obtenido** | Coincide con lo esperado.                                                          |


### Caso 3 — Neutro: campos vacíos o incompletos en la solicitud

| Campo                  | Detalle                                                                                                                                   |
| ---------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| **Objetivo**           | Verificar el comportamiento de la API ante una solicitud mal formada (sin credenciales), independientemente de si el usuario existe o no. |
| **Precondición**       | Ninguna relacionada a datos de usuario; se prueba la validación de entrada.                                                               |
| **Entrada**            | `{ "nombreUsuario": "", "password": "" }`                                                                                                 |
| **Resultado esperado** | `400 Bad Request` con el mensaje `"Usuario y contraseña son obligatorios."`, sin consultar la base de datos.                              |
| **Resultado obtenido** | Coincide con lo esperado.                                                                                                                 |
### Caso 4 — Neutro desde el Frontend: campos vacíos o incompletos en la solicitud

| Campo                  | Detalle                                                                                                                                   |
| ---------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| **Objetivo**           | Verificar el comportamiento de la pagina web ante una solicitud mal formada (sin credenciales), independientemente de si el usuario existe o no. |
| **Precondición**       | No se debe escribir nada en los campos de username y contraseña.                                                                          |
| **Entrada**            | Usuario: ` `<br>Contraseña: ` `                                                                                                           |
| **Resultado esperado** | El sitio indica en cada uno de los campos un mensaje indicando que ese espacio es obligatorio.                                            |
| **Resultado obtenido** | Coincide con lo esperado.                                                                                                                 |

### Caso 5 — Negativo: contraseña incorrecta o usuario inactivo

| Campo                  | Detalle                                                                                                                                                                   |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Objetivo**           | Verificar que el sistema rechace el acceso cuando las credenciales no son válidas o el usuario está deshabilitado.                                                        |
| **Precondición**       | Existe el usuario `user01`, pero se envía una contraseña incorrecta. (Escenario alterno: un usuario con `Activo = 0`.)                                                    |
| **Entrada**            | `{ "nombreUsuario": "user01", "password": "ContraseñaIncorrecta" }`                                                                                                       |
| **Resultado esperado** | `401 Unauthorized` con el mensaje `"Credenciales incorrectas."` En el escenario alterno de usuario inactivo, `401 Unauthorized` con `"El usuario se encuentra inactivo."` |
| **Resultado obtenido** | Coincide con lo esperado.                                                                                                                                                 |
### Caso 6 - Negativo desde el Front-End
| Campo                  | Detalle                                                                                                                     |
| ---------------------- | --------------------------------------------------------------------------------------------------------------------------- |
| **Objetivo**           | Verificar que el sistema rechace el acceso cuando las credenciales no son válidas o el usuario está deshabilitado.          |
| **Precondición**       | Existe el usuario `user01`, pero se envía una contraseña incorrecta.                                                        |
| **Entrada**            | Username: `user01`<br>Contraseña: `cualquier cosa menos una correcta`                                                       |
| **Resultado esperado** | La pagina, por el momento tira el mensaje de:<br>"Usuario o contraseña incorrectos" y tiene que volver a ingresar los datos |
| **Resultado obtenido** | Coincide con lo esperado.                                                                                                   |

---

> Nota: Estas pruebas se realizaron tanto desde la interfaz del API como desde el propio Frontend, para que así se pueda confirmar la conexión entre el front y el API, 
