CREATE TABLE Laboratorios (
    LaboratorioId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Nombre NVARCHAR(100) NOT NULL,
    Ubicacion NVARCHAR(150) NOT NULL,
    Capacidad INT NOT NULL,
    Estado NVARCHAR(20) NOT NULL,
    HoraApertura TIME NOT NULL,
    HoraCierre TIME NOT NULL,
    Active BIT NOT NULL
);

CREATE TABLE Reservas (
    ReservaId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    LaboratorioId INT NOT NULL,
    UsuarioId INT NOT NULL,
    Fecha DATE NOT NULL,
    HoraInicio TIME NOT NULL,
    HoraFin TIME NOT NULL,
    Estado NVARCHAR(20) NOT NULL,
    FechaRegistro DATETIME2 NOT NULL,
    Active BIT NOT NULL,
    CONSTRAINT FK_Reservas_Laboratorios
        FOREIGN KEY (LaboratorioId) REFERENCES Laboratorios(LaboratorioId)
);
