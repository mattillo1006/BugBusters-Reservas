# BugBusters-Reservas

Sistema de Reservas de Laboratorios.

Este repositorio contiene el proyecto **BugBusters-Reservas**, un sistema para la gestión de reservas de laboratorios. La primera entrega (Sprint 1) se centró en el módulo de autenticación (login), sobre el cual se construyen las historias siguientes (visualización de laboratorios y consulta de disponibilidad en el Sprint 2 y, a futuro, reservas y cancelaciones). Hasta el momento se han entregado las siguientes historias de usuario:

| Historia | Descripción                                | Estado                                  |
| -------- | ------------------------------------------ | --------------------------------------- |
| **HU1**  | Inicio de sesión (login)                   | API + Frontend                          |
| **HU2**  | Visualización de laboratorios              | API + Frontend |
| **HU3**  | Consulta de disponibilidad de laboratorios | API + Frontend |

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
  - [Evolución respecto al Sprint 1](#evolución-respecto-al-sprint-1)
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

---

# Información general del proyecto


## Arquitectura

El proyecto está compuesto por **dos aplicaciones .NET 10** que se ejecutan en conjunto, además del script de base de datos.

```
BugBusters-Reservas/
├── WebAPI/                          # Backend - API REST (ASP.NET Core Web API)
│   ├── Controllers/
│   │   ├── AuthController.cs        # Controlador del endpoint Auth (login)
│   │   └── LaboratoriosController.cs
│   ├── DTOs/
│   │   ├── LoginRequest.cs
│   │   ├── LoginResponse.cs
│   │   ├── LaboratorioResponse.cs
│   │   ├── DisponibilidadResponse.cs
│   │   └── ReservaOcupadaResponse.cs
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
│   │   └── LaboratoriosController.cs # Tabla de disponibilidad (HU3)
│   ├── Handlers/
│   │   └── JWTAuthHandler.cs        # Agrega el JWT a las llamadas hacia la WebAPI
│   ├── Models/
│   │   ├── LoginViewModel.cs
│   │   ├── Laboratorio.cs
│   │   └── DisponibilidadViewModel.cs
│   ├── Views/                       # Vistas Razor (html)
│   │   ├── Account/Login.cshtml
│   │   ├── Home/Index.cshtml
│   │   ├── Laboratorios/Disponibilidad.cshtml
│   │   └── Shared/_Layout.cshtml
│   ├── wwwroot/
│   │   ├── css/                     # Estilos (login.css, home.css)
│   │   └── img/                     # Imágenes (laboratorios, logo, login)
│   ├── SistemaDeReservas.slnx
│   ├── SistemaDeReservas.slnLaunch
│   └── Program.cs
├── database/
│   └── 01_create_database.sql       # Script de creación y datos semilla
└── README.md
```

- **`WebAPI`**: expone los servicios vía **API REST**, siguiendo el patrón Controller → DTO → DbContext → Modelo. Concentra la lógica de negocio (autenticación, consulta de laboratorios y disponibilidad).
- **`SistemaDeReservas`**: proyecto ASP.NET Core MVC que actúa como cliente web. Consume la `WebAPI` mediante un `HttpClient` con nombre (`"WebAPI"`) al que se le añade automáticamente el token JWT del usuario autenticado. Incluye las pantallas de inicio de sesión, listado de laboratorios y consulta de disponibilidad.
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
  - 3 reservas y 1 cancelación de ejemplo. La reserva 3 (Lab Computo 1, 2026-10-06, 09:00 – 11:00) permite ver un bloque `Ocupado` en la tabla de disponibilidad.

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

## Autenticación y autorización (JWT)

Es un mecanismo transversal: **todos los endpoints, excepto `POST /api/Auth/login`, requieren un token JWT** (atributo `[Authorize]`).

1. El usuario inicia sesión con `POST /api/Auth/login` y el API responde con sus datos básicos y un `token`.
2. **Frontend**: `AccountController` guarda el token como un claim (`AccessToken`) dentro de la cookie de autenticación. Luego, `JWTAuthHandler` lo lee y lo agrega como encabezado `Authorization: Bearer <token>` en cada llamada hacia la WebAPI.
3. **Pruebas directas contra el API** (Scalar, Postman, etc.): se obtiene el token con el login y se envía manualmente en el encabezado `Authorization: Bearer <token>`. En Scalar se puede configurar en la sección de autenticación de la interfaz.

El token incluye como claims el id, nombre de usuario, nombre completo y rol, y expira según `Jwt:ExpiresInMinutes` (60 minutos). Una petición sin token, o con un token inválido o vencido, recibe `401 Unauthorized`.

En el frontend, la cookie de autenticación es `HttpOnly`, `SameSite=Strict` y expira a los 60 minutos de inactividad (expiración deslizante). El login es la página inicial de la aplicación; si ya existe una sesión activa, abrirlo redirige directamente al inicio. Cuando no hay sesión, o cuando la WebAPI responde `401` (token inválido o vencido), el frontend cierra la sesión y redirige al login.

## Resumen de endpoints

| Método | Ruta                                          | Auth | Historia | Descripción                                             |
|--------|-----------------------------------------------|------|----------|---------------------------------------------------------|
| POST   | `/api/Auth/login`                             | No   | HU1      | Autentica un usuario y devuelve sus datos y el token    |
| GET    | `/api/Laboratorios`                           | Sí   | HU2      | Lista los laboratorios registrados                      |
| GET    | `/api/Laboratorios/{id}`                      | Sí   | HU2      | Obtiene un laboratorio (selección para reservar)        |
| GET    | `/api/Laboratorios/{id}/disponibilidad`       | Sí   | HU3      | Consulta si un laboratorio está disponible en un horario |
| GET    | `/api/Laboratorios/{id}/reservas`             | Sí   | HU3      | Lista las reservas activas de un laboratorio en un rango de fechas (marca los bloques ocupados de la tabla) |

## Decisiones de diseño y deuda técnica

Aplican a todo el proyecto:

- **Contraseñas con hash BCrypt**: las contraseñas se almacenan con hash BCrypt (`BCrypt.Net-Next`) y el login las valida con `BCrypt.Net.BCrypt.Verify`; ya no se guardan ni se comparan en texto plano como ocurría en el Sprint 1. Un valor que no tenga formato de hash BCrypt en `Usuarios.Contraseña` hace fallar la verificación, por lo que todo usuario nuevo debe guardarse hasheado.
- **Secretos en el repositorio**: la clave `Jwt:Key` y la contraseña de la base de datos están en `appsettings.json`. Antes de un ambiente productivo deben moverse a *user-secrets*, variables de entorno o un gestor de secretos.
- **DTOs separados del modelo**: se usan DTOs (`LoginRequest`, `LoginResponse`, `LaboratorioResponse`, `DisponibilidadResponse`, `ReservaOcupadaResponse`) en lugar de exponer directamente las entidades de EF Core. Así se evita filtrar campos internos o sensibles (como la contraseña o `Active`) en las respuestas.
- **Autorización por rol pendiente**: el token ya transporta el rol del usuario, pero por ahora los endpoints solo exigen estar autenticado. Las restricciones por rol (Administrador / Usuario) quedan para historias futuras.

## Evolución respecto al Sprint 1

Resumen de lo que cambió entre la primera entrega (solo login) y el estado actual del proyecto:

| Tema                       | Sprint 1                                                                 | Sprint 2                                                                                   |
|----------------------------|--------------------------------------------------------------------------|--------------------------------------------------------------------------------------------|
| Alcance                    | HU1: login                                                               | HU1 + HU2 (visualizar laboratorios) + HU3 (consultar disponibilidad)                       |
| Token de sesión            | El login devolvía solo los datos del usuario; no había JWT ni cookie     | El login devuelve un JWT; el frontend lo guarda en la cookie y lo reenvía a la WebAPI      |
| Protección de endpoints    | Sin autorización sobre los endpoints                                     | Todos los endpoints, excepto `POST /api/Auth/login`, exigen JWT (`[Authorize]`)             |
| Contraseñas                | Texto plano (comparación directa y datos semilla en texto plano)         | Hash BCrypt y verificación con `BCrypt.Verify`                                             |
| Frontend                   | Solo el *scaffolding* de la plantilla MVC (Home/Privacy/Error)           | Login, listado de laboratorios y tabla de disponibilidad; login como página inicial        |
| Endpoints                  | `POST /api/Auth/login`                                                   | Se agregan `GET /api/Laboratorios`, `/{id}`, `/{id}/disponibilidad` y `/{id}/reservas`      |
| Datos semilla              | Roles y 4 usuarios de prueba                                             | Además laboratorios, reservas y una cancelación de ejemplo                                 |
| Ejecución en Visual Studio | Se lanzaba el frontend por aparte del API                                | Perfil de arranque simultáneo (`SistemaDeReservas.slnLaunch`)                              |

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

Ambas historias forman un solo flujo: en **HU2** el usuario ve el listado de laboratorios y selecciona uno; en **HU3** consulta, para ese laboratorio, si está disponible en una fecha y horario. Si lo está, puede continuar con el proceso de reserva (historia futura).

## i. Definición de hecho (HU2 y HU3)

### HU2 - Visualización de laboratorios

- **Listado**: `GET /api/Laboratorios` retorna la lista de los laboratorios registrados (los dados de baja lógica, `Active = 0`, no se incluyen).
-  **Información adecuada**: cada laboratorio incluye nombre, ubicación, capacidad, estado y horario de operación (`HoraApertura` / `HoraCierre`) (`LaboratorioResponse`), además de su identificador.
- **Indicación de estado**: cada laboratorio indica su estado, que puede ser `Habilitado` o `FueraDeServicio`. Los laboratorios fuera de servicio también se listan, pero se identifican como tales.
- **Selección del laboratorio (API)**: `GET /api/Laboratorios/{id}` permite obtener el laboratorio elegido, y su `id` es el que se utiliza para consultar disponibilidad y, en el futuro, reservar.
- **Listado y selección en el Frontend**: la página de inicio muestra tarjetas con imagen, nombre, ubicación, capacidad, horario y estado visible (`Disponible` / `Fuera de servicio`), con buscador por nombre y vista en cartas o en lista. Cada laboratorio habilitado tiene el botón "Ver disponibilidad" para seleccionarlo; en los laboratorios fuera de servicio el botón aparece deshabilitado ("No disponible") y, si se accede directamente a su URL, se redirige al listado con un aviso.

### HU3 - Consulta de disponibilidad

- **Selección**: la consulta se hace sobre el laboratorio seleccionado en el listado de HU2 (`/api/Laboratorios/{id}/disponibilidad`).
- **Información de horario**: el usuario indica **fecha**, **hora inicial** y **hora final** (parámetros `fecha`, `horaInicio` y `horaFin`).
- **Validación de horario**: el sistema rechaza con `400 Bad Request` los horarios inválidos (ver [orden de validaciones](#ii-notas-de-la-solución-hu2-y-hu3)).
- **Verificación de reservas**: se comparan las reservas existentes del laboratorio seleccionado contra el horario consultado.
-  **Indicación de estado**: la respuesta indica con claridad si el laboratorio está disponible o no (`disponible: true | false` y un `mensaje`).
-  **Caso positivo**: si el laboratorio está disponible, la respuesta lo indica y el usuario puede continuar con la reserva (la creación de la reserva corresponde a una historia futura).
-  **Sin colisiones**: las reservas con estado `Cancelada` no afectan la disponibilidad.
- **Consulta de disponibilidad en el Frontend**: tabla semanal (lunes a viernes, bloques de una hora dentro del horario del laboratorio) en la que cada bloque se muestra como `Disponible` u `Ocupado`, con navegación entre semanas (sin retroceder antes de la semana actual) y leyenda de colores. Al elegir un bloque disponible se resalta como seleccionado y se muestra el resumen con la fecha y las horas de inicio y fin. La continuación hacia la reserva corresponde a una historia futura.

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
- **Tabla semanal con una sola llamada**: para pintar la tabla, el frontend consulta `GET /api/Laboratorios/{id}/reservas` (una llamada por semana) en lugar de consultar la disponibilidad celda por celda. El endpoint `/disponibilidad` conserva las reglas de negocio completas (validación de horario, laboratorio fuera de servicio y conflictos) y es el que se debe usar como verificación final al confirmar una reserva (historia futura).
- **Laboratorios fuera de servicio sin tabla**: como `/reservas` no considera el estado del laboratorio, el frontend no permite abrir la tabla de un laboratorio fuera de servicio (botón deshabilitado y redirección al listado con aviso).

### Observaciones y deuda técnica (HU2 y HU3)
- **Datos semilla con fechas pasadas**: las reservas del script inicial están en septiembre de 2026. Como la consulta rechaza fechas pasadas, no sirven para probar la disponibilidad; por eso las pruebas de [iv. Pruebas (HU2 y HU3)](#iv-pruebas-hu2-y-hu3) incluyen un script de datos con fechas futuras.
- **Cancelación en los datos semilla**: la reserva 1 se "cancela" insertando un registro en la tabla `Cancelaciones`, pero su `Reservas.Estado` permanece en `Activa`. Como la disponibilidad se basa en `Reservas.Estado`, esa reserva seguiría bloqueando el horario. La historia de cancelaciones debe actualizar `Reservas.Estado = 'Cancelada'` al registrar la cancelación (o el script semilla debe corregirse).
- **Campo `Active` de `Reservas`**: la consulta de conflictos no filtra por `Reservas.Active`. Si se llegara a usar como baja lógica, habría que incluirlo en la condición.
- **Fecha de referencia**: la validación de "fecha pasada" usa la fecha del servidor (`DateTime.Today`).
- **Reserva de prueba para la tabla**: el script semilla incluye la reserva 3 (Lab Computo 1, 2026-10-06, 09:00 – 11:00) para visualizar bloques `Ocupado`. Cuando esa fecha quede en el pasado, se debe actualizar o agregar otra reserva con fecha futura.
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
| **Entrada**            |                                                                                                     |
| **Resultado esperado** | `200 OK` con `disponible = true`.                                                                   |
| **Resultado obtenido** | "disponible": true,
  "mensaje": "El laboratorio se encuentra disponible."                                                                |

#### Caso HU3-4 — Negativo: horario ocupado por una reserva activa


| Campo                  | Detalle                                                                                                                   |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------------- |
| **Objetivo**           | Verificar que el sistema indique "no disponible" cuando el horario se traslapa con una reserva activa.                    |
| **Precondición**       | Datos de prueba cargados; Lab Computo 1 tiene una reserva activa el 2026-10-15 de 09:00 a 11:00.                          |
| **Entrada**            |                                                                                                                           |
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
