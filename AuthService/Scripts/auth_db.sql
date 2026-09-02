-- ============================
-- Database: auth_db (SQL Server)
-- ============================
IF DB_ID('auth_db') IS NULL
BEGIN
    CREATE DATABASE auth_db;
END
GO

USE auth_db;
GO

IF OBJECT_ID('dbo.users', 'U') IS NOT NULL
    DROP TABLE dbo.users;
GO

CREATE TABLE dbo.users (
    user_id        UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_users_user_id DEFAULT NEWID(),
    username       NVARCHAR(100) NOT NULL,
    password_hash  NVARCHAR(MAX) NOT NULL,
    role           VARCHAR(30) NOT NULL,
    is_active      BIT NOT NULL CONSTRAINT DF_users_is_active DEFAULT 1,
    created_at     DATETIME2 NOT NULL CONSTRAINT DF_users_created_at DEFAULT GETUTCDATE(),
    CONSTRAINT PK_users PRIMARY KEY (user_id),
    CONSTRAINT UQ_users_username UNIQUE (username),
    CONSTRAINT chk_user_role CHECK (role IN ('ADMIN', 'USER'))
);
GO
