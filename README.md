# BugBusters-Reservas

Sistema de Reservas de Laboratorios.

Este repositorio contiene el proyecto **BugBusters-Reservas**, un sistema para la gestión de reservas de laboratorios. La primera entrega (Sprint 1) se centró en el módulo de autenticación (login), sobre el cual se construyeron las historias siguientes: visualización de laboratorios y consulta de disponibilidad en el Sprint 2 y, en el Sprint 3, el registro de reservas con la validación de su horario y el control de conflictos entre reservas (a futuro, cancelaciones). Hasta el momento se han entregado las siguientes historias de usuario:

| Historia | Descripción                                | Estado                                  |
| -------- | ------------------------------------------ | --------------------------------------- |
| **HU1**  | Inicio de sesión (login)                   | API + Frontend                          |
| **HU2**  | Visualización de laboratorios              | API + Frontend |
| **HU3**  | Consulta de disponibilidad de laboratorios | API + Frontend |
| **HU4**  | Registrar una reserva                      | API + Frontend                          |
| **HU5**  | Validar horario de reserva                 | API + Frontend                          |
| **HU6**  | Evitar conflictos entre reservas           | API + Frontend                          |

Las secciones que aplican a todo el proyecto (arquitectura, tecnologías, configuración, ejecución y seguridad) se encuentran agrupadas en [Información general del proyecto](#información-general-del-proyecto). Cada historia de usuario solo documenta lo que le es propio.

## Tabla de contenido

- [Información general del proyecto](#información-general-del-proyecto)
  - [Arquitectura](#arquitectura)
  - [Stack tecnológico](#stack-tecnológico)
  - [Requisitos previos](#requisitos-previos)
  - [Configuración inicial](#configuración-inicial)
  - [Ejecución](#ejecución)
  - [Autenticación y autorización (JWT)](#autenticación-y-autorización-jwt)
  - [Resumen de endpoints](#resumen-de-endpoints)
  - [Decisiones de diseño y deuda técnica](#decisiones-de-diseño-y-deuda-técnica)
  - [Evolución por sprint](#evolución-por-sprint)
- [HU1 - Inicio de sesión](#hu1---inicio-de-sesión)
  - [i. Definición de hecho (HU1)](#i-definición-de-hecho-hu1)
  - [ii. Notas de la solución (HU1)](#ii-notas-de-la-solución-hu1)
  - [iii. Implementación (HU1)](#iii-implementación-hu1)
  - [iv. Pruebas (HU1)](#iv-pruebas-hu1)
- [HU2 y HU3 - Visualización y consulta de disponibilidad de laboratorios](#hu2-y-hu3---visualización-y-consulta-de-disponibilidad-de-laboratorios)
  - [i. Definición de hecho (HU2 y HU3)](#i-definición-de-hecho-hu2-y-hu3)
  - [ii. Notas de la solución (HU2 y HU3)](#ii-notas-de-la-solución-hu2-y-hu3)
  - [iii. Implementación (HU2 y HU3)](#iii-implementación-hu2-y-hu3)
  - [iv. Pruebas (HU2 y HU3)](#iv-pruebas-hu2-y-hu3)
- [HU4, HU5 y HU6 - Registro de reservas](#hu4-hu5-y-hu6---registro-de-reservas)
  - [i. Definición de hecho (HU4, HU5 y HU6)](#i-definición-de-hecho-hu4-hu5-y-hu6)
  - [ii. Notas de la solución (HU4, HU5 y HU6)](#ii-notas-de-la-solución-hu4-hu5-y-hu6)
  - [iii. Implementación (HU4, HU5 y HU6)](#iii-implementación-hu4-hu5-y-hu6)
  - [iv. Pruebas (HU4, HU5 y HU6)](#iv-pruebas-hu4-hu5-y-hu6)

---

# Información general del proyecto


## Arquitectura

El proyecto está compuesto por **dos aplicaciones .NET 10** que se ejecutan en conjunto, además del script de base de datos.

```
BugBusters-Reservas/
├── WebAPI/                          # Backend - API REST (ASP.NET Core Web API)
│   ├── Controllers/
│   │   ├── AuthController.cs        # Controlador del endpoint Auth (login)
│   │   ├── LaboratoriosController.cs # Laboratorios y disponibilidad (HU2, HU3)
│   │   └── ReservasController.cs    # Registro de reservas y "mis reservas" (HU4, HU5, HU6)
│   ├── DTOs/
│   │   ├── LoginRequest.cs
│   │   ├── LoginResponse.cs
│   │   ├── LaboratorioResponse.cs
│   │   ├── DisponibilidadResponse.cs
│   │   ├── ReservaOcupadaResponse.cs
│   │   ├── CrearReservaRequest.cs
│   │   ├── ReservaResponse.cs
│   │   └── MiReservaResponse.cs
│   ├── Models/
│   │   ├── Usuario.cs
│   │   ├── Rol.cs
│   │   ├── Laboratorio.cs
│   │   └── Reserva.cs
│   ├── Services/
│   │   └── TokenService.cs          # Generación del JWT
│   ├── Data/
│   │   └── ReservasDBContext.cs
│   ├── Program.cs
│   └── appsettings.json
├── SistemaDeReservas/               # Frontend - ASP.NET Core MVC
│   ├── Controllers/
│   │   ├── AccountController.cs     # Login / Logout
│   │   ├── HomeController.cs        # Listado de laboratorios (HU2)
│   │   ├── LaboratoriosController.cs # Tabla de disponibilidad (HU3)
│   │   └── ReservasController.cs    # Registro de reserva (HU4-HU6) y "Mis reservas"
│   ├── Handlers/
│   │   └── JWTAuthHandler.cs        # Agrega el JWT a las llamadas hacia la WebAPI
│   ├── Models/
│   │   ├── LoginViewModel.cs
│   │   ├── Laboratorio.cs
│   │   ├── DisponibilidadViewModel.cs
│   │   └── MisReservasViewModel.cs
│   ├── Views/                       # Vistas Razor (html)
│   │   ├── Account/Login.cshtml
│   │   ├── Home/Index.cshtml
│   │   ├── Laboratorios/Disponibilidad.cshtml
│   │   ├── Reservas/Index.cshtml    # Mis reservas
│   │   └── Shared/_Layout.cshtml
│   ├── wwwroot/
│   │   ├── css/                     # Estilos (login, home, disponibilidad, mis-reservas)
│   │   └── img/                     # Imágenes (laboratorios, logo, login)
│   ├── sistema-reservas-react/      # Cliente React (Vite + Tailwind), base inicial sin pantallas funcionales
│   ├── SistemaDeReservas.slnx
│   ├── SistemaDeReservas.slnLaunch
│   └── Program.cs
├── WebApi_Test/                     # Pruebas unitarias (xUnit + EF Core InMemory)
│   └── AuthController_Tests.cs      # 8 pruebas del login
├── database/
│   └── 01_create_database.sql       # Script de creación y datos semilla
└── README.md
```

- **`WebAPI`**: expone los servicios vía **API REST**, siguiendo el patrón Controller → DTO → DbContext → Modelo. Concentra la lógica de negocio (autenticación, consulta de laboratorios y disponibilidad, y registro de reservas con sus validaciones y control de conflictos).
- **`SistemaDeReservas`**: proyecto ASP.NET Core MVC que actúa como cliente web. Consume la `WebAPI` mediante un `HttpClient` con nombre (`"WebAPI"`) al que se le añade automáticamente el token JWT del usuario autenticado. Incluye las pantallas de inicio de sesión, listado de laboratorios, consulta de disponibilidad (desde donde se registra la reserva) y "Mis reservas".
- **`WebApi_Test`**: proyecto de pruebas unitarias (xUnit) que usa una base de datos en memoria; referencia al proyecto `WebAPI`.
- **Base de datos**: SQL Server, versionada mediante el script `database/01_create_database.sql`.
- Ambos proyectos están referenciados en la misma solución (`SistemaDeReservas.slnx`), lo que permite ejecutarlos y depurarlos desde un solo lugar.

> Este esquema del diseño que presenta el proyecto, se actualizara conforme al avance del mismo.
## Stack tecnológico

| Componente        | Tecnología                                                              |
|-------------------|-------------------------------------------------------------------------|
| Framework         | .NET 10 (`net10.0`)                                                     |
| API               | ASP.NET Core Web API                                                    |
| Frontend          | ASP.NET Core MVC (Razor Views)                                          |
| ORM               | Entity Framework Core 10 (`Microsoft.EntityFrameworkCore.SqlServer`)    |
| Base de datos     | SQL Server                                                              |
| Autenticación     | JWT Bearer (API) + Cookie de autenticación (Frontend)                   |
| Contraseñas       | Hash con BCrypt (`BCrypt.Net-Next`)                                     |
| Documentación API | `Microsoft.AspNetCore.OpenApi` + `Scalar.AspNetCore` (UI en `/scalar`)  |
| Pruebas unitarias | xUnit 2.9 + `Microsoft.EntityFrameworkCore.InMemory`                    |

## Requisitos previos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- SQL Server (local, contenedor Docker o instancia remota)
- (Opcional) Visual Studio 2022+ o VS Code con la extensión de C#

## Configuración inicial

### 1. Base de datos

Ejecutar el script `database/01_create_database.sql` contra una instancia de SQL Server. El script:

- Crea la base de datos `ReservasLaboratorios` (elimina la existente si ya existía).
- Crea las tablas `Roles`, `Usuarios`, `Laboratorios`, `Reservas` y `Cancelaciones` con sus llaves y restricciones.
- Inserta datos semilla:
  - 4 usuarios de prueba (`Admi`, `user01`, `user02`, `user03`), todos con la contraseña `Laboratorios123#` (guardada como hash BCrypt).
  - 3 laboratorios de ejemplo (ver tabla siguiente).
  - 3 reservas y 1 cancelación de ejemplo. La reserva 3 (Lab Computo 1, 2026-10-06, 09:00 – 11:00) permitía ver un bloque `Ocupado` en la tabla de disponibilidad. Las fechas de estas reservas ya pasaron, por lo que no sirven para probar conflictos: las pruebas de HU4, HU5 y HU6 registran sus propias reservas con fechas futuras.

| Id | Laboratorio       | Ubicación            | Capacidad | Estado            | Horario de operación |
|----|-------------------|----------------------|-----------|-------------------|----------------------|
| 1  | Lab Computo 1     | Edificio A - Piso 2  | 30        | `Habilitado`      | 07:00 – 22:00        |
| 2  | Lab Redes         | Edificio B - Piso 1  | 25        | `Habilitado`      | 08:00 – 20:00        |
| 3  | Lab Electrónica   | Edificio C - Piso 3  | 20        | `FueraDeServicio` | 09:00 – 18:00        |

### 2. Cadena de conexión y configuración JWT

En `WebAPI/appsettings.json` ajustar la cadena `ConnectionStrings:Default` con el servidor, usuario y contraseña correctos. La sección `Jwt` define cómo se firma y valida el token:

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=ReservasLaboratorios;User Id=sa;Password=<tu_password>;Trusted_Connection=False;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "<clave_secreta_larga>",
    "Issuer": "BugBusters.SistemaDeReservas.WebAPI",
    "Audience": "BugBusters.SistemaDeReservas.Cliente",
    "ExpiresInMinutes": 60
  }
}
```

En `SistemaDeReservas/appsettings.json`, la clave `ApiBaseUrl` indica dónde está la WebAPI (por defecto `http://localhost:5068/`).

## Ejecución

| Proyecto            | URL por defecto                                  |
|---------------------|--------------------------------------------------|
| `WebAPI`            | `http://localhost:5068`                          |
| `SistemaDeReservas` | `http://localhost:5082` / `https://localhost:7241` |

### API

```bash
cd WebAPI
dotnet restore
dotnet run
```

En ambiente de desarrollo, la API se puede explorar y probar desde la interfaz de **Scalar**:

```
http://localhost:5068/scalar
```

### Frontend

```bash
cd SistemaDeReservas
dotnet run
```

El frontend depende de la API para funcionar (el login consulta el endpoint de autenticación), por lo que **ambos proyectos deben estar en ejecución al mismo tiempo**. Si se trabaja en Visual Studio, el frontend puede lanzarse por aparte del API para visualizar el sitio, pero sin la API en ejecución el login no cargará correctamente porque no podrá consultar el endpoint requerido.

### Nota de despliegue (Visual Studio)

Al tratarse de 2 proyectos dentro de una solución, se deben iniciar en conjunto. El repositorio incluye el archivo `SistemaDeReservas/SistemaDeReservas.slnLaunch` con el perfil **"Perfil de Arranque Simultaneo"**, que lanza `WebAPI` y `SistemaDeReservas` a la vez. Basta con seleccionar ese perfil en Visual Studio antes de ejecutar; de lo contrario, el frontend no podrá conectarse al API.

### Pruebas automatizadas

```bash
cd WebApi_Test
dotnet test
```

El proyecto `WebApi_Test` contiene 8 pruebas del login (`AuthController_Tests`): credenciales válidas, usuario o contraseña vacíos, credenciales incorrectas, usuario inactivo, usuario inexistente y espacios en blanco en usuario y contraseña. No necesitan SQL Server porque usan una base de datos en memoria.

## Autenticación y autorización (JWT)

Es un mecanismo transversal: **todos los endpoints, excepto `POST /api/Auth/login`, requieren un token JWT** (atributo `[Authorize]`).

1. El usuario inicia sesión con `POST /api/Auth/login` y el API responde con sus datos básicos y un `token`.
2. **Frontend**: `AccountController` guarda el token como un claim (`AccessToken`) dentro de la cookie de autenticación. Luego, `JWTAuthHandler` lo lee y lo agrega como encabezado `Authorization: Bearer <token>` en cada llamada hacia la WebAPI.
3. **Pruebas directas contra el API** (Scalar, Postman, etc.): se obtiene el token con el login y se envía manualmente en el encabezado `Authorization: Bearer <token>`. En Scalar se puede configurar en la sección de autenticación de la interfaz.

El token incluye como claims el id, nombre de usuario, nombre completo y rol, y expira según `Jwt:ExpiresInMinutes` (60 minutos). Una petición sin token, o con un token inválido o vencido, recibe `401 Unauthorized`.

En el frontend, la cookie de autenticación es `HttpOnly`, `SameSite=Strict` y expira a los 25 minutos de inactividad (expiración deslizante). El login es la página inicial de la aplicación; si ya existe una sesión activa, abrirlo redirige directamente al inicio. Cuando no hay sesión, o cuando la WebAPI responde `401` (token inválido o vencido), el frontend cierra la sesión y redirige al login.

## Resumen de endpoints

| Método | Ruta                                          | Auth | Historia | Descripción                                             |
|--------|-----------------------------------------------|------|----------|---------------------------------------------------------|
| POST   | `/api/Auth/login`                             | No   | HU1      | Autentica un usuario y devuelve sus datos y el token    |
| GET    | `/api/Laboratorios`                           | Sí   | HU2      | Lista los laboratorios registrados                      |
| GET    | `/api/Laboratorios/{id}`                      | Sí   | HU2      | Obtiene un laboratorio (selección para reservar)        |
| GET    | `/api/Laboratorios/{id}/disponibilidad`       | Sí   | HU3      | Consulta si un laboratorio está disponible en un horario |
| GET    | `/api/Laboratorios/{id}/reservas`             | Sí   | HU3      | Lista las reservas activas de un laboratorio en un rango de fechas (marca los bloques ocupados de la tabla) |
| POST   | `/api/Reservas`                               | Sí   | HU4, HU5, HU6 | Registra una reserva del usuario autenticado, validando su horario y que no tenga conflictos |
| GET    | `/api/Reservas/mias`                          | Sí   | Complementario | Lista las reservas del usuario autenticado (pantalla "Mis reservas") |

## Decisiones de diseño y deuda técnica

Aplican a todo el proyecto:

- **Contraseñas con hash BCrypt**: las contraseñas se almacenan con hash BCrypt (`BCrypt.Net-Next`) y el login las valida con `BCrypt.Net.BCrypt.Verify`; ya no se guardan ni se comparan en texto plano como ocurría en el Sprint 1. Un valor que no tenga formato de hash BCrypt en `Usuarios.Contraseña` hace fallar la verificación, por lo que todo usuario nuevo debe guardarse hasheado.
- **Secretos en el repositorio**: la clave `Jwt:Key` y la contraseña de la base de datos están en `appsettings.json`. Antes de un ambiente productivo deben moverse a *user-secrets*, variables de entorno o un gestor de secretos.
- **DTOs separados del modelo**: se usan DTOs (`LoginRequest`, `LoginResponse`, `LaboratorioResponse`, `DisponibilidadResponse`, `ReservaOcupadaResponse`, `CrearReservaRequest`, `ReservaResponse`, `MiReservaResponse`) en lugar de exponer directamente las entidades de EF Core. Así se evita filtrar campos internos o sensibles (como la contraseña o `Active`) en las respuestas.
- **Reglas de negocio en la API**: las validaciones de las reservas (horario, duración y conflictos) viven en la API; el frontend solo guía al usuario, de modo que cualquier cliente de la API queda sujeto a las mismas reglas.
- **Autorización por rol pendiente**: el token ya transporta el rol del usuario, pero por ahora los endpoints solo exigen estar autenticado. Las restricciones por rol (Administrador / Usuario) quedan para historias futuras.

## Evolución por sprint

Resumen de lo que cambió entre la primera entrega (solo login) y el estado actual del proyecto:

| Tema                       | Sprint 1                                                                 | Sprint 2                                                                                   | Sprint 3                                                                                                   |
|----------------------------|--------------------------------------------------------------------------|--------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------|
| Alcance                    | HU1: login                                                               | HU1 + HU2 (visualizar laboratorios) + HU3 (consultar disponibilidad)                       | HU1 a HU3 + HU4 (registrar reserva) + HU5 (validar horario) + HU6 (evitar conflictos)                      |
| Token de sesión            | El login devolvía solo los datos del usuario; no había JWT ni cookie     | El login devuelve un JWT; el frontend lo guarda en la cookie y lo reenvía a la WebAPI      | Sin cambios; el id del usuario del token se usa como responsable de la reserva                             |
| Protección de endpoints    | Sin autorización sobre los endpoints                                     | Todos los endpoints, excepto `POST /api/Auth/login`, exigen JWT (`[Authorize]`)             | Los nuevos endpoints de reservas también exigen JWT                                                        |
| Contraseñas                | Texto plano (comparación directa y datos semilla en texto plano)         | Hash BCrypt y verificación con `BCrypt.Verify`                                             | Sin cambios                                                                                                |
| Frontend                   | Solo el *scaffolding* de la plantilla MVC (Home/Privacy/Error)           | Login, listado de laboratorios y tabla de disponibilidad; login como página inicial        | La tabla permite elegir un bloque y registrar la reserva; nueva pantalla "Mis reservas"; sesión de 25 minutos |
| Endpoints                  | `POST /api/Auth/login`                                                   | Se agregan `GET /api/Laboratorios`, `/{id}`, `/{id}/disponibilidad` y `/{id}/reservas`      | Se agregan `POST /api/Reservas` y `GET /api/Reservas/mias`                                                 |
| Datos semilla              | Roles y 4 usuarios de prueba                                             | Además laboratorios, reservas y una cancelación de ejemplo                                 | Las pruebas registran sus propias reservas, porque las del script semilla ya quedaron en fechas pasadas    |
| Pruebas                    | Manuales (Scalar y Frontend)                                             | Manuales (Scalar y Frontend)                                                               | Manuales de HU4 a HU6 y proyecto `WebApi_Test` (xUnit) con 8 pruebas del login                             |
| Ejecución en Visual Studio | Se lanzaba el frontend por aparte del API                                | Perfil de arranque simultáneo (`SistemaDeReservas.slnLaunch`)                              | Sin cambios                                                                                                |

---

# HU1 - Inicio de sesión

## i. Definición de hecho (HU1)

Para considerar terminada (Done) la funcionalidad de **Login (AB#1)**, se deben cumplir los siguientes criterios:

- Existe un modelo de datos (`Usuario`, `Rol`) que representa la información necesaria para autenticar a un usuario.
- La base de datos `ReservasLaboratorios` está creada mediante script SQL versionado (`database/01_create_database.sql`), incluyendo las tablas `Roles` y `Usuarios` con sus restricciones (llaves primarias, foráneas, valores únicos y por defecto).
- El proyecto `WebAPI` se conecta correctamente a la base de datos SQL Server mediante Entity Framework Core (`ReservasDBContext`).
- Existe un endpoint REST (`POST /api/Auth/login`) capaz de:
  - Validar que el usuario y la contraseña no vengan vacíos.
  - Verificar las credenciales contra la base de datos (la contraseña enviada se compara contra el hash BCrypt almacenado).
  - Verificar que el usuario esté activo (`Activo = true`).
  - Retornar la información básica del usuario autenticado (id, nombre de usuario, nombre completo y rol) junto con un token JWT cuando el login es correcto.
- El endpoint diferencia y responde correctamente los distintos escenarios (petición inválida, credenciales incorrectas, usuario inactivo, login exitoso) con los códigos de estado HTTP adecuados.
- Existen DTOs (`LoginRequest`, `LoginResponse`) que desacoplan el contrato de la API del modelo de datos interno.
- El proyecto compila y expone documentación interactiva de la API (Scalar/OpenAPI) en ambiente de desarrollo.
- El frontend consume el API para realizar la autenticación, conserva la sesión mediante cookie y reenvía el token JWT en las llamadas posteriores (ver [Autenticación y autorización](#autenticación-y-autorización-jwt)).
- Se documentaron y ejecutaron manualmente al menos tres casos de prueba del login: uno positivo, uno neutro y uno negativo (ver sección [iv. Pruebas (HU1)](#iv-pruebas-hu1)).

## ii. Notas de la solución (HU1)

- **Flujo**: Frontend (`AccountController`) → `POST api/auth/login` → `AuthController` → `ReservasDBContext` → tabla `Usuarios` (con su `Rol`). Si todo es correcto, `TokenService` genera el JWT.
- **Orden de validaciones** del endpoint:

| Orden | Validación                          | Respuesta                                                        |
|-------|-------------------------------------|------------------------------------------------------------------|
| 1     | Usuario o contraseña vacíos         | `400 Bad Request` — `"Usuario y contraseña son obligatorios."`   |
| 2     | Usuario inexistente o contraseña incorrecta | `401 Unauthorized` — `"Credenciales incorrectas."`        |
| 3     | Usuario con `Activo = 0`            | `401 Unauthorized` — `"El usuario se encuentra inactivo."`       |
| 4     | Credenciales válidas                | `200 OK` — `LoginResponse` con `token`                           |

- **Validación en el frontend**: `LoginViewModel` marca usuario y contraseña como obligatorios, de modo que los campos vacíos se rechazan en la pantalla sin llegar al API. Cualquier respuesta no exitosa del API se muestra al usuario como `"Usuario o contraseña incorrectos"`.
- Tras un login correcto, el usuario es redirigido a la página de inicio (`Home/Index`), que desde el Sprint 2 muestra el listado de laboratorios (HU2). La página de inicio exige sesión y, si ya hay una sesión activa, abrir el login redirige directamente al inicio.
- **Mostrar/ocultar contraseña**: el campo de contraseña del login incluye un botón con icono de ojo que permite ver lo digitado y volver a ocultarlo con otro clic.

## iii. Implementación (HU1)

Los pasos para levantar el proyecto se encuentran en [Configuración inicial](#configuración-inicial) y [Ejecución](#ejecución).

### Endpoint

| Método | Ruta              | Descripción                                        |
|--------|-------------------|----------------------------------------------------|
| POST   | `/api/Auth/login` | Autentica un usuario y devuelve sus datos y un JWT |

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
  "rol": "Usuario",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

## iv. Pruebas (HU1)

En este apartado se presentan las pruebas realizadas en el API y en el proyecto en conjunto (API + Frontend).

### Caso 1 — Positivo: credenciales válidas de un usuario activo

| Campo                  | Detalle                                                                                                                      |
| ---------------------- | ---------------------------------------------------------------------------------------------------------------------------- |
| **Objetivo**           | Verificar que un usuario activo con credenciales correctas pueda iniciar sesión.                                             |
| **Precondición**       | Existe el usuario `user01` con contraseña `Laboratorios123#` y `Activo = 1`.                                                 |
| **Entrada**            | `{ "nombreUsuario": "user01", "password": "Laboratorios123#" }`                                                              |
| **Resultado esperado** | `200 OK` con un `LoginResponse` que incluye `usuarioId`, `nombreUsuario`, `nombreCompleto`, `rol = "Usuario"` y `token`.     |
| **Resultado obtenido** | Coincide con lo esperado.                                                                                                    |

### Caso 2 — Positivo desde el Frontend: credenciales válidas de un usuario activo

| Campo                  | Detalle                                                                            |
| ---------------------- | ---------------------------------------------------------------------------------- |
| **Objetivo**           | Verificar que un usuario activo con credenciales correctas pueda iniciar sesión.   |
| **Precondición**       | Existe el usuario `user01` con contraseña `Laboratorios123#` y `Activo = 1`.       |
| **Entrada**            | Username: `user01`<br>Contraseña: `Laboratorios123#`                               |
| **Resultado esperado** | El usuario es reenviado a la página de inicio (a partir del Sprint 2, el listado de laboratorios). |
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

| Campo                  | Detalle                                                                                                                                         |
| ---------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------- |
| **Objetivo**           | Verificar el comportamiento de la página web ante una solicitud mal formada (sin credenciales), independientemente de si el usuario existe o no. |
| **Precondición**       | No se debe escribir nada en los campos de username y contraseña.                                                                                |
| **Entrada**            | Usuario: ` `<br>Contraseña: ` `                                                                                                                 |
| **Resultado esperado** | El sitio indica en cada uno de los campos un mensaje indicando que ese espacio es obligatorio.                                                  |
| **Resultado obtenido** | Coincide con lo esperado.                                                                                                                       |

### Caso 5 — Negativo: contraseña incorrecta o usuario inactivo

| Campo                  | Detalle                                                                                                                                                                   |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Objetivo**           | Verificar que el sistema rechace el acceso cuando las credenciales no son válidas o el usuario está deshabilitado.                                                        |
| **Precondición**       | Existe el usuario `user01`, pero se envía una contraseña incorrecta. (Escenario alterno: un usuario con `Activo = 0`.)                                                    |
| **Entrada**            | `{ "nombreUsuario": "user01", "password": "ContraseñaIncorrecta" }`                                                                                                       |
| **Resultado esperado** | `401 Unauthorized` con el mensaje `"Credenciales incorrectas."` En el escenario alterno de usuario inactivo, `401 Unauthorized` con `"El usuario se encuentra inactivo."` |
| **Resultado obtenido** | Coincide con lo esperado.                                                                                                                                                 |

### Caso 6 — Negativo desde el Frontend: contraseña incorrecta

| Campo                  | Detalle                                                                                                                       |
| ---------------------- | ----------------------------------------------------------------------------------------------------------------------------- |
| **Objetivo**           | Verificar que el sistema rechace el acceso cuando las credenciales no son válidas o el usuario está deshabilitado.            |
| **Precondición**       | Existe el usuario `user01`, pero se envía una contraseña incorrecta.                                                          |
| **Entrada**            | Username: `user01`<br>Contraseña: `cualquier cosa menos una correcta`                                                         |
| **Resultado esperado** | La página muestra el mensaje `"Usuario o contraseña incorrectos"` y el usuario tiene que volver a ingresar los datos.         |
| **Resultado obtenido** | Coincide con lo esperado.                                                                                                     |

> Nota: estas pruebas se realizaron tanto desde la interfaz del API como desde el propio Frontend, para confirmar la conexión entre el front y el API.

---

# HU2 y HU3 - Visualización y consulta de disponibilidad de laboratorios

Ambas historias forman un solo flujo: en **HU2** el usuario ve el listado de laboratorios y selecciona uno; en **HU3** consulta, para ese laboratorio, si está disponible en una fecha y horario. Si lo está, puede continuar con el proceso de reserva (HU4).

## i. Definición de hecho (HU2 y HU3)

### HU2 - Visualización de laboratorios

- **Listado**: `GET /api/Laboratorios` retorna la lista de los laboratorios registrados (los dados de baja lógica, `Active = 0`, no se incluyen).
-  **Información adecuada**: cada laboratorio incluye nombre, ubicación, capacidad, estado y horario de operación (`HoraApertura` / `HoraCierre`) (`LaboratorioResponse`), además de su identificador.
- **Indicación de estado**: cada laboratorio indica su estado, que puede ser `Habilitado` o `FueraDeServicio`. Los laboratorios fuera de servicio también se listan, pero se identifican como tales.
- **Selección del laboratorio (API)**: `GET /api/Laboratorios/{id}` permite obtener el laboratorio elegido, y su `id` es el que se utiliza para consultar disponibilidad y para reservar (HU4).
- **Listado y selección en el Frontend**: la página de inicio muestra tarjetas con imagen, nombre, ubicación, capacidad, horario y estado visible (`Disponible` / `Fuera de servicio`), con buscador por nombre y vista en cartas o en lista. Cada laboratorio habilitado tiene el botón "Ver disponibilidad" para seleccionarlo; en los laboratorios fuera de servicio el botón aparece deshabilitado ("No disponible") y, si se accede directamente a su URL, se redirige al listado con un aviso.

### HU3 - Consulta de disponibilidad

- **Selección**: la consulta se hace sobre el laboratorio seleccionado en el listado de HU2 (`/api/Laboratorios/{id}/disponibilidad`).
- **Información de horario**: el usuario indica **fecha**, **hora inicial** y **hora final** (parámetros `fecha`, `horaInicio` y `horaFin`).
- **Validación de horario**: el sistema rechaza con `400 Bad Request` los horarios inválidos (ver [orden de validaciones](#ii-notas-de-la-solución-hu2-y-hu3)).
- **Verificación de reservas**: se comparan las reservas existentes del laboratorio seleccionado contra el horario consultado.
-  **Indicación de estado**: la respuesta indica con claridad si el laboratorio está disponible o no (`disponible: true | false` y un `mensaje`).
-  **Caso positivo**: si el laboratorio está disponible, la respuesta lo indica y el usuario puede continuar con la reserva (la creación de la reserva se implementó en la HU4).
-  **Sin colisiones**: las reservas con estado `Cancelada` no afectan la disponibilidad.
- **Consulta de disponibilidad en el Frontend**: tabla semanal (lunes a viernes, bloques de una hora dentro del horario del laboratorio) en la que cada bloque se muestra como `Disponible` u `Ocupado`, con navegación entre semanas (sin retroceder antes de la semana actual) y leyenda de colores. Al elegir un bloque disponible se resalta como seleccionado y se muestra el resumen con la fecha y las horas de inicio y fin. Desde ese resumen se continúa con el registro de la reserva (HU4).

## ii. Notas de la solución (HU2 y HU3)

### Modelo de datos involucrado

| Entidad       | Campos relevantes                                                                                    |
|---------------|------------------------------------------------------------------------------------------------------|
| `Laboratorio` | `LaboratorioId`, `Nombre`, `Ubicacion`, `Capacidad`, `Estado`, `HoraApertura`, `HoraCierre`, `Active` |
| `Reserva`     | `ReservaId`, `LaboratorioId`, `UsuarioId`, `Fecha`, `HoraInicio`, `HoraFin`, `Estado`, `Active`      |

Restricciones de base de datos relevantes:

- `Laboratorios.Estado` solo admite `Habilitado` o `FueraDeServicio` (`CK_Lab_Estado`).
- `Laboratorios.HoraCierre` debe ser posterior a `HoraApertura` (`CK_Lab_Horario`).
- `Reservas.Estado` solo admite `Activa` o `Cancelada` (`CK_Res_Estado`).

### DTOs

| DTO                     | Campos                                                          | Uso                          |
|-------------------------|-----------------------------------------------------------------|------------------------------|
| `LaboratorioResponse`   | `laboratorioId`, `nombre`, `ubicacion`, `capacidad`, `estado`, `horaApertura`, `horaCierre` | Respuesta de HU2 |
| `DisponibilidadResponse`| `disponible` (bool), `mensaje` (string)                         | Respuesta de HU3             |
| `ReservaOcupadaResponse`| `fecha`, `horaInicio`, `horaFin`                                | Apoyo de la tabla de HU3 (bloques ocupados) |

### Decisiones de diseño

- **Reservas canceladas**: solo se consideran las reservas con `Estado = 'Activa'`, por lo que las canceladas no bloquean el laboratorio.
- **Reserva de un solo día**: la consulta recibe una única fecha; una reserva no abarca varios días.
- **Laboratorios fuera de servicio visibles**: el listado los incluye para cumplir con la indicación de estado. Para la interfaz se debe mostrar el valor `FueraDeServicio` como **"Fuera de servicio"**.
- **Respuesta de disponibilidad con HTTP 200**: "no disponible" es un resultado de negocio válido, no un error; los errores de validación se reservan para los `4xx`.
- **Tabla semanal con una sola llamada**: para pintar la tabla, el frontend consulta `GET /api/Laboratorios/{id}/reservas` (una llamada por semana) en lugar de consultar la disponibilidad celda por celda. El endpoint `/disponibilidad` conserva las reglas de negocio completas (validación de horario, laboratorio fuera de servicio y conflictos) y es una consulta previa; al confirmar la reserva (HU4) `POST /api/Reservas` aplica de nuevo las mismas reglas.
- **Laboratorios fuera de servicio sin tabla**: como `/reservas` no considera el estado del laboratorio, el frontend no permite abrir la tabla de un laboratorio fuera de servicio (botón deshabilitado y redirección al listado con aviso).

### Observaciones y deuda técnica (HU2 y HU3)
- **Datos semilla con fechas pasadas**: las reservas del script inicial están en septiembre de 2026. Como la consulta rechaza fechas pasadas, no sirven para probar la disponibilidad; por eso las pruebas de [iv. Pruebas (HU2 y HU3)](#iv-pruebas-hu2-y-hu3) incluyen un script de datos con fechas futuras.
- **Cancelación en los datos semilla**: la reserva 1 se "cancela" insertando un registro en la tabla `Cancelaciones`, pero su `Reservas.Estado` permanece en `Activa`. Como la disponibilidad se basa en `Reservas.Estado`, esa reserva seguiría bloqueando el horario. La historia de cancelaciones debe actualizar `Reservas.Estado = 'Cancelada'` al registrar la cancelación (o el script semilla debe corregirse).
- **Campo `Active` de `Reservas`**: la consulta de conflictos no filtra por `Reservas.Active`. Si se llegara a usar como baja lógica, habría que incluirlo en la condición.
- **Fecha de referencia**: la validación de "fecha pasada" usa la fecha del servidor (`DateTime.Today`).
- **Reserva de prueba para la tabla**: el script semilla incluye la reserva 3 (Lab Computo 1, 2026-10-06, 09:00 – 11:00) para visualizar bloques `Ocupado`. Esa fecha ya quedó en el pasado, por lo que ese bloque ya no se muestra como `Ocupado`; para verlo basta registrar una reserva desde el sistema (HU4).
- **Endpoint `/reservas` sin validaciones de negocio**: no valida el estado ni el horario del laboratorio; solo devuelve las reservas activas del rango. Las validaciones completas ocurren en `/disponibilidad`.

## iii. Implementación (HU2 y HU3)

La configuración y ejecución del proyecto están en [Información general del proyecto](#información-general-del-proyecto). Todos los endpoints requieren el encabezado `Authorization: Bearer <token>` (ver [Autenticación y autorización](#autenticación-y-autorización-jwt)).

### Endpoints

| Método | Ruta                                    | Historia | Descripción                                              |
|--------|-----------------------------------------|----------|----------------------------------------------------------|
| GET    | `/api/Laboratorios`                     | HU2      | Lista los laboratorios                                   |
| GET    | `/api/Laboratorios/{id}`                | HU2      | Obtiene un laboratorio específico (selección)            |
| GET    | `/api/Laboratorios/{id}/disponibilidad` | HU3      | Consulta la disponibilidad en una fecha y horario        |
| GET    | `/api/Laboratorios/{id}/reservas`       | HU3      | Lista las reservas activas del laboratorio en un rango de fechas |

Parámetros de `GET /api/Laboratorios/{id}/disponibilidad` (query string):

| Parámetro    | Tipo        | Ejemplo      | Descripción        |
|--------------|-------------|--------------|--------------------|
| `fecha`      | Fecha       | `2026-10-15` | Día de la consulta |
| `horaInicio` | Hora        | `09:00`      | Hora inicial       |
| `horaFin`    | Hora        | `11:00`      | Hora final         |

Parámetros de `GET /api/Laboratorios/{id}/reservas` (query string). Devuelve una lista de `ReservaOcupadaResponse` (`fecha`, `horaInicio`, `horaFin`) con las reservas cuyo `Estado` es `Activa`:

| Parámetro | Tipo  | Ejemplo      | Descripción        |
|-----------|-------|--------------|--------------------|
| `desde`   | Fecha | `2026-10-05` | Inicio del rango   |
| `hasta`   | Fecha | `2026-10-09` | Fin del rango      |

### Guía para realizar consultas en Scalar

#### 1. Realizar Inicio de Sesión (Login)

En el Endpoint de /api/Auth/login, se necesita realizar el login normalmente para poder obtener un token que permite tener la autorización para realizar las consultas
``` HTTP
{
  "nombreUsuario": "Admi",
  "password": "Laboratorios123#"
}
```

>Cuando realiza el login, Scalar devolverá un JSON así

``` JSON
{
  "usuarioId": 1,
  "nombreUsuario": "Admi",
  "nombreCompleto": "Administrador General",
  "rol": "Administrador",
  "token": "R5cCI6IkpXVCJ9.eyJzdWI" (Ejemplo de token)
}

```

>Este token es necesario copiarlo para tenerlo listo en posteriores consultas
#### 2. Ahora se ingresa al endpoint de elección

Para este ejemplo, se presentara el Endpoint **/api/Laboratorios/{id}** . Dentro del apartado de Scalar se presentan diferentes campos
- Id -> En este se tiene que ingresar el id del Laboratorio que se quiere buscar
- Headers (Encabezados de la consulta) **Importante**
	- accept -> Este apartado no se toca y se mantiene igual
	- authorization | Bearer + token del login
		Este campo se tiene que agregar, e incluir ese campo de Bearer para que procese el token
Esta misma accion se repite en cada endpoint que se necesite.
## iv. Pruebas (HU2 y HU3)

Las pruebas se ejecutan contra el API (desde Scalar) y desde el Front-End para así probar ambas partes del sistema.

### HU2 - Visualización de laboratorios

#### Caso HU2-1 — Positivo: listado de laboratorios

| Campo                  | Detalle                                                                                                                                                                         |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Objetivo**           | Verificar que el sistema muestre los laboratorios registrados con su información y estado.                                                                                      |
| **Precondición**       | Base de datos con los 3 laboratorios semilla (todos con `Active = 1`) y token válido.                                                                                           |
| **Entrada**            | `GET /api/Laboratorios` con `Authorization: Bearer <token>`                                                                                                                     |
| **Resultado esperado** | `200 OK` con 3 laboratorios; cada uno incluye `nombre`, `ubicacion`, `capacidad`, `estado`, `horaApertura` y `horaCierre`. Lab Computo 1 y Lab Redes con `Habilitado`; Lab Electrónica con `FueraDeServicio`. |
| **Resultado obtenido** | Scalar, devuelve el json con la lista de los diferentes laboratorios que hay registrados en la base de datos.                                                                   |

#### Caso HU2-2 — Positivo: selección de un laboratorio

| Campo                  | Detalle                                                                                                                                        |
| ---------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------- |
| **Objetivo**           | Verificar que se pueda obtener el laboratorio seleccionado desde el listado para continuar.                                                    |
| **Precondición**       | Existe el laboratorio con `LaboratorioId = 1` y token válido.                                                                                  |
| **Entrada**            | `GET /api/Laboratorios/1`                                                                                                                      |
| **Resultado esperado** | `200 OK` con `{ "laboratorioId": 1, "nombre": "Lab Computo 1", "ubicacion": "Edificio A - Piso 2", "capacidad": 30, "estado": "Habilitado", "horaApertura": "07:00:00", "horaCierre": "22:00:00" }`. |
| **Resultado obtenido** | Scalar devuelve la informacion del laboratorio cuya id esta registrada en la base de datos, sin importar el estado.                            |

#### Caso HU2-3 — Negativo: acceso sin autenticación

| Campo                  | Detalle                                                                                         |
| ---------------------- | ----------------------------------------------------------------------------------------------- |
| **Objetivo**           | Verificar que el listado no esté disponible para usuarios sin sesión.                           |
| **Precondición**       | Ninguna.                                                                                        |
| **Entrada**            | `GET /api/Laboratorios` **sin** el encabezado `Authorization`                                   |
| **Resultado esperado** | `401 Unauthorized`.                                                                             |
| **Resultado obtenido** | Scalar devuelve el resultado esperado, indicando que no esta autorizado para realizar la accion |

### HU3 - Consulta de disponibilidad

#### Caso HU3-1 — Positivo: horario libre

| Campo                  | Detalle                                                                                                                                                               |
| ---------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Objetivo**           | Verificar que el sistema indique disponible un laboratorio sin reservas en el horario consultado.                                                                     |
| **Precondición**       | Datos de prueba cargados; token válido. Lab Computo 1                                                                                                                 |
| **Entrada**            | Token válido; Lab Computo 1<br>Fecha: 2026-12-10T00:00:00Z (Fecha Valida)<br>Hora Inicio: 9:00<br>Hora Fin: 14:00                                                     |
| **Resultado esperado** | `200 OK` con `{ "disponible": true, "mensaje": "El laboratorio se encuentra disponible." }`.                                                                          |
| **Resultado obtenido** | El sistema, devuelve una respuesta satisfactoria, e indica <br>1. Que el laboratorio si esta disponible <br>2. `"mensaje": "El laboratorio se encuentra disponible."` |

#### Caso HU3-2 — Positivo: horario contiguo a una reserva (límite exclusivo)

| Campo                  | Detalle                                                                                                              |
| ---------------------- | -------------------------------------------------------------------------------------------------------------------- |
| **Objetivo**           | Verificar que un horario que inicia justo cuando termina otra reserva no se considere colisión.                      |
| **Precondición**       | Igual que HU3-1 (reserva activa de 09:00 a 11:00 en Lab Computo 1).                                                  |
| **Entrada**            | `GET /api/Laboratorios/1/disponibilidad?fecha=2026-10-15&horaInicio=11:00&horaFin=13:00`                             |
| **Resultado esperado** | `200 OK` con `disponible = true`.                                                                                    |
| **Resultado obtenido** | "disponible": true,
  "mensaje": "El laboratorio se encuentra disponible."                                                                                 |

#### Caso HU3-3 — Positivo: una reserva cancelada no afecta la disponibilidad
Igual que la siguiente prueba, se tienen que incluir datos para poder probar esto.

| Campo                  | Detalle                                                                                             |
| ---------------------- | --------------------------------------------------------------------------------------------------- |
| **Objetivo**           | Verificar que las reservas canceladas no bloqueen el laboratorio.                                   |
| **Precondición**       | Datos de prueba cargados; Lab Redes tiene una reserva **cancelada** el 2026-10-15 de 10:00 a 12:00. |
| **Entrada**            | `GET /api/Laboratorios/2/disponibilidad?fecha=2026-10-15&horaInicio=10:00:00&horaFin=12:00:00` con `Authorization: Bearer <token>` |
| **Resultado esperado** | `200 OK` con `disponible = true`.                                                                   |
| **Resultado obtenido** | "disponible": true,
  "mensaje": "El laboratorio se encuentra disponible."                                                                |

#### Caso HU3-4 — Negativo: horario ocupado por una reserva activa


| Campo                  | Detalle                                                                                                                   |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------------- |
| **Objetivo**           | Verificar que el sistema indique "no disponible" cuando el horario se traslapa con una reserva activa.                    |
| **Precondición**       | Datos de prueba cargados; Lab Computo 1 tiene una reserva activa el 2026-10-15 de 09:00 a 11:00.                          |
| **Entrada**            | `GET /api/Laboratorios/1/disponibilidad?fecha=2026-10-15&horaInicio=09:00:00&horaFin=11:00:00` con `Authorization: Bearer <token>` |
| **Resultado esperado** | `200 OK` con `{ "disponible": false, "mensaje": "El laboratorio no se encuentra disponible en el horario solicitado." }`. |
| **Resultado obtenido** |  "disponible": false,
  "mensaje": "El laboratorio no se encuentra disponible en el horario solicitado."                                                                                      |

#### Caso HU3-5 — Negativo: laboratorio fuera de servicio

| Campo                  | Detalle                                                                                                                      |
| ---------------------- | ---------------------------------------------------------------------------------------------------------------------------- |
| **Objetivo**           | Verificar que un laboratorio fuera de servicio nunca se indique como disponible.                                             |
| **Precondición**       | Lab Electrónica tiene `Estado = 'FueraDeServicio'`                                                                           |
| **Entrada**            | Token válido; Lab Computo 3<br>Fecha: 2026-10-10T00:00:00Z (Fecha Valida)<br>Hora Inicio: 9:00<br>Hora Fin: 14:00            |
| **Resultado esperado** | `200 OK` con `{ "disponible": false, "mensaje": "El laboratorio se encuentra fuera de servicio." }`.                         |
| **Resultado obtenido** | Tira un respuesta adecuada, ya que no da error, sino que comunica que el laboratorio no puede ser consultado dada su estado. |

#### Caso HU3-6 — Neutro: horarios inválidos

| Campo                            | Detalle                                                                                                                  |
| -------------------------------- | ------------------------------------------------------------------------------------------------------------------------ |
| **Objetivo**                     | Verificar que el sistema valide el horario antes de consultar las reservas.                                              |
| **Precondición**                 | Token válido; Lab Computo 1 <br>Fecha: 2026-01-10T00:00:00Z (Fecha Anterior)<br>Hora Inicio: 9:00<br>Hora Fin: 14:00     |
| **Entrada y resultado esperado** | Se espera que el sistema sea capaz de indicar que no se puede consultar una fecha pasada                                 |
| **Resultado obtenido**           | El sistema muestra un mensaje correspondiente:<br>`"mensaje": "No se puede consultar disponibilidad en fechas pasadas."` |

#### Caso HU3-7 — Negativo: laboratorio inexistente

| Campo                  | Detalle                                                                                                                                                  |
| ---------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Objetivo**           | Verificar la respuesta cuando se consulta un laboratorio que no existe.                                                                                  |
| **Precondición**       | No existe el laboratorio con `LaboratorioId = 55`; token válido.                                                                                         |
| **Entrada**            | Token válido; Lab Computo 5<br>Fecha: 2026-01-10T00:00:00Z (Fecha Anterior)<br>Hora Inicio: 9:00<br>Hora Fin: 14:00                                      |
| **Resultado esperado** | `404 Not Found` con `"Laboratorio no encontrado."`. Y ademas se adjunta un mensaje<br>`"mensaje": "Laboratorio no encontrado."`                          |
| **Resultado obtenido** | El sistema indica que el laboratorio no existe, por lo que se deja ver que aunque también la fecha es pasada, el sistema verifica primero la existencia. |

---

# HU4, HU5 y HU6 - Registro de reservas

Las tres historias forman un solo flujo y comparten el mismo endpoint: en **HU4** el usuario registra la reserva de un laboratorio indicando fecha y horario; en **HU5** el sistema valida las condiciones del horario solicitado; y en **HU6** verifica que el laboratorio no tenga otra reserva en ese horario. Todas se resuelven en `POST /api/Reservas` (API) y en la tabla de disponibilidad (frontend), donde el usuario elige el bloque y confirma la reserva.

## i. Definición de hecho (HU4, HU5 y HU6)

### HU4 - Registrar una reserva

- **Información necesaria**: la reserva se registra con laboratorio, responsable, fecha, hora de inicio y hora de finalización. `CrearReservaRequest` recibe laboratorio, fecha y horas; el responsable se toma del usuario autenticado.
- **Correspondencia de reserva**: el responsable es siempre el usuario autenticado. La API obtiene su id del token JWT (no del cuerpo de la petición), por lo que no es posible reservar a nombre de otra persona. El resumen de la pantalla muestra el responsable.
- **Datos obligatorios**: si falta alguno, la API responde `400 Bad Request` indicando cuáles faltan y no registra nada. En el frontend, el botón "Reservar" permanece deshabilitado hasta elegir un bloque y la hora final se selecciona de una lista, por lo que no se envía un formulario incompleto.
- **Horario correcto**: la hora final debe ser posterior a la hora inicial (se valida como parte de HU5).
- **Mensaje de confirmación**: al registrar, la API responde `201 Created` con un `ReservaResponse` (`"Reserva registrada correctamente."`). El frontend muestra ese aviso sobre la tabla de disponibilidad, donde el bloque reservado pasa a `Ocupado`, y la reserva aparece en "Mis reservas".

### HU5 - Validar horario de reserva

- **Horario correcto**: la hora inicial debe ser menor a la hora final.
- **Fecha antigua**: no se permiten reservas en fechas anteriores a la fecha actual.
- **Horario de funcionamiento**: la reserva debe quedar dentro de la hora de apertura y cierre del laboratorio.
- **Duración máxima**: una reserva no puede superar las 4 horas.
- **Validación total**: se evalúan todas las reglas sin excepción. Si alguna no se cumple, la reserva se rechaza con `400 Bad Request`, y la respuesta trae en `mensaje` y `errores` la razón de cada regla incumplida. El frontend muestra el mensaje al usuario.

### HU6 - Evitar conflictos entre reservas

- **Traslape de reservas**: no se registra una reserva si el mismo laboratorio ya tiene una reserva activa en esa fecha cuyo horario se traslape.
- **Conflictos parciales y completos**: la comparación (`horaInicio < reserva.HoraFin` y `horaFin > reserva.HoraInicio`) detecta tanto el traslape parcial (al inicio o al final) como el total (mismo horario, un horario contenido en otro o que lo cubre).
- **Mismo horario en laboratorios diferentes**: la verificación se hace solo contra las reservas del laboratorio solicitado, por lo que otro laboratorio puede reservarse en ese mismo horario.
- **Rechazo de solicitud**: ante un conflicto la API responde `409 Conflict` y no inserta nada.
- **Informar estado**: la respuesta indica `"El laboratorio no se encuentra disponible en el horario solicitado."`. El frontend muestra el aviso y vuelve a cargar la tabla con el bloque en `Ocupado`.
- **Sin falsos conflictos**: los horarios contiguos (uno termina cuando el otro empieza) y las reservas con estado `Cancelada` no bloquean el laboratorio.

## ii. Notas de la solución (HU4, HU5 y HU6)

- **Flujo**: Frontend (tabla de `LaboratoriosController.Disponibilidad`) → el usuario elige un bloque y la hora final → `POST /Reservas/Crear` (`ReservasController` del frontend, con token antifalsificación) → `POST api/Reservas` con el JWT agregado por `JWTAuthHandler` → `ReservasController` (API) → `ReservasDBContext` → tabla `Reservas`. El resultado se guarda en `TempData` y se redirige a la misma semana del laboratorio.
- **Orden de validaciones** del endpoint `POST /api/Reservas`:

| Orden | Validación                                                                                                  | Respuesta                                                                           |
|-------|-------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------|
| 1     | Token sin un id de usuario válido                                                                           | `401 Unauthorized`                                                                  |
| 2     | Falta el laboratorio, la fecha, la hora de inicio o la hora de finalización                                 | `400 Bad Request` — `"Debe indicar la fecha, la hora de inicio."` (lista lo que falta) |
| 3     | Laboratorio inexistente o dado de baja (`Active = 0`)                                                       | `404 Not Found` — `"Laboratorio no encontrado."`                                    |
| 4     | Reglas de horario de HU5: hora final no posterior, fecha pasada, fuera del horario de operación, más de 4 h | `400 Bad Request` — `mensaje` con todas las razones y `errores` con la lista        |
| 5     | Laboratorio con `Estado = 'FueraDeServicio'`                                                                | `409 Conflict` — `"El laboratorio se encuentra fuera de servicio."`                 |
| 6     | Traslape con una reserva activa (HU6)                                                                       | `409 Conflict` — `"El laboratorio no se encuentra disponible en el horario solicitado."` |
| 7     | Todo correcto                                                                                               | `201 Created` — `ReservaResponse`                                                   |

### Modelo de datos involucrado

| Entidad   | Valores al registrar una reserva                                                                                                                      |
|-----------|--------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Reserva` | `UsuarioId` (del token), `LaboratorioId`, `Fecha`, `HoraInicio`, `HoraFin`, `Estado = 'Activa'`, `FechaRegistro` (fecha y hora del servidor), `Active = 1` |

### DTOs

| DTO                   | Campos                                                                                         | Uso                                         |
|-----------------------|------------------------------------------------------------------------------------------------|---------------------------------------------|
| `CrearReservaRequest` | `laboratorioId`, `fecha`, `horaInicio`, `horaFin` (todos opcionales en el contrato)            | Cuerpo de `POST /api/Reservas`              |
| `ReservaResponse`     | `reservaId`, `laboratorioId`, `laboratorio`, `fecha`, `inicioReserva`, `finReserva`, `estado`, `mensaje` | Respuesta de la reserva registrada          |
| `MiReservaResponse`   | `reservaId`, `laboratorioId`, `laboratorio`, `ubicacion`, `fecha`, `horaInicio`, `horaFin`, `estado`, `fechaRegistro` | Respuesta de `GET /api/Reservas/mias`       |

### Decisiones de diseño

- **Responsable tomado del token**: el contrato no incluye el usuario; así se cumple la correspondencia entre la reserva y el usuario autenticado sin depender de lo que envíe el cliente.
- **Campos opcionales en la petición**: `CrearReservaRequest` usa tipos que admiten nulo para distinguir un dato que no se envió de un valor por defecto (por ejemplo `00:00`) y poder indicar qué falta.
- **Validación acumulativa**: `POST /api/Reservas` reporta todas las reglas de horario incumplidas (HU5 exige validar "sin excepción"), a diferencia de `/disponibilidad`, que se detiene en la primera.
- **`409 Conflict` en lugar de `200`**: en `/disponibilidad`, "no disponible" es un resultado de consulta válido (`200`); en `POST /api/Reservas` es una acción que no se puede completar, por lo que se responde `409`.
- **Verificación e inserción en una sola transacción**: la búsqueda de traslapes y el `INSERT` se ejecutan en una transacción con aislamiento `Serializable`, para que dos usuarios no puedan reservar el mismo espacio al mismo tiempo.
- **API como fuente de verdad**: el frontend restringe la hora final a un rango de 1 a 4 horas posteriores al inicio sin pasar del cierre del laboratorio, pero todas las reglas se vuelven a validar en la API.
- **Reserva de un solo día y horas completas en la interfaz**: la tabla trabaja con bloques de una hora; la API acepta cualquier hora que cumpla las reglas. La duración máxima está definida en la constante `DuracionMaximaHoras` de `ReservasController`.
- **Mis reservas (complementario)**: el frontend incluye la pantalla "Mis reservas", alimentada por `GET /api/Reservas/mias`, para que el usuario confirme sus reservas. La base de datos solo guarda `Activa` o `Cancelada`; los estados *Próxima*, *En curso* y *Finalizada* se deducen comparando la fecha y hora de la reserva con la actual. Las pestañas separan "Próximas" (activas que aún no terminan) del "Historial" (finalizadas y canceladas).

### Observaciones y deuda técnica (HU4, HU5 y HU6)

- **Hora pasada en el día actual**: la regla de "fecha antigua" compara solo la fecha, por lo que hoy se acepta una reserva cuya hora ya pasó.
- **Días pasados de la semana en curso**: la tabla arranca el lunes de la semana actual y no deshabilita los días anteriores a hoy; si se elige uno, la API rechaza la reserva y el mensaje se muestra en pantalla.
- **Hora final y bloques ocupados**: la lista de hora final solo respeta las 4 horas y el cierre del laboratorio; si entre el inicio y la hora final elegida hay un bloque ocupado, la API responde `409` y se informa al usuario.
- **Fecha y hora del servidor**: `DateTime.Today` y `DateTime.Now` se evalúan en el servidor, sin manejo de zonas horarias.
- **Mensajes de horario distintos**: el mensaje de horario de operación de `/disponibilidad` muestra las horas como `07:00:00`, mientras que `POST /api/Reservas` las muestra como `07:00`.
- **Cancelación pendiente**: ninguna pantalla ni endpoint cancela reservas todavía; el estado `Cancelada` solo existe por datos cargados en la base de datos.
- **Pruebas automatizadas**: el proyecto `WebApi_Test` cubre el login (8 pruebas); las pruebas de reservas de este sprint se realizaron manualmente (ver [iv. Pruebas (HU4, HU5 y HU6)](#iv-pruebas-hu4-hu5-y-hu6)).
- **Cliente React sin integrar**: `sistema-reservas-react` quedó como base (Vite + Tailwind) con una pantalla de prueba y no reemplaza a las vistas Razor, que siguen siendo la interfaz del sistema.
- **`[Authorize]` desactivado en el frontend**: `LaboratoriosController` del frontend tiene el atributo comentado; el acceso sigue protegido porque la API responde `401` sin sesión válida y el frontend redirige al login, pero conviene reactivarlo.

## iii. Implementación (HU4, HU5 y HU6)

La configuración y ejecución del proyecto están en [Información general del proyecto](#información-general-del-proyecto). Ambos endpoints requieren el encabezado `Authorization: Bearer <token>` (ver [Autenticación y autorización](#autenticación-y-autorización-jwt)).

### Endpoints

| Método | Ruta                 | Historia        | Descripción                                                              |
|--------|----------------------|-----------------|--------------------------------------------------------------------------|
| POST   | `/api/Reservas`      | HU4, HU5 y HU6  | Registra una reserva a nombre del usuario autenticado                    |
| GET    | `/api/Reservas/mias` | Complementario  | Lista las reservas activas y canceladas del usuario autenticado (más recientes primero) |

Ejemplo de petición:

```http
POST /api/Reservas
Authorization: Bearer <token>
Content-Type: application/json

{
  "laboratorioId": 1,
  "fecha": "2026-12-10",
  "horaInicio": "09:00:00",
  "horaFin": "11:00:00"
}
```

Ejemplo de respuesta exitosa (`201 Created`):

```json
{
  "reservaId": 4,
  "laboratorioId": 1,
  "laboratorio": "Lab Computo 1",
  "fecha": "2026-12-10T00:00:00",
  "inicioReserva": "09:00:00",
  "finReserva": "11:00:00",
  "estado": "Activa",
  "mensaje": "Reserva registrada correctamente."
}
```

Ejemplo de rechazo por reglas de horario (`400 Bad Request`), con fecha pasada y duración mayor a 4 horas:

```json
{
  "mensaje": "No se pueden realizar reservas en fechas anteriores a la fecha actual. Una reserva no puede superar las 4 horas.",
  "errores": [
    "No se pueden realizar reservas en fechas anteriores a la fecha actual.",
    "Una reserva no puede superar las 4 horas."
  ]
}
```

Ejemplo de rechazo por conflicto (`409 Conflict`):

```json
{
  "mensaje": "El laboratorio no se encuentra disponible en el horario solicitado."
}
```

### Guía para registrar una reserva en Scalar

1. Iniciar sesión en `/api/Auth/login` con un usuario de prueba (por ejemplo `user01`) y copiar el `token`.
2. Abrir el endpoint `POST /api/Reservas` y, en los encabezados, agregar `authorization` con el valor `Bearer <token>`.
3. En el cuerpo de la petición, indicar `laboratorioId`, `fecha`, `horaInicio` y `horaFin`, como en el ejemplo anterior. No se envía el usuario: se toma del token.
4. Enviar la petición y revisar el código de estado y el `mensaje` de la respuesta.
5. Para ver la reserva registrada, consultar `GET /api/Reservas/mias` con el mismo token.

### Guía para registrar una reserva desde el frontend

1. Iniciar sesión y, en el inicio, elegir un laboratorio habilitado con "Ver disponibilidad".
2. En la tabla semanal, hacer clic en un bloque `Disponible`; se resalta como seleccionado y el panel "Resumen de la reserva" muestra el responsable, el laboratorio, la fecha y la hora de inicio.
3. Elegir la hora de finalización en la lista (de 1 a 4 horas después del inicio, sin pasar del cierre del laboratorio).
4. Presionar "Reservar". La página muestra el aviso de confirmación o el motivo del rechazo, y la tabla se actualiza.
5. Revisar la reserva en "Mis reservas" (menú superior), en la pestaña "Próximas".

## iv. Pruebas (HU4, HU5 y HU6)

Las pruebas se ejecutan contra el API (desde Scalar) y desde el Frontend. Para cada caso se usa un token de `user01`, salvo donde se indique otro usuario. Los casos usan fechas futuras (diciembre de 2026) y, cuando necesitan una reserva previa, esta se registra con el mismo endpoint, ya que las reservas del script semilla están en fechas pasadas. Datos base utilizados:

| Id | Laboratorio       | Horario de operación | Estado            |
|----|-------------------|----------------------|-------------------|
| 1  | Lab Computo 1     | 07:00 – 22:00        | `Habilitado`      |
| 2  | Lab Redes         | 08:00 – 20:00        | `Habilitado`      |
| 3  | Lab Electrónica   | 09:00 – 18:00        | `FueraDeServicio` |

### HU4 - Registrar una reserva

#### Caso HU4-1 — Positivo: reserva con todos los datos válidos

| Campo                  | Detalle                                                                                                                                         |
| ---------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------- |
| **Objetivo**           | Verificar que se registre una reserva con laboratorio, fecha y horario válidos a nombre del usuario autenticado.                                |
| **Precondición**       | Token válido de `user01`; Lab Computo 1 habilitado y sin reservas el 2026-12-10.                                                                |
| **Entrada**            | `POST /api/Reservas`<br>`{ "laboratorioId": 1, "fecha": "2026-12-10", "horaInicio": "09:00:00", "horaFin": "11:00:00" }`                        |
| **Resultado esperado** | `201 Created` con un `ReservaResponse` con `estado = "Activa"`, el horario solicitado y `mensaje = "Reserva registrada correctamente."`.        |
| **Resultado obtenido** | Coincide con lo esperado. La reserva queda registrada y se puede ver en `GET /api/Reservas/mias`.                                               |

#### Caso HU4-2 — Positivo desde el Frontend: reservar desde la tabla de disponibilidad

| Campo                  | Detalle                                                                                                                                                                  |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **Objetivo**           | Verificar el flujo completo de reserva en la interfaz.                                                                                                                   |
| **Precondición**       | Sesión iniciada con `user01`; Lab Computo 1 habilitado y con bloques `Disponible` en la semana siguiente.                                                                |
| **Entrada**            | Ver disponibilidad de Lab Computo 1 → clic en un bloque `Disponible` (por ejemplo, martes 10:00) → hora de finalización `12:00` → "Reservar".                            |
| **Resultado esperado** | La página muestra el aviso `"Reserva registrada correctamente."`, los bloques de 10:00 a 12:00 pasan a `Ocupado` y la reserva aparece en "Mis reservas", pestaña "Próximas". |
| **Resultado obtenido** | Coincide con lo esperado.                                                                                                                                                |

#### Caso HU4-3 — Positivo: el responsable es el usuario autenticado

| Campo                  | Detalle                                                                                                                                   |
| ---------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| **Objetivo**           | Verificar que la reserva quede a nombre del usuario del token y no de otro.                                                               |
| **Precondición**       | Tokens válidos de `user01` y `user02`; Lab Redes sin reservas el 2026-12-15.                                                              |
| **Entrada**            | Con el token de `user02`: `POST /api/Reservas` con `laboratorioId = 2`, `fecha = 2026-12-15`, de `10:00:00` a `12:00:00`. Luego `GET /api/Reservas/mias` con cada token. |
| **Resultado esperado** | `201 Created`. La reserva aparece en `/mias` de `user02` y no aparece en el de `user01`.                                                  |
| **Resultado obtenido** | Coincide con lo esperado.                                                                                                                 |

#### Caso HU4-4 — Negativo: datos obligatorios incompletos

| Campo                  | Detalle                                                                                                      |
| ---------------------- | ------------------------------------------------------------------------------------------------------------ |
| **Objetivo**           | Verificar que no se registre la reserva si faltan datos y que se informe la razón.                           |
| **Precondición**       | Token válido de `user01`.                                                                                    |
| **Entrada**            | `POST /api/Reservas` con `{ "laboratorioId": 1 }`                                                            |
| **Resultado esperado** | `400 Bad Request` con `mensaje = "Debe indicar la fecha, la hora de inicio, la hora de finalización."` y sin registrar nada. |
| **Resultado obtenido** | Coincide con lo esperado.                                                                                    |

#### Caso HU4-5 — Negativo: laboratorio inexistente

| Campo                  | Detalle                                                                                                  |
| ---------------------- | -------------------------------------------------------------------------------------------------------- |
| **Objetivo**           | Verificar la respuesta cuando se reserva un laboratorio que no existe.                                   |
| **Precondición**       | No existe el laboratorio con `LaboratorioId = 55`; token válido.                                         |
| **Entrada**            | `POST /api/Reservas` con `laboratorioId = 55`, `fecha = 2026-12-10`, de `09:00:00` a `11:00:00`.         |
| **Resultado esperado** | `404 Not Found` con `mensaje = "Laboratorio no encontrado."`.                                            |
| **Resultado obtenido** | Coincide con lo esperado.                                                                                |

#### Caso HU4-6 — Negativo: acceso sin autenticación

| Campo                  | Detalle                                                          |
| ---------------------- | ---------------------------------------------------------------- |
| **Objetivo**           | Verificar que no se pueda reservar sin una sesión válida.        |
| **Precondición**       | Ninguna.                                                         |
| **Entrada**            | `POST /api/Reservas` **sin** el encabezado `Authorization`.      |
| **Resultado esperado** | `401 Unauthorized`.                                              |
| **Resultado obtenido** | Coincide con lo esperado.                                        |

### HU5 - Validar horario de reserva

#### Caso HU5-1 — Negativo: hora final no posterior a la hora inicial

| Campo                  | Detalle                                                                                                                        |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------------------ |
| **Objetivo**           | Verificar que se rechace un horario donde la hora final no es posterior a la inicial.                                          |
| **Precondición**       | Token válido; Lab Computo 1.                                                                                                   |
| **Entrada**            | `POST /api/Reservas` con `laboratorioId = 1`, `fecha = 2026-12-10`, `horaInicio = 11:00:00`, `horaFin = 09:00:00` (también con ambas horas iguales). |
| **Resultado esperado** | `400 Bad Request` con `mensaje = "La hora final debe ser posterior a la hora inicial."`.                                       |
| **Resultado obtenido** | Coincide con lo esperado.                                                                                                      |

#### Caso HU5-2 — Negativo: fecha anterior a la actual

| Campo                  | Detalle                                                                                                              |
| ---------------------- | -------------------------------------------------------------------------------------------------------------------- |
| **Objetivo**           | Verificar que no se permita reservar en una fecha pasada.                                                            |
| **Precondición**       | Token válido; Lab Computo 1.                                                                                         |
| **Entrada**            | `POST /api/Reservas` con `laboratorioId = 1`, `fecha = 2026-10-01`, de `09:00:00` a `11:00:00`.                      |
| **Resultado esperado** | `400 Bad Request` con `mensaje = "No se pueden realizar reservas en fechas anteriores a la fecha actual."`.          |
| **Resultado obtenido** | Coincide con lo esperado.                                                                                            |

#### Caso HU5-3 — Negativo: fuera del horario de funcionamiento

| Campo                  | Detalle                                                                                                                  |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------------ |
| **Objetivo**           | Verificar que solo se pueda reservar dentro del horario de operación del laboratorio.                                    |
| **Precondición**       | Token válido; Lab Redes opera de 08:00 a 20:00.                                                                          |
| **Entrada**            | `POST /api/Reservas` con `laboratorioId = 2`, `fecha = 2026-12-10`, de `07:00:00` a `09:00:00`.                          |
| **Resultado esperado** | `400 Bad Request` con `mensaje = "El laboratorio solo puede reservarse entre 08:00 y 20:00."`.                           |
| **Resultado obtenido** | Coincide con lo esperado.                                                                                                |

#### Caso HU5-4 — Negativo: duración mayor a cuatro horas

| Campo                  | Detalle                                                                                           |
| ---------------------- | ------------------------------------------------------------------------------------------------- |
| **Objetivo**           | Verificar que una reserva no supere la duración máxima.                                           |
| **Precondición**       | Token válido; Lab Computo 1.                                                                      |
| **Entrada**            | `POST /api/Reservas` con `laboratorioId = 1`, `fecha = 2026-12-10`, de `09:00:00` a `14:00:00` (5 horas). |
| **Resultado esperado** | `400 Bad Request` con `mensaje = "Una reserva no puede superar las 4 horas."`.                    |
| **Resultado obtenido** | Coincide con lo esperado.                                                                         |

#### Caso HU5-5 — Positivo: duración exacta de cuatro horas

| Campo                  | Detalle                                                                                              |
| ---------------------- | ---------------------------------------------------------------------------------------------------- |
| **Objetivo**           | Verificar que el límite de cuatro horas sea inclusivo.                                               |
| **Precondición**       | Token válido; Lab Computo 1 sin reservas el 2026-12-11.                                              |
| **Entrada**            | `POST /api/Reservas` con `laboratorioId = 1`, `fecha = 2026-12-11`, de `09:00:00` a `13:00:00`.      |
| **Resultado esperado** | `201 Created` con la reserva de 09:00 a 13:00.                                                       |
| **Resultado obtenido** | Coincide con lo esperado.                                                                            |

#### Caso HU5-6 — Negativo: varias reglas incumplidas a la vez (validación total)

| Campo                  | Detalle                                                                                                                                                                  |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **Objetivo**           | Verificar que el sistema evalúe todas las reglas y las informe juntas.                                                                                                   |
| **Precondición**       | Token válido; Lab Computo 1.                                                                                                                                             |
| **Entrada**            | `POST /api/Reservas` con `laboratorioId = 1`, `fecha = 2026-10-01`, de `09:00:00` a `14:00:00` (fecha pasada y 5 horas).                                                 |
| **Resultado esperado** | `400 Bad Request` con las dos razones en `mensaje` y en la lista `errores`: fecha anterior a la actual y duración mayor a 4 horas.                                       |
| **Resultado obtenido** | Coincide con lo esperado.                                                                                                                                                |

#### Caso HU5-7 — Neutro desde el Frontend: la hora final se limita según las reglas

| Campo                  | Detalle                                                                                                                                                         |
| ---------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Objetivo**           | Verificar que la interfaz solo ofrezca horas finales válidas.                                                                                                   |
| **Precondición**       | Sesión iniciada; Lab Redes (cierra a las 20:00) con bloques disponibles.                                                                                        |
| **Entrada**            | Clic en un bloque `Disponible` de las 09:00 y luego en uno de las 19:00, revisando la lista "Hora de finalización".                                             |
| **Resultado esperado** | Con inicio a las 09:00 la lista ofrece de 10:00 a 13:00 (máximo 4 horas); con inicio a las 19:00 solo ofrece 20:00 (no pasa del cierre). La duración mostrada se actualiza al cambiar la hora final. |
| **Resultado obtenido** | Coincide con lo esperado.                                                                                                                                       |

#### Caso HU5-8 — Negativo desde el Frontend: día anterior a hoy dentro de la semana en curso

| Campo                  | Detalle                                                                                                                                   |
| ---------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| **Objetivo**           | Verificar que se rechace y se explique el motivo cuando se elige un día que ya pasó.                                                      |
| **Precondición**       | Sesión iniciada; la semana en curso tiene días ya pasados (por ejemplo, un jueves y se elige el lunes).                                   |
| **Entrada**            | Clic en un bloque `Disponible` de un día anterior a hoy → hora de finalización → "Reservar".                                              |
| **Resultado esperado** | La página muestra el aviso en rojo `"No se pueden realizar reservas en fechas anteriores a la fecha actual."` y no se registra la reserva. |
| **Resultado obtenido** | Coincide con lo esperado.                                                                                                                 |

### HU6 - Evitar conflictos entre reservas

Para los casos HU6-1 a HU6-4 se parte de una reserva activa: Lab Computo 1, 2026-12-14, de 09:00 a 11:00 (registrada con `POST /api/Reservas`).

#### Caso HU6-1 — Negativo: mismo laboratorio y mismo horario

| Campo                  | Detalle                                                                                                                              |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------------------------ |
| **Objetivo**           | Verificar que no se registren dos reservas idénticas para el mismo laboratorio.                                                      |
| **Precondición**       | Reserva activa de Lab Computo 1, 2026-12-14, 09:00 – 11:00.                                                                          |
| **Entrada**            | `POST /api/Reservas` con `laboratorioId = 1`, `fecha = 2026-12-14`, de `09:00:00` a `11:00:00`.                                      |
| **Resultado esperado** | `409 Conflict` con `mensaje = "El laboratorio no se encuentra disponible en el horario solicitado."` y sin nueva reserva.            |
| **Resultado obtenido** | Coincide con lo esperado.                                                                                                            |

#### Caso HU6-2 — Negativo: traslape parcial o contenido

| Campo                  | Detalle                                                                                                                                              |
| ---------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Objetivo**           | Verificar que se detecten los conflictos parciales y completos entre horarios.                                                                       |
| **Precondición**       | Reserva activa de Lab Computo 1, 2026-12-14, 09:00 – 11:00.                                                                                          |
| **Entrada**            | Cuatro peticiones `POST /api/Reservas` para Lab Computo 1 el 2026-12-14 con los horarios: `08:00–10:00` (traslape al inicio), `10:00–12:00` (traslape al final), `09:30–10:30` (contenido) y `08:00–12:00` (la cubre). |
| **Resultado esperado** | Las cuatro responden `409 Conflict` con `"El laboratorio no se encuentra disponible en el horario solicitado."`.                                     |
| **Resultado obtenido** | Coincide con lo esperado.                                                                                                                            |

#### Caso HU6-3 — Positivo: horario contiguo a una reserva

| Campo                  | Detalle                                                                                            |
| ---------------------- | -------------------------------------------------------------------------------------------------- |
| **Objetivo**           | Verificar que un horario que empieza justo cuando termina otra reserva no es un conflicto.         |
| **Precondición**       | Reserva activa de Lab Computo 1, 2026-12-14, 09:00 – 11:00.                                        |
| **Entrada**            | `POST /api/Reservas` con `laboratorioId = 1`, `fecha = 2026-12-14`, de `11:00:00` a `13:00:00`.    |
| **Resultado esperado** | `201 Created`.                                                                                     |
| **Resultado obtenido** | Coincide con lo esperado.                                                                          |

#### Caso HU6-4 — Positivo: mismo horario en un laboratorio diferente

| Campo                  | Detalle                                                                                         |
| ---------------------- | ----------------------------------------------------------------------------------------------- |
| **Objetivo**           | Verificar que los conflictos se evalúen por laboratorio.                                        |
| **Precondición**       | Reserva activa de Lab Computo 1, 2026-12-14, 09:00 – 11:00; Lab Redes sin reservas ese día.     |
| **Entrada**            | `POST /api/Reservas` con `laboratorioId = 2`, `fecha = 2026-12-14`, de `09:00:00` a `11:00:00`. |
| **Resultado esperado** | `201 Created`.                                                                                  |
| **Resultado obtenido** | Coincide con lo esperado.                                                                       |

#### Caso HU6-5 — Positivo: una reserva cancelada no bloquea el horario

| Campo                  | Detalle                                                                                                                                                                              |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **Objetivo**           | Verificar que solo las reservas activas generen conflicto.                                                                                                                           |
| **Precondición**       | Reserva de Lab Computo 1, 2026-12-16, 14:00 – 16:00, cuyo estado se cambia a cancelada en la base de datos: `UPDATE Reservas SET Estado = 'Cancelada' WHERE ReservaId = <id>;`.      |
| **Entrada**            | `POST /api/Reservas` con `laboratorioId = 1`, `fecha = 2026-12-16`, de `14:00:00` a `16:00:00`.                                                                                      |
| **Resultado esperado** | `201 Created`.                                                                                                                                                                       |
| **Resultado obtenido** | Coincide con lo esperado.                                                                                                                                                            |

#### Caso HU6-6 — Negativo desde el Frontend: la hora final alcanza un bloque ocupado

| Campo                  | Detalle                                                                                                                                                                      |
| ---------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Objetivo**           | Verificar que la interfaz informe el conflicto y muestre el estado real del laboratorio.                                                                                     |
| **Precondición**       | Sesión iniciada; Lab Computo 1 tiene una reserva activa el martes de la semana siguiente de 11:00 a 12:00; el bloque de las 10:00 de ese día está `Disponible`.              |
| **Entrada**            | Clic en el bloque `Disponible` de las 10:00 de ese martes → hora de finalización `13:00` (cruza el bloque ocupado) → "Reservar".                                              |
| **Resultado esperado** | La página muestra el aviso en rojo `"El laboratorio no se encuentra disponible en el horario solicitado."`, no se crea la reserva y la tabla conserva el bloque de 11:00 como `Ocupado`. |
| **Resultado obtenido** | Coincide con lo esperado.                                                                                                                                                    |

> Nota: las pruebas se realizaron tanto desde la interfaz del API como desde el propio Frontend, para confirmar que las reglas se cumplen en ambas partes.
