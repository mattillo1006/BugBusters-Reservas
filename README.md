# BugBusters-Reservas

Sistema de Reservas de Laboratorios.

Este repositorio contiene el proyecto **BugBusters-Reservas**, un sistema para la gestión de reservas de laboratorios. El Sprint 1 entregó el login; el Sprint 2, la visualización de laboratorios y la consulta de disponibilidad; el Sprint 3, el registro de reservas con validación de horario y control de conflictos; y el **Sprint 4**, la consulta de "Mis reservas" y la cancelación de reservas. Historias entregadas hasta el momento:

| Historia | Descripción                                | Sprint | Estado         |
| -------- | ------------------------------------------ | ------ | -------------- |
| **HU1**  | Inicio de sesión (login)                   | 1      | API + Frontend |
| **HU2**  | Visualización de laboratorios              | 2      | API + Frontend |
| **HU3**  | Consulta de disponibilidad de laboratorios | 2      | API + Frontend |
| **HU4**  | Registrar una reserva                      | 3      | API + Frontend |
| **HU5**  | Validar horario de reserva                 | 3      | API + Frontend |
| **HU6**  | Evitar conflictos entre reservas           | 3      | API + Frontend |
| **HU7**  | Consultar mis reservas                     | 4      | API + Frontend |
| **HU8**  | Cancelar una reserva                       | 4      | API + Frontend |

Las secciones que aplican a todo el proyecto (arquitectura, tecnologías, configuración, ejecución y seguridad) están en [Información general del proyecto](#información-general-del-proyecto). Cada historia solo documenta lo que le es propio.

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
- [HU2 y HU3 - Visualización y consulta de disponibilidad de laboratorios](#hu2-y-hu3---visualización-y-consulta-de-disponibilidad-de-laboratorios)
- [HU4, HU5 y HU6 - Registro de reservas](#hu4-hu5-y-hu6---registro-de-reservas)
- [HU7 y HU8 - Consultar y cancelar reservas (Sprint 4)](#hu7-y-hu8---consultar-y-cancelar-reservas-sprint-4)
  - [i. Definición de hecho (HU7 y HU8)](#i-definición-de-hecho-hu7-y-hu8)
  - [ii. Notas de la solución (HU7 y HU8)](#ii-notas-de-la-solución-hu7-y-hu8)
  - [iii. Implementación (HU7 y HU8)](#iii-implementación-hu7-y-hu8)
  - [iv. Pruebas (HU7 y HU8)](#iv-pruebas-hu7-y-hu8)

Cada historia está organizada en: **i. Definición de hecho**, **ii. Notas de la solución**, **iii. Implementación** y **iv. Pruebas**.

---

# Información general del proyecto

## Arquitectura

El proyecto está compuesto por **dos aplicaciones .NET 10** que se ejecutan en conjunto, un proyecto de pruebas y el script de base de datos.

```
BugBusters-Reservas/
├── WebAPI/                          # Backend - API REST (ASP.NET Core Web API)
│   ├── Controllers/
│   │   ├── AuthController.cs        # Login (HU1)
│   │   ├── LaboratoriosController.cs # Laboratorios y disponibilidad (HU2, HU3)
│   │   └── ReservasController.cs    # Registrar, consultar y cancelar reservas (HU4-HU8)
│   ├── DTOs/                        # Contratos de entrada/salida (Login*, Laboratorio*, Disponibilidad*,
│   │                                #   ReservaOcupada*, CrearReserva*, Reserva*, MiReserva*, CancelarReserva*)
│   ├── Models/                      # Usuario, Rol, Laboratorio, Reserva, Cancelacion
│   ├── Services/TokenService.cs     # Generación del JWT
│   ├── Data/ReservasDBContext.cs
│   ├── Program.cs
│   └── appsettings.json
├── SistemaDeReservas/               # Frontend - ASP.NET Core MVC
│   ├── Controllers/                 # Account, Home, Laboratorios, Reservas ("Mis reservas", crear y cancelar)
│   ├── Handlers/JWTAuthHandler.cs   # Agrega el JWT a las llamadas hacia la WebAPI
│   ├── Models/                      # ViewModels (Login, Laboratorio, Disponibilidad, MisReservas)
│   ├── Views/                       # Vistas Razor: Account/Login, Home/Index, Laboratorios/Disponibilidad,
│   │                                #   Reservas/index (Mis reservas), Shared/_Layout
│   ├── wwwroot/                     # css (login, home, disponibilidad, mis-reservas) e img
│   └── Program.cs
├── WebApi_Test/                     # Pruebas unitarias (xUnit + EF Core InMemory)
│   └── Unitarias/                   # Auth, Laboratorios (consulta y GETs), Reservas (Crear y Cancelar): 45 pruebas
├── database/
│   └── 01_create_database.sql       # Script de creación y datos semilla
├── azure-pipelines.yml              # CI: compila, prueba con cobertura y analiza en SonarQube Cloud
└── README.md
```

- **`WebAPI`**: expone los servicios vía **API REST** con el patrón Controller → DTO → DbContext → Modelo. Concentra la lógica de negocio (autenticación, laboratorios y disponibilidad, registro y cancelación de reservas, con sus validaciones).
- **`SistemaDeReservas`**: proyecto ASP.NET Core MVC que actúa como cliente web. Consume la `WebAPI` mediante un `HttpClient` con nombre (`"WebAPI"`) al que se le añade automáticamente el JWT del usuario autenticado. No contiene reglas de negocio: solo presenta datos y guía al usuario.
- **`WebApi_Test`**: pruebas unitarias (xUnit) con base de datos en memoria; referencia al proyecto `WebAPI`.
- **Base de datos**: SQL Server, versionada mediante `database/01_create_database.sql`.
- Los proyectos comparten la solución `SistemaDeReservas.slnx`, por lo que se ejecutan y depuran desde un solo lugar.
- **Ortogonalidad, acoplamiento y cohesión**: el frontend solo conoce el contrato HTTP de la API (no el modelo de datos); cada controlador de la API agrupa un solo tema (auth, laboratorios, reservas); los DTOs desacoplan el contrato de las entidades.

> Este esquema se actualiza conforme avanza el proyecto.

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
| Pruebas unitarias | xUnit + `Microsoft.EntityFrameworkCore.InMemory`                        |
| CI                | Azure Pipelines + SonarQube Cloud                                       |

## Requisitos previos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- SQL Server (local, contenedor Docker o instancia remota)
- (Opcional) Visual Studio 2022+ o VS Code con la extensión de C#

## Configuración inicial

### 1. Base de datos

Ejecutar `database/01_create_database.sql` contra una instancia de SQL Server. El script:

- Crea la base de datos `ReservasLaboratorios` (elimina la existente si ya existía).
- Crea las tablas `Roles`, `Usuarios`, `Laboratorios`, `Reservas` y `Cancelaciones` con sus llaves y restricciones. `Cancelaciones` guarda **un único registro por reserva** (`UQ_Can_Reserva`) con quién canceló, cuándo y el motivo.
- Inserta datos semilla: 4 usuarios (`Admi` con rol Administrador; `user01`, `user02`, `user03` con rol Usuario), todos con la contraseña `Laboratorios123#` (hash BCrypt), 3 laboratorios, 3 reservas y 1 cancelación de ejemplo.

| Id | Laboratorio       | Ubicación            | Capacidad | Estado            | Horario de operación |
|----|-------------------|----------------------|-----------|-------------------|----------------------|
| 1  | Lab Computo 1     | Edificio A - Piso 2  | 30        | `Habilitado`      | 07:00 – 22:00        |
| 2  | Lab Redes         | Edificio B - Piso 1  | 25        | `Habilitado`      | 08:00 – 20:00        |
| 3  | Lab Electrónica   | Edificio C - Piso 3  | 20        | `FueraDeServicio` | 09:00 – 18:00        |

Reservas semilla: la 1 y la 3 son de `user01` (ids de usuario: `Admi`=1, `user01`=2, `user02`=3, `user03`=4) y la 2 de `user02`; `user03` no tiene reservas. **Las tres están en fechas ya pasadas**, por lo que no sirven para probar reservas ni cancelaciones: las pruebas registran sus propias reservas con fechas futuras (diciembre de 2026).

### 2. Cadena de conexión y configuración JWT

En `WebAPI/appsettings.json` ajustar `ConnectionStrings:Default` y la sección `Jwt`:

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

| Proyecto            | URL por defecto                                    |
|---------------------|----------------------------------------------------|
| `WebAPI`            | `http://localhost:5068` (Scalar en `/scalar`)      |
| `SistemaDeReservas` | `http://localhost:5082` / `https://localhost:7241` |

```bash
# API
cd WebAPI && dotnet restore && dotnet run

# Frontend (en otra terminal)
cd SistemaDeReservas && dotnet run
```

**Ambos proyectos deben estar en ejecución al mismo tiempo**, porque el frontend consume la API.

### Nota de despliegue (Visual Studio)

El repositorio incluye `SistemaDeReservas/SistemaDeReservas.slnLaunch` con el perfil **"Perfil de Arranque Simultaneo"**, que lanza `WebAPI` y `SistemaDeReservas` a la vez. Basta seleccionarlo en Visual Studio antes de ejecutar.

### Integración continua (Azure Pipelines)

`azure-pipelines.yml` se ejecuta con cada push a `main`: instala .NET 10, restaura y compila, ejecuta las pruebas de `WebApi_Test` con cobertura, publica los resultados y envía el análisis a SonarQube Cloud (con su Quality Gate). Si alguna prueba falla, el pipeline falla.

### Pruebas automatizadas

```bash
cd WebApi_Test
dotnet test
```

45 pruebas unitarias que no necesitan SQL Server (usan una base en memoria). Detalle en [iv. Pruebas (HU7 y HU8)](#iv-pruebas-hu7-y-hu8).

## Autenticación y autorización (JWT)

Mecanismo transversal: **todos los endpoints, excepto `POST /api/Auth/login`, requieren un token JWT** (`[Authorize]`).

1. El usuario inicia sesión con `POST /api/Auth/login` y la API responde con sus datos básicos y un `token`.
2. **Frontend**: `AccountController` guarda el token como claim (`AccessToken`) en la cookie de autenticación y `JWTAuthHandler` lo envía como `Authorization: Bearer <token>` en cada llamada a la WebAPI.
3. **Pruebas directas contra la API** (Scalar, Postman): se obtiene el token con el login y se envía en el encabezado `Authorization: Bearer <token>`.

El token incluye id, nombre de usuario, nombre completo y rol, y expira a los 60 minutos (`Jwt:ExpiresInMinutes`). Sin token, o con uno inválido o vencido, la API responde `401 Unauthorized`. En el frontend la cookie es `HttpOnly`, `SameSite=Strict` y expira tras 25 minutos de inactividad; ante un `401` de la API el frontend cierra la sesión y redirige al login.

## Resumen de endpoints

| Método | Ruta                                    | Auth | Historia      | Descripción                                                  |
|--------|-----------------------------------------|------|---------------|--------------------------------------------------------------|
| POST   | `/api/Auth/login`                       | No   | HU1           | Autentica un usuario y devuelve sus datos y el token         |
| GET    | `/api/Laboratorios`                     | Sí   | HU2           | Lista los laboratorios                                       |
| GET    | `/api/Laboratorios/{id}`                | Sí   | HU2           | Obtiene un laboratorio (selección para reservar)             |
| GET    | `/api/Laboratorios/{id}/disponibilidad` | Sí   | HU3           | Consulta si un laboratorio está disponible en un horario     |
| GET    | `/api/Laboratorios/{id}/reservas`       | Sí   | HU3           | Reservas activas de un laboratorio en un rango (bloques ocupados) |
| POST   | `/api/Reservas`                         | Sí   | HU4, HU5, HU6 | Registra una reserva validando horario y conflictos          |
| GET    | `/api/Reservas/mias`                    | Sí   | **HU7**       | Lista las reservas (activas y canceladas) del usuario autenticado |
| GET    | `/api/Reservas/todas`                   | Sí (Administrador) | **HU7/HU8** | Lista las reservas de todos los usuarios (para que el administrador cancele cualquiera) |
| POST   | `/api/Reservas/{id}/cancelar`           | Sí   | **HU8**       | Cancela una reserva propia que aún no ha iniciado (el administrador, cualquiera) |

## Decisiones de diseño y deuda técnica

- **Contraseñas con hash BCrypt** (`BCrypt.Net-Next`), verificadas con `BCrypt.Verify`. Todo usuario nuevo debe guardarse hasheado.
- **Secretos en el repositorio**: `Jwt:Key` y la contraseña de la base de datos están en `appsettings.json`. Antes de un ambiente productivo deben moverse a *user-secrets*, variables de entorno o un gestor de secretos.
- **DTOs separados del modelo**: las respuestas no exponen entidades de EF Core, para no filtrar campos internos o sensibles.
- **Reglas de negocio en la API**: el frontend solo guía al usuario; cualquier cliente de la API queda sujeto a las mismas reglas.
- **El usuario siempre sale del token**, nunca del cuerpo ni de la URL (reservar, consultar "mis reservas" y cancelar).
- **Autorización por rol**: el token transporta el rol. Desde el Sprint 4 se aplica en dos puntos: `GET /api/Reservas/todas` exige el rol `Administrador` y `POST /api/Reservas/{id}/cancelar` permite al administrador cancelar reservas ajenas. El resto de los endpoints solo exige estar autenticado.

## Evolución por sprint

| Tema         | Sprint 1             | Sprint 2                                                  | Sprint 3                                                       | Sprint 4                                                              |
|--------------|----------------------|-----------------------------------------------------------|----------------------------------------------------------------|-----------------------------------------------------------------------|
| Alcance      | HU1: login           | + HU2 (laboratorios) y HU3 (disponibilidad)               | + HU4 (reservar), HU5 (validar horario) y HU6 (conflictos)     | + **HU7** (mis reservas) y **HU8** (cancelar reserva)                 |
| Seguridad    | Sin JWT; contraseñas en texto plano | JWT, `[Authorize]` en la API y BCrypt        | Los endpoints de reservas también exigen JWT                   | Primer uso del rol: `todas` solo para `Administrador`; `mias` y `cancelar` usan el id del token |
| Frontend     | Plantilla MVC        | Login, listado y tabla de disponibilidad                  | Reservar desde la tabla; pantalla "Mis reservas" básica        | "Mis reservas" con pestañas Próximas/Historial, cancelación con confirmación y vista "todas" para el administrador |
| Endpoints    | `POST /Auth/login`   | + 4 endpoints de `Laboratorios`                           | + `POST /Reservas` y `GET /Reservas/mias`                      | + `POST /Reservas/{id}/cancelar` y `GET /Reservas/todas` (admin); `mias` se formaliza como HU7 |
| Pruebas      | Manuales             | Manuales                                                  | Manuales + xUnit (login)                                       | Manuales + xUnit: 45 pruebas (login, laboratorios, crear y cancelar)  |
| Ejecución    | Frontend por aparte  | Perfil de arranque simultáneo                             | Sin cambios                                                    | Pipeline de CI en Azure Pipelines con SonarQube Cloud                 |

---

# HU1 - Inicio de sesión

## i. Definición de hecho (HU1)

Para considerar terminada la funcionalidad de **Login (AB#1)**:

- Existe el modelo de datos (`Usuario`, `Rol`) y la base `ReservasLaboratorios` se crea con el script versionado (`database/01_create_database.sql`), con sus restricciones.
- La `WebAPI` se conecta a SQL Server mediante Entity Framework Core (`ReservasDBContext`).
- `POST /api/Auth/login` valida que usuario y contraseña no vengan vacíos, verifica las credenciales contra el hash BCrypt, verifica que el usuario esté activo y devuelve sus datos básicos con un token JWT.
- Responde con el código HTTP adecuado en cada escenario (petición inválida, credenciales incorrectas, usuario inactivo, éxito) usando DTOs (`LoginRequest`, `LoginResponse`).
- El frontend consume el API, conserva la sesión con una cookie y reenvía el JWT (ver [Autenticación y autorización](#autenticación-y-autorización-jwt)).
- Se documentaron y ejecutaron casos de prueba positivos, neutros y negativos (ver iv).

## ii. Notas de la solución (HU1)

- **Flujo**: `AccountController` → `POST api/auth/login` → `AuthController` → `ReservasDBContext` → `Usuarios` (con su `Rol`) → `TokenService` genera el JWT.
- **Orden de validaciones**:

| Orden | Validación                                  | Respuesta                                                      |
|-------|---------------------------------------------|----------------------------------------------------------------|
| 1     | Usuario o contraseña vacíos                 | `400` — `"Usuario y contraseña son obligatorios."`             |
| 2     | Usuario inexistente o contraseña incorrecta | `401` — `"Credenciales incorrectas."`                          |
| 3     | Usuario con `Activo = 0`                    | `401` — `"El usuario se encuentra inactivo."`                  |
| 4     | Credenciales válidas                        | `200` — `LoginResponse` con `token`                            |

- **Frontend**: `LoginViewModel` marca ambos campos como obligatorios; cualquier respuesta no exitosa se muestra como `"Usuario o contraseña incorrectos"`. El login es la página inicial y, si ya hay sesión, redirige al inicio. El campo de contraseña tiene un botón (ojo) para mostrar u ocultar lo digitado.

## iii. Implementación (HU1)

Pasos para levantar el proyecto en [Configuración inicial](#configuración-inicial) y [Ejecución](#ejecución).

```http
POST /api/Auth/login
Content-Type: application/json

{ "nombreUsuario": "user01", "password": "Laboratorios123#" }
```

Respuesta (`200 OK`):

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

Ejecutadas en el API (Scalar) y en el Frontend. Además existen 8 pruebas automatizadas (`AuthController_Tests`).

| Caso | Tipo     | Entrada                                         | Resultado esperado                                                                 | Resultado |
|------|----------|-------------------------------------------------|------------------------------------------------------------------------------------|-----------|
| 1    | Positivo | API: `user01` / `Laboratorios123#` (activo)     | `200` con `usuarioId`, `nombreUsuario`, `nombreCompleto`, `rol = "Usuario"`, `token` | ✔ Coincide |
| 2    | Positivo | Frontend: mismas credenciales                   | Redirige a la página de inicio (listado de laboratorios)                           | ✔ Coincide |
| 3    | Neutro   | API: usuario y contraseña vacíos                | `400` — `"Usuario y contraseña son obligatorios."`, sin consultar la BD            | ✔ Coincide |
| 4    | Neutro   | Frontend: campos vacíos                         | Cada campo muestra que es obligatorio                                              | ✔ Coincide |
| 5    | Negativo | API: contraseña incorrecta (o usuario inactivo) | `401` — `"Credenciales incorrectas."` (o `"El usuario se encuentra inactivo."`)    | ✔ Coincide |
| 6    | Negativo | Frontend: contraseña incorrecta                 | Mensaje `"Usuario o contraseña incorrectos"`                                       | ✔ Coincide |

---

# HU2 y HU3 - Visualización y consulta de disponibilidad de laboratorios

En **HU2** el usuario ve el listado de laboratorios y selecciona uno; en **HU3** consulta si está disponible en una fecha y horario, y puede continuar con la reserva (HU4).

## i. Definición de hecho (HU2 y HU3)

### HU2 - Visualización de laboratorios

- `GET /api/Laboratorios` lista los laboratorios (sin los dados de baja lógica, `Active = 0`), con nombre, ubicación, capacidad, estado y horario de operación.
- Cada laboratorio indica su estado (`Habilitado` / `FueraDeServicio`); los fuera de servicio también se listan, identificados como tales.
- `GET /api/Laboratorios/{id}` permite seleccionar un laboratorio; su `id` se usa para consultar disponibilidad y reservar.
- **Frontend**: tarjetas con imagen, datos y estado visible (`Disponible` / `Fuera de servicio`), buscador por nombre y vista en cartas o lista. "Ver disponibilidad" para los habilitados; deshabilitado ("No disponible") para los fuera de servicio, con redirección y aviso si se accede por URL.

### HU3 - Consulta de disponibilidad

- `GET /api/Laboratorios/{id}/disponibilidad` recibe `fecha`, `horaInicio` y `horaFin`, valida el horario (`400` si es inválido), compara contra las reservas del laboratorio y responde `disponible: true | false` con un `mensaje`.
- Las reservas `Cancelada` no afectan la disponibilidad.
- **Frontend**: tabla semanal (lunes a viernes, bloques de una hora) con `Disponible` / `Ocupado`, navegación entre semanas (sin retroceder antes de la actual) y leyenda. Al elegir un bloque se muestra el resumen y se continúa con HU4.

## ii. Notas de la solución (HU2 y HU3)

- **Modelo**: `Laboratorio` (`LaboratorioId`, `Nombre`, `Ubicacion`, `Capacidad`, `Estado`, `HoraApertura`, `HoraCierre`, `Active`) y `Reserva`. Restricciones: `CK_Lab_Estado`, `CK_Lab_Horario`, `CK_Res_Estado`.
- **DTOs**: `LaboratorioResponse`, `DisponibilidadResponse` (`disponible`, `mensaje`) y `ReservaOcupadaResponse` (`fecha`, `horaInicio`, `horaFin`).
- **Decisiones**:
  - Solo cuentan las reservas con `Estado = 'Activa'`; la reserva es de un solo día.
  - "No disponible" es un resultado válido (`200`); los `4xx` se reservan para errores de validación.
  - La tabla semanal se pinta con una sola llamada a `/reservas` por semana, no consultando celda por celda. `/disponibilidad` conserva las reglas completas y `POST /api/Reservas` las vuelve a aplicar.
  - El frontend no abre la tabla de un laboratorio fuera de servicio.
- **Observaciones**: la validación de "fecha pasada" usa la fecha del servidor (`DateTime.Today`); `/reservas` no valida estado ni horario del laboratorio; `Reservas.Active` no se usa en la consulta de conflictos.

## iii. Implementación (HU2 y HU3)

Todos los endpoints requieren `Authorization: Bearer <token>`.

| Método | Ruta                                    | Descripción                                                        |
|--------|-----------------------------------------|--------------------------------------------------------------------|
| GET    | `/api/Laboratorios`                     | Lista los laboratorios                                             |
| GET    | `/api/Laboratorios/{id}`                | Obtiene un laboratorio                                             |
| GET    | `/api/Laboratorios/{id}/disponibilidad` | Parámetros `fecha` (`2026-12-10`), `horaInicio` y `horaFin` (`09:00`) |
| GET    | `/api/Laboratorios/{id}/reservas`       | Parámetros `desde` y `hasta`; devuelve las reservas activas del rango |

**Guía para Scalar**: (1) hacer login en `/api/Auth/login` y copiar el `token`; (2) en el endpoint deseado, completar el `id` y, en los encabezados, agregar `authorization` con el valor `Bearer <token>` (se repite en cada endpoint).

## iv. Pruebas (HU2 y HU3)

Ejecutadas contra el API (Scalar) y el Frontend. Además hay pruebas automatizadas en `LaboratoriosController_GETS_Tests` y `LaboratoriosController_ConsDisponibilidad_Tests` (8 cada una).

| Caso   | Tipo     | Entrada / precondición                                              | Resultado esperado                                                          | Resultado |
|--------|----------|---------------------------------------------------------------------|-----------------------------------------------------------------------------|-----------|
| HU2-1  | Positivo | `GET /api/Laboratorios` con token                                   | `200` con los 3 laboratorios y su estado                                    | ✔ Coincide |
| HU2-2  | Positivo | `GET /api/Laboratorios/1`                                           | `200` con los datos de Lab Computo 1                                        | ✔ Coincide |
| HU2-3  | Negativo | `GET /api/Laboratorios` sin token                                   | `401 Unauthorized`                                                          | ✔ Coincide |
| HU3-1  | Positivo | Lab Computo 1, fecha futura sin reservas                            | `200`, `disponible = true`                                                  | ✔ Coincide |
| HU3-2  | Positivo | Horario que empieza justo cuando termina otra reserva               | `disponible = true` (límite exclusivo)                                      | ✔ Coincide |
| HU3-3  | Positivo | Horario con una reserva **cancelada**                               | `disponible = true`                                                         | ✔ Coincide |
| HU3-4  | Negativo | Horario traslapado con una reserva activa                           | `200`, `disponible = false` y mensaje de no disponible                      | ✔ Coincide |
| HU3-5  | Negativo | Lab Electrónica (`FueraDeServicio`)                                 | `200`, `disponible = false`, `"El laboratorio se encuentra fuera de servicio."` | ✔ Coincide |
| HU3-6  | Neutro   | Fecha pasada                                                        | Mensaje `"No se puede consultar disponibilidad en fechas pasadas."`         | ✔ Coincide |
| HU3-7  | Negativo | Laboratorio inexistente                                             | `404` — `"Laboratorio no encontrado."` (se verifica antes que la fecha)     | ✔ Coincide |

---

# HU4, HU5 y HU6 - Registro de reservas

Las tres historias comparten un flujo y el endpoint `POST /api/Reservas`: **HU4** registra la reserva, **HU5** valida el horario y **HU6** evita conflictos. En el frontend se resuelven desde la tabla de disponibilidad.

## i. Definición de hecho (HU4, HU5 y HU6)

- **HU4**: la reserva se registra con laboratorio, fecha, hora de inicio y fin; el **responsable es el usuario autenticado** (id tomado del JWT, no del cuerpo). Si faltan datos, `400` indicando cuáles. Al registrar responde `201` con `"Reserva registrada correctamente."` y el frontend lo muestra sobre la tabla (el bloque pasa a `Ocupado`).
- **HU5**: se validan todas las reglas "sin excepción" y se informan juntas (`mensaje` y `errores`, `400`): hora final posterior a la inicial, fecha no anterior a hoy, dentro del horario de operación del laboratorio y duración máxima de 4 horas.
- **HU6**: no se registra una reserva que se traslape (parcial o total) con otra **activa** del mismo laboratorio y fecha; responde `409` con `"El laboratorio no se encuentra disponible en el horario solicitado."`. No generan conflicto los horarios contiguos, las reservas canceladas ni el mismo horario en otro laboratorio.

## ii. Notas de la solución (HU4, HU5 y HU6)

- **Flujo**: tabla de disponibilidad → `POST /Reservas/Crear` (frontend, con token antifalsificación) → `POST api/Reservas` (JWT agregado por `JWTAuthHandler`) → `ReservasController` (API) → `Reservas`. El resultado se guarda en `TempData` y se redirige a la misma semana.
- **Orden de validaciones** de `POST /api/Reservas`:

| Orden | Validación                                                                     | Respuesta                                                   |
|-------|--------------------------------------------------------------------------------|-------------------------------------------------------------|
| 1     | Token sin id de usuario válido                                                 | `401`                                                       |
| 2     | Falta laboratorio, fecha, hora de inicio u hora de finalización                | `400` — `"Debe indicar ..."` (lista lo que falta)           |
| 3     | Laboratorio inexistente o dado de baja                                         | `404` — `"Laboratorio no encontrado."`                      |
| 4     | Reglas de horario de HU5 (acumulativas)                                        | `400` — `mensaje` y `errores`                               |
| 5     | Laboratorio `FueraDeServicio`                                                  | `409` — `"El laboratorio se encuentra fuera de servicio."`  |
| 6     | Traslape con una reserva activa (HU6)                                          | `409` — `"El laboratorio no se encuentra disponible en el horario solicitado."` |
| 7     | Todo correcto                                                                  | `201` — `ReservaResponse`                                   |

- **DTOs**: `CrearReservaRequest` (campos opcionales para distinguir "no enviado" de un valor por defecto) y `ReservaResponse`.
- **Decisiones**: la verificación de traslape y el `INSERT` van en una transacción `Serializable` (dos usuarios no pueden reservar el mismo espacio a la vez); `409` en lugar de `200` porque aquí la acción no se puede completar; la API es la fuente de verdad (el frontend limita la hora final a 1–4 horas sin pasar del cierre, pero todo se revalida); la duración máxima está en la constante `DuracionMaximaHoras`.
- **Observaciones**: hoy se acepta una reserva cuya hora ya pasó (solo se compara la fecha); la tabla no deshabilita los días anteriores a hoy en la semana en curso (la API rechaza y se muestra el mensaje); `DateTime.Today` y `DateTime.Now` usan la hora del servidor sin zonas horarias.

## iii. Implementación (HU4, HU5 y HU6)

```http
POST /api/Reservas
Authorization: Bearer <token>
Content-Type: application/json

{ "laboratorioId": 1, "fecha": "2026-12-10", "horaInicio": "09:00:00", "horaFin": "11:00:00" }
```

Respuesta (`201 Created`):

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

Rechazo por reglas de horario (`400`) y por conflicto (`409`):

```json
{ "mensaje": "No se pueden realizar reservas en fechas anteriores a la fecha actual. Una reserva no puede superar las 4 horas.",
  "errores": [ "No se pueden realizar reservas en fechas anteriores a la fecha actual.", "Una reserva no puede superar las 4 horas." ] }
```
```json
{ "mensaje": "El laboratorio no se encuentra disponible en el horario solicitado." }
```

**Desde Scalar**: login → agregar `authorization: Bearer <token>` → `POST /api/Reservas` con `laboratorioId`, `fecha`, `horaInicio` y `horaFin` (el usuario se toma del token) → revisar el resultado y consultar `GET /api/Reservas/mias`.

**Desde el frontend**: iniciar sesión → "Ver disponibilidad" de un laboratorio habilitado → clic en un bloque `Disponible` → elegir la hora de finalización (1 a 4 horas después, sin pasar del cierre) → "Reservar" → revisar el aviso y la reserva en "Mis reservas".

## iv. Pruebas (HU4, HU5 y HU6)

Ejecutadas contra el API (Scalar) y el Frontend con fechas futuras (diciembre de 2026); cuando se necesita una reserva previa se registra con el mismo endpoint. Además hay 10 pruebas automatizadas en `ReservasController_Crear_Tests`.

| Caso   | Tipo     | Entrada / precondición                                                      | Resultado esperado                                                               | Resultado |
|--------|----------|-----------------------------------------------------------------------------|----------------------------------------------------------------------------------|-----------|
| HU4-1  | Positivo | `POST` válido: Lab 1, 2026-12-10, 09:00–11:00                               | `201`, `estado = "Activa"`, mensaje de confirmación                              | ✔ Coincide |
| HU4-2  | Positivo | Frontend: bloque `Disponible` → fin 12:00 → "Reservar"                      | Aviso de confirmación, bloques `Ocupado` y reserva en "Próximas"                 | ✔ Coincide |
| HU4-3  | Positivo | `user02` reserva Lab Redes; luego `/mias` con cada token                    | `201`; aparece en `/mias` de `user02` y no en el de `user01`                     | ✔ Coincide |
| HU4-4  | Negativo | `POST` con solo `laboratorioId`                                             | `400` — `"Debe indicar la fecha, la hora de inicio, la hora de finalización."`   | ✔ Coincide |
| HU4-5  | Negativo | Laboratorio 55 (inexistente)                                                | `404` — `"Laboratorio no encontrado."`                                           | ✔ Coincide |
| HU4-6  | Negativo | `POST` sin token                                                            | `401 Unauthorized`                                                               | ✔ Coincide |
| HU5-1  | Negativo | Hora final anterior o igual a la inicial                                    | `400` — `"La hora final debe ser posterior a la hora inicial."`                  | ✔ Coincide |
| HU5-2  | Negativo | Fecha pasada                                                                | `400` — `"No se pueden realizar reservas en fechas anteriores a la fecha actual."` | ✔ Coincide |
| HU5-3  | Negativo | Lab Redes de 07:00 a 09:00 (abre 08:00)                                     | `400` — `"El laboratorio solo puede reservarse entre 08:00 y 20:00."`            | ✔ Coincide |
| HU5-4  | Negativo | Reserva de 5 horas                                                          | `400` — `"Una reserva no puede superar las 4 horas."`                             | ✔ Coincide |
| HU5-5  | Positivo | Reserva de exactamente 4 horas (09:00–13:00)                                | `201` (límite inclusivo)                                                         | ✔ Coincide |
| HU5-6  | Negativo | Fecha pasada y 5 horas a la vez                                             | `400` con ambas razones en `mensaje` y `errores`                                 | ✔ Coincide |
| HU5-7  | Neutro   | Frontend: lista "Hora de finalización" con inicio 09:00 y 19:00 (Lab Redes) | De 10:00 a 13:00 (máx. 4 h) y solo 20:00 (no pasa del cierre)                    | ✔ Coincide |
| HU5-8  | Negativo | Frontend: bloque de un día anterior a hoy en la semana en curso             | Aviso en rojo de fecha anterior; no se registra                                  | ✔ Coincide |
| HU6-1  | Negativo | Mismo laboratorio y mismo horario que una reserva activa                    | `409` — no disponible; sin nueva reserva                                         | ✔ Coincide |
| HU6-2  | Negativo | Traslape al inicio, al final, contenido y que la cubre (reserva base 09–11) | Las cuatro responden `409`                                                       | ✔ Coincide |
| HU6-3  | Positivo | Horario contiguo (11:00–13:00)                                              | `201`                                                                            | ✔ Coincide |
| HU6-4  | Positivo | Mismo horario en otro laboratorio                                           | `201`                                                                            | ✔ Coincide |
| HU6-5  | Positivo | Horario de una reserva **cancelada**                                        | `201`                                                                            | ✔ Coincide |
| HU6-6  | Negativo | Frontend: la hora final cruza un bloque ocupado                             | Aviso en rojo de no disponible; la tabla conserva el bloque `Ocupado`            | ✔ Coincide |

---

# HU7 y HU8 - Consultar y cancelar reservas (Sprint 4)

Ambas historias comparten la pantalla **"Mis reservas"** (menú superior): en **HU7** el usuario consulta las reservas que ha realizado y en **HU8** cancela una de ellas para liberar el horario. Trazabilidad: **PBI 7 (HU7)** y **PBI 8 (HU8)** en Azure Boards, iteración *Sistema de Reservas\Sprint 4*, con sus criterios de aceptación y los commits referenciando `AB#7` / `AB#8`.

## i. Definición de hecho (HU7 y HU8)

Una historia se considera **Hecha** cuando cumple, además de sus criterios de aceptación, estos puntos comunes del equipo:

- El código está en la rama `main` de Azure Repos y compila sin errores.
- Existe interfaz web que permite realizar la función (no solo el endpoint) y la API aplica las reglas de negocio.
- Las pruebas unitarias pasan en el pipeline y se ejecutaron los casos manuales documentados en [iv. Pruebas](#iv-pruebas-hu7-y-hu8).
- El PBI está en estado *Done* y los commits referencian el PBI (`AB#7`, `AB#8`).
- El README está actualizado y la funcionalidad se puede demostrar.

### HU7 - Consultar mis reservas

- **Mostrar según usuario**: `GET /api/Reservas/mias` devuelve únicamente las reservas del usuario autenticado; su id sale del token, nunca de la URL. En el frontend, "Mis reservas" muestra solo esas reservas.
- **Información mostrada**: laboratorio (con su ubicación), fecha, hora inicial, hora final, duración y estado de la reserva.
- **Filtrar por estado**: pestañas **Próximas** (activas que aún no terminan) e **Historial** (finalizadas y canceladas), con contador, y una etiqueta de estado en cada fila (*Próxima*, *En curso*, *Finalizada*, *Cancelada*), de modo que se identifican claramente las activas y las canceladas.
- Si no hay reservas en la pestaña, se muestra un mensaje en lugar de una tabla vacía.
- **Vista de administrador**: el usuario con rol `Administrador` conserva su "Mis reservas" y dispone del enlace **"Ver todas las reservas"**, que consulta `GET /api/Reservas/todas` y agrega la columna *Usuario*. Un usuario normal no puede acceder a ese endpoint (`403`).

### HU8 - Cancelar una reserva

- **Selección**: en la pestaña "Próximas", cada reserva cancelable tiene el botón **Cancelar** que abre una confirmación para esa reserva (con un motivo opcional de hasta 255 caracteres).
- **Eliminación propia**: para un usuario normal, `POST /api/Reservas/{id}/cancelar` solo cancela sus propias reservas; si pertenece a otro usuario responde `404`, igual que si no existiera, y no se modifica.
- **Eliminación por Admin**: el usuario con rol `Administrador` puede cancelar cualquier reserva. Desde "Ver todas las reservas" elige la reserva y la cancela igual que una propia; la cancelación queda registrada con el id del administrador en `CanceladaPorId`.
- **Liberación de horario**: al cancelar, `Reservas.Estado` pasa a `Cancelada` y se registra la cancelación en la tabla `Cancelaciones` (quién, cuándo y motivo) en una sola operación. Como la disponibilidad y los conflictos solo consideran reservas `Activa`, otro usuario puede reservar ese horario de inmediato.
- **Alerta de confirmación**: antes de cancelar el sistema pide confirmar ("¿Cancelar la reserva de …?") y, al terminar, muestra `"Reserva cancelada correctamente."`; si no se pudo, muestra el motivo.
- **Reglas adicionales** (también para el administrador): no se puede cancelar una reserva ya cancelada (`409`) ni una que ya inició o finalizó (`409`).

## ii. Notas de la solución (HU7 y HU8)

- **Flujo de consulta**: `ReservasController.Index` (frontend) → `GET api/Reservas/mias` (JWT agregado por `JWTAuthHandler`) → `ReservasController.GetMias` (API) → `Reservas` + `Laboratorios`. Resultado ordenado de la más reciente a la más antigua; el frontend la separa en Próximas e Historial. Si el usuario es administrador y elige "Ver todas las reservas" (`?todas=true`), el frontend llama a `GET api/Reservas/todas` (`GetTodas`, `[Authorize(Roles = "Administrador")]`), que además une `Usuarios` para devolver el nombre del responsable.
- **Flujo de cancelación**: botón Cancelar → `POST /Reservas/Cancelar` (frontend, con token antifalsificación) → `POST api/Reservas/{id}/cancelar` → `ReservasController.Cancelar` (API) → `Reservas` y `Cancelaciones`. El resultado va a `TempData` y se redirige a "Mis reservas" conservando la pestaña activa.
- **Orden de validaciones** de `POST /api/Reservas/{id}/cancelar`:

| Orden | Validación                                                         | Respuesta                                                              |
|-------|--------------------------------------------------------------------|------------------------------------------------------------------------|
| 1     | Token sin id de usuario válido                                     | `401 Unauthorized`                                                     |
| 2     | Motivo de más de 255 caracteres                                    | `400` — `"El motivo no puede superar los 255 caracteres."`             |
| 3     | Reserva inexistente, dada de baja o de otro usuario (salvo que quien cancela sea `Administrador`) | `404` — `"Reserva no encontrada."`                  |
| 4     | Reserva ya cancelada                                               | `409` — `"La reserva ya se encuentra cancelada."`                      |
| 5     | Reserva que ya inició o finalizó                                   | `409` — `"No se puede cancelar una reserva que ya inició o finalizó."` |
| 6     | Todo correcto                                                      | `200` — `CancelarReservaResponse`                                      |

### DTOs

| DTO                       | Campos                                                                                                 | Uso                                  |
|---------------------------|--------------------------------------------------------------------------------------------------------|--------------------------------------|
| `MiReservaResponse`       | `reservaId`, `laboratorioId`, `laboratorio`, `ubicacion`, `fecha`, `horaInicio`, `horaFin`, `estado`, `fechaRegistro`, `usuario` (solo lo llena `/todas`) | Respuesta de `GET /api/Reservas/mias` (HU7) y `/todas` |
| `CancelarReservaRequest`  | `motivo` (opcional)                                                                                    | Cuerpo de `POST /api/Reservas/{id}/cancelar` |
| `CancelarReservaResponse` | `reservaId`, `estado`, `fechaCancelacion`, `mensaje`                                                   | Respuesta de la cancelación (HU8)    |

### Decisiones de diseño

- **Usuario tomado del token**: el contrato no incluye el usuario, así que nadie puede consultar ni cancelar reservas a nombre de otro.
- **Reserva ajena = `404`**: para un usuario normal no se revela qué ids existen.
- **Excepción por rol**: la regla de propiedad se evalúa con `User.IsInRole("Administrador")`; el rol viaja en el JWT (`ClaimTypes.Role`) y no en el cuerpo de la petición, así que no se puede falsificar desde el cliente. El administrador sigue sujeto a las reglas de estado y de tiempo.
- **Vista de todas separada de "Mis reservas"**: `/mias` siempre devuelve solo las reservas propias (criterio de HU7); la vista completa del administrador es un endpoint aparte.
- **Estados derivados**: la BD solo guarda `Activa` o `Cancelada`; *Próxima*, *En curso* y *Finalizada* se deducen comparando fecha y hora de la reserva con la actual (`MiReserva.ObtenerEstado`). El botón Cancelar solo aparece si la reserva está activa y aún no inicia (`PuedeCancelar`); la API lo vuelve a validar.
- **Cancelación atómica**: el cambio de estado y el registro en `Cancelaciones` se guardan con un único `SaveChanges`; se guardan juntos o no se guardan.
- **Un registro de cancelación por reserva** (`UQ_Can_Reserva`): si ya existía uno (por ejemplo, el dato semilla) se actualiza en lugar de insertar otro.
- **Confirmación en la interfaz**: se usa un bloque desplegable con el detalle de la reserva y el campo de motivo, sin depender de JavaScript.

### Observaciones y deuda técnica (HU7 y HU8)

- **Administrador y reservas en curso**: el administrador tampoco puede cancelar una reserva que ya inició. Si el negocio lo requiere, habría que relajar esa regla solo para ese rol.
- **Solo se cancelan reservas futuras**: una reserva en curso no se puede cancelar (decisión de negocio asumida; ajustable si el cliente indica lo contrario).
- **Datos semilla**: la reserva 1 tiene una cancelación registrada pero su `Estado` sigue `Activa`; como su fecha ya pasó, el frontend la muestra como *Finalizada*. Las reservas semilla no sirven para probar cancelaciones por estar en el pasado.
- **Fecha y hora del servidor**: `DateTime.Now` se evalúa en el servidor, sin zonas horarias.
- **`[Authorize]` desactivado** en `LaboratoriosController` del frontend (el acceso sigue protegido porque la API responde `401` sin sesión válida); conviene reactivarlo.

## iii. Implementación (HU7 y HU8)

La configuración y ejecución están en [Información general del proyecto](#información-general-del-proyecto). Ambos endpoints requieren `Authorization: Bearer <token>`. Para el despliegue local basta levantar API y frontend juntos (perfil de arranque simultáneo) y, en integración continua, el pipeline compila y ejecuta las pruebas con cada push a `main`.

### Endpoints

| Método | Ruta                          | Historia | Descripción                                                                  |
|--------|-------------------------------|----------|------------------------------------------------------------------------------|
| GET    | `/api/Reservas/mias`          | HU7      | Lista las reservas (activas y canceladas) del usuario autenticado, de la más reciente a la más antigua |
| GET    | `/api/Reservas/todas`         | HU7/HU8  | Solo `Administrador`: lista las reservas de todos los usuarios con el nombre del responsable |
| POST   | `/api/Reservas/{id}/cancelar` | HU8      | Cancela una reserva propia que aún no ha iniciado (el administrador, cualquiera); el cuerpo con `motivo` es opcional |

Ejemplo de cancelación:

```http
POST /api/Reservas/4/cancelar
Authorization: Bearer <token>
Content-Type: application/json

{ "motivo": "Ya no necesito el laboratorio" }
```

Respuesta exitosa (`200 OK`):

```json
{
  "reservaId": 4,
  "estado": "Cancelada",
  "fechaCancelacion": "2026-11-08T18:30:00",
  "mensaje": "Reserva cancelada correctamente."
}
```

Ejemplo de rechazo (`409 Conflict`): `{ "mensaje": "La reserva ya se encuentra cancelada." }`

### Guía para probar en Scalar

1. Iniciar sesión en `/api/Auth/login` con `user01` y copiar el `token`.
2. Registrar una reserva futura con `POST /api/Reservas` (por ejemplo, Lab 1, 2026-12-10, 09:00–11:00) y anotar su `reservaId`.
3. Consultar `GET /api/Reservas/mias` y comprobar que aparece con `estado = "Activa"` (HU7).
4. Cancelar con `POST /api/Reservas/{id}/cancelar` y volver a consultar `/mias`: el estado es `Cancelada` (HU8).
5. Repetir el `POST /api/Reservas` del paso 2: ahora responde `201`, porque el horario quedó libre.

6. Como administrador: iniciar sesión con `Admi`, consultar `GET /api/Reservas/todas` y cancelar con `POST /api/Reservas/{id}/cancelar` una reserva de `user01`: responde `200`.

En todas las peticiones se agrega el encabezado `authorization` con el valor `Bearer <token>`.

### Guía para probar desde el frontend

1. Iniciar sesión y registrar una reserva futura desde "Ver disponibilidad" (HU4).
2. Abrir **Mis reservas** en el menú superior: la reserva aparece en **Próximas** con su laboratorio, fecha, horario, duración y estado (HU7). Las finalizadas y canceladas están en **Historial**.
3. Presionar **Cancelar** en la fila, escribir un motivo (opcional) y confirmar con **"Sí, cancelar reserva"** (HU8).
4. La página muestra `"Reserva cancelada correctamente."` y la reserva pasa al **Historial** como *Cancelada*.
5. Volver a la tabla de disponibilidad del laboratorio: el bloque está otra vez `Disponible`.
6. Como administrador (`Admi`): en "Mis reservas" presionar **"Ver todas las reservas"**, localizar una reserva de otro usuario (columna *Usuario*) y cancelarla con el mismo procedimiento.

## iv. Pruebas (HU7 y HU8)

### Pruebas automatizadas (xUnit)

`cd WebApi_Test && dotnet test` ejecuta 45 pruebas con base en memoria, sin necesidad de SQL Server. Las de este sprint cubren la cancelación en `ReservasController_Cancelar_Tests`:

| Prueba                                          | Verifica                                                                  |
|-------------------------------------------------|---------------------------------------------------------------------------|
| `Cancelar_HappyCase`                            | Cancela una reserva propia futura: `200`, estado `Cancelada` y registro en `Cancelaciones` con el motivo (sin espacios sobrantes) |
| `Cancelar_SinMotivo_Funciona`                   | La cancelación sin motivo funciona y guarda el motivo nulo                |
| `Cancelar_ReservaDeOtroUsuario_NoEncontrada`    | `404`; la reserva sigue `Activa` y no se crea cancelación                 |
| `Cancelar_ReservaInexistente_NoEncontrada`      | `404` con un id inexistente                                               |
| `Cancelar_YaCancelada_Conflicto`                | `409` al cancelar dos veces                                               |
| `Cancelar_ReservaPasada_Conflicto`              | `409`; la reserva pasada sigue `Activa`                                   |
| `Cancelar_MotivoDemasiadoLargo_BadRequest`      | `400` con motivo de 256 caracteres; la reserva no cambia                  |
| `Cancelar_SinToken_NoAutorizado`                | Sin usuario autenticado, rechaza la solicitud (`401`)                     |
| `Cancelar_Administrador_PuedeCancelarReservaDeOtroUsuario` | El administrador cancela una reserva ajena: `200`, estado `Cancelada` y `CanceladaPorId` = id del administrador |
| `Cancelar_UsuarioNormal_ReservaDeOtro_NoEncontrada` | Un usuario con rol `Usuario` no puede cancelar una reserva ajena: `404` y sigue `Activa` |
| `Cancelar_Administrador_ReservaPasada_Conflicto` | Las reglas de tiempo aplican también al administrador: `409`             |

La consulta de "mis reservas" y de "todas" (HU7) se verifica con los casos manuales siguientes. Pendiente: pruebas unitarias de `GetMias` y `GetTodas`.

### Pruebas manuales

Se usan tokens de `user01`, `user02`, `user03` y `Admi`, y fechas futuras (diciembre de 2026). Antes de las pruebas de cancelación se registra con `POST /api/Reservas` una reserva futura por usuario.

#### HU7 - Consultar mis reservas

| Caso  | Tipo     | Entrada / precondición                                                        | Resultado esperado                                                                                   | Resultado |
|-------|----------|-------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------|-----------|
| HU7-1 | Positivo | `GET /api/Reservas/mias` con el token de `user01` y luego con el de `user02`  | Cada respuesta incluye solo las reservas de su usuario (`user01`: reservas 1 y 3 semilla más las que registre; `user02`: la 2 más las que registre) | ✔ Coincide |
| HU7-2 | Positivo | Frontend: `user01` con una reserva futura abre "Mis reservas"                 | La fila muestra laboratorio, ubicación, fecha, hora inicial, hora final, duración y estado *Próxima* | ✔ Coincide |
| HU7-3 | Positivo | Frontend: `user01` tiene una reserva activa y otra cancelada                  | La activa aparece en **Próximas** y la cancelada en **Historial** con la etiqueta *Cancelada*; los contadores coinciden | ✔ Coincide |
| HU7-4 | Neutro   | Frontend: `user03` (sin reservas) abre "Mis reservas"                         | Se muestra el mensaje de que no hay reservas y el botón para ver laboratorios                        | ✔ Coincide |
| HU7-5 | Negativo | `GET /api/Reservas/mias` sin el encabezado `Authorization`                    | `401 Unauthorized`                                                                                   | ✔ Coincide |
| HU7-6 | Negativo | Frontend: abrir `/Reservas` sin sesión iniciada                               | Redirige al login                                                                                    | ✔ Coincide |
| HU7-7 | Positivo | Administrador (`Admi`): `GET /api/Reservas/todas` o "Ver todas las reservas" en el frontend | Lista las reservas de todos los usuarios con el nombre del responsable; "Mis reservas" sigue mostrando solo las del administrador | ✔ Coincide |
| HU7-8 | Negativo | `user01` (rol Usuario): `GET /api/Reservas/todas`                             | `403 Forbidden`; en el frontend no se muestra el enlace "Ver todas las reservas"                    | ✔ Coincide |

#### HU8 - Cancelar una reserva

| Caso  | Tipo     | Entrada / precondición                                                           | Resultado esperado                                                                                         | Resultado |
|-------|----------|----------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------|-----------|
| HU8-1 | Positivo | `user01` cancela con `POST /api/Reservas/{id}/cancelar` una reserva futura propia, con motivo | `200`, `estado = "Cancelada"`, `"Reserva cancelada correctamente."`; en `/mias` figura `Cancelada` y queda un registro en `Cancelaciones` | ✔ Coincide |
| HU8-2 | Positivo | Frontend: en "Próximas", **Cancelar** → motivo → **"Sí, cancelar reserva"**      | Aviso `"Reserva cancelada correctamente."`; la reserva pasa a **Historial** como *Cancelada*               | ✔ Coincide |
| HU8-3 | Positivo | Tras HU8-1, `user02` intenta reservar el mismo laboratorio, fecha y horario      | `201 Created`: el horario quedó liberado (y en la tabla el bloque vuelve a `Disponible`)                   | ✔ Coincide |
| HU8-4 | Neutro   | Cancelar sin escribir motivo (API sin cuerpo o frontend con el campo vacío)      | La cancelación se realiza igualmente y el motivo queda nulo                                                | ✔ Coincide |
| HU8-5 | Negativo | `user02` intenta cancelar la reserva de `user01`                                 | `404` — `"Reserva no encontrada."`; la reserva de `user01` sigue `Activa`                                  | ✔ Coincide |
| HU8-6 | Negativo | Cancelar una reserva que ya está cancelada                                       | `409` — `"La reserva ya se encuentra cancelada."`                                                          | ✔ Coincide |
| HU8-7 | Negativo | Cancelar una reserva que ya inició o finalizó (por ejemplo, la reserva semilla 3) | `409` — `"No se puede cancelar una reserva que ya inició o finalizó."`; sigue `Activa`                     | ✔ Coincide |
| HU8-8 | Negativo | Motivo de más de 255 caracteres                                                  | `400` — `"El motivo no puede superar los 255 caracteres."`; la reserva no cambia                           | ✔ Coincide |
| HU8-9 | Negativo | Cancelar un id inexistente (por ejemplo, 999) y cancelar sin token               | `404` — `"Reserva no encontrada."`; sin token, `401 Unauthorized`                                          | ✔ Coincide |
| HU8-10 | Neutro  | Frontend: observar las reservas *En curso* y las del Historial                   | No ofrecen el botón Cancelar (la pestaña Historial no tiene la columna de acciones)                        | ✔ Coincide |
| HU8-11 | Positivo | Administrador (`Admi`) cancela una reserva futura de `user01` (API o "Ver todas las reservas") | `200` y `"Reserva cancelada correctamente."`; la reserva pasa a `Cancelada` y `Cancelaciones.CanceladaPorId` es el id del administrador | ✔ Coincide |

> Las pruebas manuales se realizan tanto desde el API (Scalar) como desde el Frontend, para confirmar que las reglas se cumplen en ambas partes.
