USE MASTER;
DROP DATABASE IF EXISTS ReservasLaboratorios;
CREATE DATABASE ReservasLaboratorios;
GO

USE ReservasLaboratorios;
GO

-- ------------------------------------------------------------
-- ROLES
-- ------------------------------------------------------------
DROP TABLE IF EXISTS Roles;
CREATE TABLE Roles (
    RolId INT IDENTITY(1,1) NOT NULL,
    NombreRol NVARCHAR(50) NOT NULL,
    Descripcion NVARCHAR(150) NULL,
    CONSTRAINT PK_Roles PRIMARY KEY (RolId),
    CONSTRAINT UQ_Roles_Nombre UNIQUE (NombreRol)
);
GO

-- ------------------------------------------------------------
-- USUARIOS
-- ------------------------------------------------------------
DROP TABLE IF EXISTS Usuarios;
CREATE TABLE Usuarios (
    UsuarioId INT IDENTITY(1,1) NOT NULL,
    NombreUsuario NVARCHAR(50) NOT NULL,
    NombreCompleto NVARCHAR(150) NOT NULL,
    Correo NVARCHAR(150) NOT NULL,
    Contraseña NVARCHAR(255) NOT NULL,
    RolId INT NOT NULL,
    Activo BIT NOT NULL CONSTRAINT DF_Usuarios_Activo DEFAULT (1),
    FechaRegistro DATETIME2 NOT NULL CONSTRAINT DF_Usuarios_Fecha DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_Usuarios PRIMARY KEY (UsuarioId),
    CONSTRAINT UQ_Usuarios_Nombre UNIQUE (NombreUsuario),
    CONSTRAINT UQ_Usuarios_Correo UNIQUE (Correo),
    CONSTRAINT FK_Usuarios_Rol FOREIGN KEY (RolId) REFERENCES Roles(RolId)
);
GO

-- ------------------------------------------------------------
-- LABORATORIOS
-- ------------------------------------------------------------
DROP TABLE IF EXISTS Laboratorios;
CREATE TABLE Laboratorios (
    LaboratorioId INT IDENTITY(1,1) NOT NULL,
    Nombre NVARCHAR(100) NOT NULL,
    Ubicacion NVARCHAR(150) NOT NULL,
    Capacidad INT NOT NULL,
    Estado NVARCHAR(20) NOT NULL CONSTRAINT DF_Lab_Estado DEFAULT ('Habilitado'),
    HoraApertura TIME(0) NOT NULL CONSTRAINT DF_Lab_Apertura DEFAULT ('07:00'),
    HoraCierre TIME(0) NOT NULL CONSTRAINT DF_Lab_Cierre DEFAULT ('22:00'),
    Active BIT NOT NULL CONSTRAINT DF_Lab_Active DEFAULT (1),
    CONSTRAINT PK_Laboratorios PRIMARY KEY (LaboratorioId),
    CONSTRAINT UQ_Lab_Nombre UNIQUE (Nombre),
    CONSTRAINT CK_Lab_Capacidad CHECK (Capacidad > 0),
    CONSTRAINT CK_Lab_Estado CHECK (Estado IN ('Habilitado', 'FueraDeServicio')),
    CONSTRAINT CK_Lab_Horario CHECK (HoraCierre > HoraApertura)
);
GO

-- ------------------------------------------------------------
-- RESERVAS
-- ------------------------------------------------------------
DROP TABLE IF EXISTS Reservas;
CREATE TABLE Reservas (
    ReservaId INT IDENTITY(1,1) NOT NULL,
    LaboratorioId INT NOT NULL,
    UsuarioId INT NOT NULL,
    Fecha DATE NOT NULL,
    HoraInicio TIME(0) NOT NULL,
    HoraFin TIME(0) NOT NULL,
    Estado NVARCHAR(20) NOT NULL CONSTRAINT DF_Res_Estado DEFAULT ('Activa'),
    FechaRegistro DATETIME2 NOT NULL CONSTRAINT DF_Res_Registro DEFAULT (SYSDATETIME()),
    Active BIT NOT NULL CONSTRAINT DF_Res_Active DEFAULT (1),
    CONSTRAINT PK_Reservas PRIMARY KEY (ReservaId),
    CONSTRAINT FK_Reservas_Lab FOREIGN KEY (LaboratorioId) REFERENCES Laboratorios(LaboratorioId),
    CONSTRAINT FK_Reservas_Usr FOREIGN KEY (UsuarioId) REFERENCES Usuarios(UsuarioId),
    CONSTRAINT CK_Res_Estado CHECK (Estado IN ('Activa', 'Cancelada')),
    CONSTRAINT CK_Res_Orden CHECK (HoraFin > HoraInicio),
    CONSTRAINT CK_Res_Duracion CHECK (DATEDIFF(MINUTE, HoraInicio, HoraFin) <= 240)
);
GO

-- ------------------------------------------------------------
-- CANCELACIONES
-- ------------------------------------------------------------
DROP TABLE IF EXISTS Cancelaciones;
CREATE TABLE Cancelaciones (
    CancelacionId INT IDENTITY(1,1) NOT NULL,
    ReservaId INT NOT NULL,
    CanceladaPorId INT NOT NULL,
    FechaCancelacion DATETIME2 NOT NULL CONSTRAINT DF_Can_Fecha DEFAULT (SYSDATETIME()),
    Motivo NVARCHAR(255) NULL,
    Active BIT NOT NULL CONSTRAINT DF_Can_Active DEFAULT (1),
    CONSTRAINT PK_Cancelaciones PRIMARY KEY (CancelacionId),
    CONSTRAINT FK_Can_Reserva FOREIGN KEY (ReservaId) REFERENCES Reservas(ReservaId),
    CONSTRAINT FK_Can_Usuario FOREIGN KEY (CanceladaPorId) REFERENCES Usuarios(UsuarioId),
    CONSTRAINT UQ_Can_Reserva UNIQUE (ReservaId)
);
GO


INSERT INTO Roles (NombreRol, Descripcion)
VALUES 
('Administrador', 'Acceso total al sistema'),
('Usuario', 'Acceso limitado a reservas');

-- ------------------------------------------------------------
-- USUARIOS
-- ------------------------------------------------------------
INSERT INTO Usuarios (NombreUsuario, NombreCompleto, Correo, Contraseña, RolId)
VALUES 
('Admi', 'Administrador General', 'admin@utn.ac.cr', 'Laboratorios123#', 1),
('user01', 'Matthew', 'matthew@utn.ac.cr', 'Laboratorios123#', 2),
('user02', 'Sebas', 'sebastian@utn.ac.cr', 'Laboratorios123#', 2),
('user03', 'Pao', 'hodgson@utn.ac.cr', 'Laboratorios123#', 2);


-- ------------------------------------------------------------
-- LABORATORIOS
-- ------------------------------------------------------------
INSERT INTO Laboratorios (Nombre, Ubicacion, Capacidad, Estado, HoraApertura, HoraCierre, Active)
VALUES
('Lab Computo 1', 'Edificio A - Piso 2', 30, 'Habilitado', '07:00', '22:00', 1),
('Lab Redes', 'Edificio B - Piso 1', 25, 'Habilitado', '08:00', '20:00', 1),
('Lab Electrónica', 'Edificio C - Piso 3', 20, 'FueraDeServicio', '09:00', '18:00', 1);


-- ------------------------------------------------------------
-- RESERVAS
-- ------------------------------------------------------------
-- Reserva activa de 2 horas
INSERT INTO Reservas (LaboratorioId, UsuarioId, Fecha, HoraInicio, HoraFin)
VALUES (1, 2, '2026-09-20', '09:00', '11:00');

-- Reserva activa de 3 horas
INSERT INTO Reservas (LaboratorioId, UsuarioId, Fecha, HoraInicio, HoraFin)
VALUES (2, 3, '2026-09-21', '10:00', '13:00');

-- ------------------------------------------------------------
-- CANCELACIONES
-- ------------------------------------------------------------
-- Cancelación de la primera reserva
INSERT INTO Cancelaciones (ReservaId, CanceladaPorId, Motivo)
VALUES (1, 2, 'El usuario no pudo asistir');

-- Ver todos los roles
SELECT * FROM Roles;

-- Ver todos los usuarios con su rol
SELECT U.UsuarioId, U.NombreUsuario, U.NombreCompleto, U.Correo, R.NombreRol, U.Contraseña, U.FechaRegistro, U.Activo
FROM Usuarios U
INNER JOIN Roles R ON U.RolId = R.RolId;

-- Ver todos los laboratorios
SELECT * FROM Laboratorios;

-- Ver todas las reservas con detalle de usuario y laboratorio
SELECT R.ReservaId, L.Nombre AS Laboratorio, U.NombreUsuario AS Usuario, R.Fecha, R.HoraInicio, R.HoraFin, R.Estado, R.FechaRegistro, R.Active
FROM Reservas R
INNER JOIN Laboratorios L ON R.LaboratorioId = L.LaboratorioId
INNER JOIN Usuarios U ON R.UsuarioId = U.UsuarioId;

-- Ver todas las cancelaciones con detalle de reserva y usuario que canceló
SELECT C.CancelacionId, C.ReservaId, U.NombreUsuario AS CanceladaPor, C.FechaCancelacion, C.Motivo, C.Active
FROM Cancelaciones C
INNER JOIN Usuarios U ON C.CanceladaPorId = U.UsuarioId;
