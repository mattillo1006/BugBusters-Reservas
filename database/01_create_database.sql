/* =========================================================
   Sistema de Gestión de Mantenimiento de Activos
   ISW-621 - Programación en Ambiente Web I
   Motor: SQL Server
   Contenido: base de datos + tablas (sin datos)
   ========================================================= */

USE master;
GO

/* ---------- DROP DATABASE ---------- */
IF DB_ID('FastFixIt') IS NOT NULL
BEGIN
    ALTER DATABASE FastFixIt SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE FastFixIt;
END
GO

CREATE DATABASE FastFixIt;
GO

USE FastFixIt;
GO

/* ---------- DROP TABLES (orden inverso a las dependencias) ---------- */
DROP TABLE IF EXISTS OrderStatusHistory;
DROP TABLE IF EXISTS AssetStatusHistory;
DROP TABLE IF EXISTS OrderTechnicians;
DROP TABLE IF EXISTS OrderSpareParts;
DROP TABLE IF EXISTS MaintenanceOrders;
DROP TABLE IF EXISTS PreventiveMaintenanceRules;
DROP TABLE IF EXISTS SparePartSuppliers;
DROP TABLE IF EXISTS SpareParts;
DROP TABLE IF EXISTS Suppliers;
DROP TABLE IF EXISTS TechnicianSpecialties;
DROP TABLE IF EXISTS Technicians;
DROP TABLE IF EXISTS MaintenanceTypes;
DROP TABLE IF EXISTS Specialties;
DROP TABLE IF EXISTS Assets;
DROP TABLE IF EXISTS AssetCategories;
DROP TABLE IF EXISTS Users;
DROP TABLE IF EXISTS Roles;
GO

/* =========================================================
   1. SEGURIDAD: ROLES Y USUARIOS
   ========================================================= */

CREATE TABLE Roles
(
    RoleId          INT IDENTITY(1,1) NOT NULL,
    Name            NVARCHAR(50)      NOT NULL,
    Description     NVARCHAR(250)     NULL,
    CONSTRAINT PK_Roles PRIMARY KEY (RoleId),
    CONSTRAINT UQ_Roles_Name UNIQUE (Name)
);
GO

CREATE TABLE Users
(
    UserId          INT IDENTITY(1,1) NOT NULL,
    FullName        NVARCHAR(150)     NOT NULL,
    Login           NVARCHAR(100)     NOT NULL,
    Email           NVARCHAR(150)     NOT NULL,
    PasswordHash    NVARCHAR(255)     NOT NULL,
    RoleId          INT               NOT NULL,
    IsActive        BIT               NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
    CreatedAt       DATETIME2(0)      NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSDATETIME()),
    LastAccessAt    DATETIME2(0)      NULL,
    CONSTRAINT PK_Users PRIMARY KEY (UserId),
    CONSTRAINT UQ_Users_Login UNIQUE (Login),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES Roles (RoleId)
);
GO

/* =========================================================
   2. ACTIVOS Y CATEGORÍAS
   ========================================================= */

CREATE TABLE AssetCategories
(
    AssetCategoryId         INT IDENTITY(1,1) NOT NULL,
    Name                    NVARCHAR(100)     NOT NULL,
    Description             NVARCHAR(250)     NULL,
    -- Parámetro de mantenimiento preventivo por defecto de la categoría (sección 7.2 / 7.9)
    ControlVariable         NVARCHAR(50)      NOT NULL,   -- UsageHours | Mileage | Cycles | Days
    MaintenanceThreshold    DECIMAL(18,2)     NOT NULL,
    IsActive                BIT               NOT NULL CONSTRAINT DF_AssetCategories_IsActive DEFAULT (1),
    CONSTRAINT PK_AssetCategories PRIMARY KEY (AssetCategoryId),
    CONSTRAINT UQ_AssetCategories_Name UNIQUE (Name),
    CONSTRAINT CK_AssetCategories_Threshold CHECK (MaintenanceThreshold > 0)
);
GO

CREATE TABLE Assets
(
    AssetId             INT IDENTITY(1,1) NOT NULL,
    Code                NVARCHAR(30)      NOT NULL,
    Name                NVARCHAR(150)     NOT NULL,
    Description         NVARCHAR(500)     NULL,
    AcquisitionDate     DATE              NOT NULL,
    ReferenceValue      DECIMAL(18,2)     NOT NULL,
    CurrentStatus       NVARCHAR(30)      NOT NULL CONSTRAINT DF_Assets_Status DEFAULT ('Operational'),
    ImagePath           NVARCHAR(300)     NULL,
    AssetCategoryId     INT               NOT NULL,
    -- Atributos adicionales propios del contexto (sección 7.1)
    UsageHours          DECIMAL(18,2)     NOT NULL CONSTRAINT DF_Assets_UsageHours DEFAULT (0),
    CriticalityLevel    NVARCHAR(20)      NOT NULL CONSTRAINT DF_Assets_Criticality DEFAULT ('Medium'),
    LastMaintenanceDate DATE              NULL,
    IsActive            BIT               NOT NULL CONSTRAINT DF_Assets_IsActive DEFAULT (1),
    CONSTRAINT PK_Assets PRIMARY KEY (AssetId),
    CONSTRAINT UQ_Assets_Code UNIQUE (Code),
    CONSTRAINT FK_Assets_AssetCategories FOREIGN KEY (AssetCategoryId) REFERENCES AssetCategories (AssetCategoryId),
    CONSTRAINT CK_Assets_Status CHECK (CurrentStatus IN ('Operational','UnderMaintenance','OutOfService','Decommissioned')),
    CONSTRAINT CK_Assets_Criticality CHECK (CriticalityLevel IN ('Low','Medium','High','Critical')),
    CONSTRAINT CK_Assets_UsageHours CHECK (UsageHours >= 0)
);
GO

/* =========================================================
   3. TÉCNICOS Y ESPECIALIDADES (N:N con atributos)
   ========================================================= */

CREATE TABLE Specialties
(
    SpecialtyId     INT IDENTITY(1,1) NOT NULL,
    Name            NVARCHAR(100)     NOT NULL,
    Description     NVARCHAR(250)     NULL,
    IsActive        BIT               NOT NULL CONSTRAINT DF_Specialties_IsActive DEFAULT (1),
    CONSTRAINT PK_Specialties PRIMARY KEY (SpecialtyId),
    CONSTRAINT UQ_Specialties_Name UNIQUE (Name)
);
GO

CREATE TABLE Technicians
(
    TechnicianId            INT IDENTITY(1,1) NOT NULL,
    FullName                NVARCHAR(150)     NOT NULL,
    IdentificationNumber    NVARCHAR(30)      NOT NULL,
    Email                   NVARCHAR(150)     NULL,
    Phone                   NVARCHAR(30)      NULL,
    IsAvailable             BIT               NOT NULL CONSTRAINT DF_Technicians_IsAvailable DEFAULT (1),
    HourlyRate              DECIMAL(18,2)     NOT NULL,
    IsActive                BIT               NOT NULL CONSTRAINT DF_Technicians_IsActive DEFAULT (1),
    CONSTRAINT PK_Technicians PRIMARY KEY (TechnicianId),
    CONSTRAINT UQ_Technicians_Identification UNIQUE (IdentificationNumber),
    CONSTRAINT CK_Technicians_HourlyRate CHECK (HourlyRate >= 0)
);
GO

CREATE TABLE TechnicianSpecialties
(
    TechnicianSpecialtyId   INT IDENTITY(1,1) NOT NULL,
    TechnicianId            INT               NOT NULL,
    SpecialtyId             INT               NOT NULL,
    CertificationLevel      NVARCHAR(30)      NOT NULL,   -- Junior | Intermediate | Senior | Expert
    CertifiedSince          DATE              NOT NULL,
    CONSTRAINT PK_TechnicianSpecialties PRIMARY KEY (TechnicianSpecialtyId),
    CONSTRAINT UQ_TechnicianSpecialties UNIQUE (TechnicianId, SpecialtyId),
    CONSTRAINT FK_TechnicianSpecialties_Technicians FOREIGN KEY (TechnicianId) REFERENCES Technicians (TechnicianId),
    CONSTRAINT FK_TechnicianSpecialties_Specialties FOREIGN KEY (SpecialtyId)  REFERENCES Specialties (SpecialtyId)
);
GO

/* =========================================================
   4. TIPOS DE MANTENIMIENTO
   Define la especialidad requerida para asignar técnicos (sección 7.7)
   ========================================================= */

CREATE TABLE MaintenanceTypes
(
    MaintenanceTypeId       INT IDENTITY(1,1) NOT NULL,
    Name                    NVARCHAR(60)      NOT NULL,   -- Preventive | Corrective | Emergency
    Description             NVARCHAR(250)     NULL,
    RequiredSpecialtyId     INT               NULL,
    IsEmergency             BIT               NOT NULL CONSTRAINT DF_MaintenanceTypes_IsEmergency DEFAULT (0),
    IsActive                BIT               NOT NULL CONSTRAINT DF_MaintenanceTypes_IsActive DEFAULT (1),
    CONSTRAINT PK_MaintenanceTypes PRIMARY KEY (MaintenanceTypeId),
    CONSTRAINT UQ_MaintenanceTypes_Name UNIQUE (Name),
    CONSTRAINT FK_MaintenanceTypes_Specialties FOREIGN KEY (RequiredSpecialtyId) REFERENCES Specialties (SpecialtyId)
);
GO

/* =========================================================
   5. REPUESTOS Y PROVEEDORES (N:N con atributos)
   ========================================================= */

CREATE TABLE Suppliers
(
    SupplierId      INT IDENTITY(1,1) NOT NULL,
    Name            NVARCHAR(150)     NOT NULL,
    ContactName     NVARCHAR(150)     NULL,
    Phone           NVARCHAR(30)      NULL,
    Email           NVARCHAR(150)     NULL,
    Address         NVARCHAR(250)     NULL,
    GeneralTerms    NVARCHAR(500)     NULL,
    IsActive        BIT               NOT NULL CONSTRAINT DF_Suppliers_IsActive DEFAULT (1),
    CONSTRAINT PK_Suppliers PRIMARY KEY (SupplierId),
    CONSTRAINT UQ_Suppliers_Name UNIQUE (Name)
);
GO

CREATE TABLE SpareParts
(
    SparePartId     INT IDENTITY(1,1) NOT NULL,
    Code            NVARCHAR(30)      NOT NULL,
    Name            NVARCHAR(150)     NOT NULL,
    UnitOfMeasure   NVARCHAR(30)      NOT NULL,
    CurrentStock    DECIMAL(18,2)     NOT NULL CONSTRAINT DF_SpareParts_CurrentStock DEFAULT (0),
    MinimumStock    DECIMAL(18,2)     NOT NULL CONSTRAINT DF_SpareParts_MinimumStock DEFAULT (0),
    IsActive        BIT               NOT NULL CONSTRAINT DF_SpareParts_IsActive DEFAULT (1),
    CONSTRAINT PK_SpareParts PRIMARY KEY (SparePartId),
    CONSTRAINT UQ_SpareParts_Code UNIQUE (Code),
    CONSTRAINT CK_SpareParts_CurrentStock CHECK (CurrentStock >= 0),
    CONSTRAINT CK_SpareParts_MinimumStock CHECK (MinimumStock >= 0)
);
GO

CREATE TABLE SparePartSuppliers
(
    SparePartSupplierId     INT IDENTITY(1,1) NOT NULL,
    SparePartId             INT               NOT NULL,
    SupplierId              INT               NOT NULL,
    UnitPrice               DECIMAL(18,2)     NOT NULL,
    EstimatedDeliveryDays   INT               NOT NULL,
    IsPreferred             BIT               NOT NULL CONSTRAINT DF_SparePartSuppliers_IsPreferred DEFAULT (0),
    CONSTRAINT PK_SparePartSuppliers PRIMARY KEY (SparePartSupplierId),
    CONSTRAINT UQ_SparePartSuppliers UNIQUE (SparePartId, SupplierId),
    CONSTRAINT FK_SparePartSuppliers_SpareParts FOREIGN KEY (SparePartId) REFERENCES SpareParts (SparePartId),
    CONSTRAINT FK_SparePartSuppliers_Suppliers  FOREIGN KEY (SupplierId)  REFERENCES Suppliers (SupplierId),
    CONSTRAINT CK_SparePartSuppliers_UnitPrice CHECK (UnitPrice >= 0),
    CONSTRAINT CK_SparePartSuppliers_Delivery  CHECK (EstimatedDeliveryDays >= 0)
);
GO

/* =========================================================
   6. MANTENIMIENTO PREVENTIVO (regla por categoría o por activo)
   ========================================================= */

CREATE TABLE PreventiveMaintenanceRules
(
    PreventiveMaintenanceRuleId INT IDENTITY(1,1) NOT NULL,
    AssetCategoryId             INT               NULL,
    AssetId                     INT               NULL,
    ControlVariable             NVARCHAR(50)      NOT NULL,   -- UsageHours | Mileage | Cycles | Days
    Threshold                   DECIMAL(18,2)     NOT NULL,
    WarningMargin               DECIMAL(18,2)     NOT NULL CONSTRAINT DF_PMRules_WarningMargin DEFAULT (0),
    IsActive                    BIT               NOT NULL CONSTRAINT DF_PMRules_IsActive DEFAULT (1),
    CONSTRAINT PK_PreventiveMaintenanceRules PRIMARY KEY (PreventiveMaintenanceRuleId),
    CONSTRAINT FK_PMRules_AssetCategories FOREIGN KEY (AssetCategoryId) REFERENCES AssetCategories (AssetCategoryId),
    CONSTRAINT FK_PMRules_Assets          FOREIGN KEY (AssetId)         REFERENCES Assets (AssetId),
    CONSTRAINT CK_PMRules_Threshold CHECK (Threshold > 0),
    -- La regla aplica a una categoría o a un activo puntual, nunca a ambos ni a ninguno
    CONSTRAINT CK_PMRules_Scope CHECK
    (
        (AssetCategoryId IS NOT NULL AND AssetId IS NULL)
     OR (AssetCategoryId IS NULL     AND AssetId IS NOT NULL)
    )
);
GO

/* =========================================================
   7. ORDEN DE MANTENIMIENTO (proceso principal)
   ========================================================= */

CREATE TABLE MaintenanceOrders
(
    MaintenanceOrderId  INT IDENTITY(1,1) NOT NULL,
    OrderCode           NVARCHAR(30)      NOT NULL,
    AssetId             INT               NOT NULL,
    MaintenanceTypeId   INT               NOT NULL,
    RequestedByUserId   INT               NOT NULL,
    Reason              NVARCHAR(500)     NOT NULL,
    Diagnosis           NVARCHAR(1000)    NULL,
    RequestDate         DATETIME2(0)      NOT NULL CONSTRAINT DF_Orders_RequestDate DEFAULT (SYSDATETIME()),
    ScheduledDate       DATE              NULL,
    ClosingDate         DATETIME2(0)      NULL,
    Priority            NVARCHAR(20)      NOT NULL CONSTRAINT DF_Orders_Priority DEFAULT ('Medium'),
    CurrentStatus       NVARCHAR(30)      NOT NULL CONSTRAINT DF_Orders_Status   DEFAULT ('Requested'),
    SparePartsCost      DECIMAL(18,2)     NOT NULL CONSTRAINT DF_Orders_SpareCost DEFAULT (0),
    LaborCost           DECIMAL(18,2)     NOT NULL CONSTRAINT DF_Orders_LaborCost DEFAULT (0),
    TotalCost           AS (SparePartsCost + LaborCost) PERSISTED,
    CONSTRAINT PK_MaintenanceOrders PRIMARY KEY (MaintenanceOrderId),
    CONSTRAINT UQ_MaintenanceOrders_Code UNIQUE (OrderCode),
    CONSTRAINT FK_Orders_Assets           FOREIGN KEY (AssetId)           REFERENCES Assets (AssetId),
    CONSTRAINT FK_Orders_MaintenanceTypes FOREIGN KEY (MaintenanceTypeId) REFERENCES MaintenanceTypes (MaintenanceTypeId),
    CONSTRAINT FK_Orders_Users            FOREIGN KEY (RequestedByUserId) REFERENCES Users (UserId),
    CONSTRAINT CK_Orders_Status CHECK (CurrentStatus IN
        ('Requested','Diagnosed','Approved','Rejected','InProgress','WaitingForParts','Completed','Cancelled')),
    CONSTRAINT CK_Orders_Priority CHECK (Priority IN ('Low','Medium','High','Urgent')),
    CONSTRAINT CK_Orders_Costs CHECK (SparePartsCost >= 0 AND LaborCost >= 0)
);
GO

/* ---------- 7.6 Detalle de repuestos utilizados ---------- */
CREATE TABLE OrderSpareParts
(
    OrderSparePartId    INT IDENTITY(1,1) NOT NULL,
    MaintenanceOrderId  INT               NOT NULL,
    SparePartId         INT               NOT NULL,
    SupplierId          INT               NULL,   -- proveedor seleccionado para ese consumo
    Quantity            DECIMAL(18,2)     NOT NULL,
    UnitCostAtUse       DECIMAL(18,2)     NOT NULL,
    Subtotal            AS (Quantity * UnitCostAtUse) PERSISTED,
    RegisteredAt        DATETIME2(0)      NOT NULL CONSTRAINT DF_OrderSpareParts_RegisteredAt DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_OrderSpareParts PRIMARY KEY (OrderSparePartId),
    CONSTRAINT FK_OrderSpareParts_Orders     FOREIGN KEY (MaintenanceOrderId) REFERENCES MaintenanceOrders (MaintenanceOrderId),
    CONSTRAINT FK_OrderSpareParts_SpareParts FOREIGN KEY (SparePartId)        REFERENCES SpareParts (SparePartId),
    CONSTRAINT FK_OrderSpareParts_Suppliers  FOREIGN KEY (SupplierId)         REFERENCES Suppliers (SupplierId),
    CONSTRAINT CK_OrderSpareParts_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_OrderSpareParts_UnitCost CHECK (UnitCostAtUse >= 0)
);
GO

/* ---------- 7.7 Asignación de técnicos a la orden ---------- */
CREATE TABLE OrderTechnicians
(
    OrderTechnicianId   INT IDENTITY(1,1) NOT NULL,
    MaintenanceOrderId  INT               NOT NULL,
    TechnicianId        INT               NOT NULL,
    AssignmentDate      DATETIME2(0)      NOT NULL CONSTRAINT DF_OrderTechnicians_AssignmentDate DEFAULT (SYSDATETIME()),
    HoursWorked         DECIMAL(9,2)      NOT NULL CONSTRAINT DF_OrderTechnicians_Hours DEFAULT (0),
    HourlyRateAtAssignment DECIMAL(18,2)  NOT NULL,
    CONSTRAINT PK_OrderTechnicians PRIMARY KEY (OrderTechnicianId),
    CONSTRAINT UQ_OrderTechnicians UNIQUE (MaintenanceOrderId, TechnicianId),
    CONSTRAINT FK_OrderTechnicians_Orders      FOREIGN KEY (MaintenanceOrderId) REFERENCES MaintenanceOrders (MaintenanceOrderId),
    CONSTRAINT FK_OrderTechnicians_Technicians FOREIGN KEY (TechnicianId)       REFERENCES Technicians (TechnicianId),
    CONSTRAINT CK_OrderTechnicians_Hours CHECK (HoursWorked >= 0),
    CONSTRAINT CK_OrderTechnicians_Rate  CHECK (HourlyRateAtAssignment >= 0)
);
GO

/* =========================================================
   8. HISTORIAL DE ESTADOS (solo lectura para la aplicación)
   ========================================================= */

CREATE TABLE AssetStatusHistory
(
    AssetStatusHistoryId    INT IDENTITY(1,1) NOT NULL,
    AssetId                 INT               NOT NULL,
    PreviousStatus          NVARCHAR(30)      NULL,
    NewStatus               NVARCHAR(30)      NOT NULL,
    ChangeDate              DATETIME2(0)      NOT NULL CONSTRAINT DF_AssetHistory_ChangeDate DEFAULT (SYSDATETIME()),
    UserId                  INT               NULL,   -- NULL cuando el cambio lo dispara el sistema
    MaintenanceOrderId      INT               NULL,   -- orden que originó el cambio automático
    Remarks                 NVARCHAR(500)     NULL,
    CONSTRAINT PK_AssetStatusHistory PRIMARY KEY (AssetStatusHistoryId),
    CONSTRAINT FK_AssetHistory_Assets FOREIGN KEY (AssetId)            REFERENCES Assets (AssetId),
    CONSTRAINT FK_AssetHistory_Users  FOREIGN KEY (UserId)             REFERENCES Users (UserId),
    CONSTRAINT FK_AssetHistory_Orders FOREIGN KEY (MaintenanceOrderId) REFERENCES MaintenanceOrders (MaintenanceOrderId)
);
GO

CREATE TABLE OrderStatusHistory
(
    OrderStatusHistoryId    INT IDENTITY(1,1) NOT NULL,
    MaintenanceOrderId      INT               NOT NULL,
    PreviousStatus          NVARCHAR(30)      NULL,
    NewStatus               NVARCHAR(30)      NOT NULL,
    ChangeDate              DATETIME2(0)      NOT NULL CONSTRAINT DF_OrderHistory_ChangeDate DEFAULT (SYSDATETIME()),
    UserId                  INT               NULL,
    Remarks                 NVARCHAR(500)     NULL,
    CONSTRAINT PK_OrderStatusHistory PRIMARY KEY (OrderStatusHistoryId),
    CONSTRAINT FK_OrderHistory_Orders FOREIGN KEY (MaintenanceOrderId) REFERENCES MaintenanceOrders (MaintenanceOrderId),
    CONSTRAINT FK_OrderHistory_Users  FOREIGN KEY (UserId)             REFERENCES Users (UserId)
);
GO

/* =========================================================
   9. ÍNDICES DE APOYO PARA REPORTES Y DASHBOARD
   ========================================================= */

CREATE INDEX IX_Assets_AssetCategoryId        ON Assets (AssetCategoryId);
CREATE INDEX IX_Assets_CurrentStatus          ON Assets (CurrentStatus);
CREATE INDEX IX_Orders_AssetId                ON MaintenanceOrders (AssetId);
CREATE INDEX IX_Orders_CurrentStatus          ON MaintenanceOrders (CurrentStatus);
CREATE INDEX IX_Orders_RequestDate            ON MaintenanceOrders (RequestDate);
CREATE INDEX IX_OrderSpareParts_OrderId       ON OrderSpareParts (MaintenanceOrderId);
CREATE INDEX IX_OrderTechnicians_TechnicianId ON OrderTechnicians (TechnicianId);
CREATE INDEX IX_AssetHistory_AssetId          ON AssetStatusHistory (AssetId, ChangeDate);
CREATE INDEX IX_OrderHistory_OrderId          ON OrderStatusHistory (MaintenanceOrderId, ChangeDate);
GO