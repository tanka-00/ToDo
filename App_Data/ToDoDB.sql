USE [ToDoDB];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        UserId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
        Name         NVARCHAR(100) NOT NULL,
        Email        NVARCHAR(150) NOT NULL,
        PasswordHash NVARCHAR(200) NOT NULL,
        PasswordSalt NVARCHAR(200) NOT NULL,
        Role         NVARCHAR(20) NOT NULL CONSTRAINT DF_Users_Role DEFAULT (N'User'),
        IsActive     BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
        CreatedAt    DATETIME2(0) NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSDATETIME()),
        CONSTRAINT UQ_Users_Email UNIQUE (Email),
        CONSTRAINT CK_Users_Role CHECK (Role IN (N'User', N'Admin'))
    );
END;
GO

IF OBJECT_ID(N'dbo.Tasks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tasks
    (
        TaskId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Tasks PRIMARY KEY,
        UserId      INT NOT NULL,
        Title       NVARCHAR(200) NOT NULL,
        Description NVARCHAR(1000) NULL,
        IsCompleted BIT NOT NULL CONSTRAINT DF_Tasks_IsCompleted DEFAULT (0),
        CreatedAt   DATETIME2(0) NOT NULL CONSTRAINT DF_Tasks_CreatedAt DEFAULT (SYSDATETIME()),
        UpdatedAt   DATETIME2(0) NULL,
        CONSTRAINT FK_Tasks_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId) ON DELETE CASCADE
    );

    CREATE INDEX IX_Tasks_UserId ON dbo.Tasks(UserId);
END;
GO

CREATE OR ALTER PROCEDURE dbo.GetTasks
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        TaskId,
        UserId,
        Title,
        Description,
        IsCompleted,
        CreatedAt,
        UpdatedAt
    FROM dbo.Tasks
    WHERE UserId = @UserId
    ORDER BY IsCompleted ASC, CreatedAt DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.ManageTask
    @Action NVARCHAR(10),
    @UserId INT,
    @TaskId INT = NULL,
    @Title NVARCHAR(200) = NULL,
    @Description NVARCHAR(1000) = NULL,
    @IsCompleted BIT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Affected INT = 0;

    IF @Action = N'INSERT'
    BEGIN
        IF LTRIM(RTRIM(ISNULL(@Title, N''))) = N''
        BEGIN
            SELECT 0 AS Affected;
            RETURN;
        END;

        INSERT INTO dbo.Tasks (UserId, Title, Description)
        VALUES
        (
            @UserId,
            LTRIM(RTRIM(@Title)),
            NULLIF(LTRIM(RTRIM(@Description)), N'')
        );

        SET @Affected = 1;
    END
    ELSE IF @Action = N'UPDATE'
    BEGIN
        UPDATE dbo.Tasks
        SET
            Title = LTRIM(RTRIM(@Title)),
            Description = NULLIF(LTRIM(RTRIM(@Description)), N''),
            UpdatedAt = SYSDATETIME()
        WHERE TaskId = @TaskId
          AND UserId = @UserId;

        SET @Affected = @@ROWCOUNT;
    END
    ELSE IF @Action = N'DELETE'
    BEGIN
        DELETE FROM dbo.Tasks
        WHERE TaskId = @TaskId
          AND UserId = @UserId;

        SET @Affected = @@ROWCOUNT;
    END
    ELSE IF @Action = N'COMPLETE'
    BEGIN
        UPDATE dbo.Tasks
        SET
            IsCompleted = @IsCompleted,
            UpdatedAt = SYSDATETIME()
        WHERE TaskId = @TaskId
          AND UserId = @UserId;

        SET @Affected = @@ROWCOUNT;
    END;

    SELECT @Affected AS Affected;
END;
GO

CREATE OR ALTER PROCEDURE dbo.GetUserByEmail
    @Email NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        UserId,
        Name,
        Email,
        PasswordHash,
        PasswordSalt,
        Role,
        IsActive,
        CreatedAt
    FROM dbo.Users
    WHERE Email = @Email;
END;
GO

CREATE OR ALTER PROCEDURE dbo.GetUserById
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        UserId,
        Name,
        Email,
        Role,
        IsActive,
        CreatedAt
    FROM dbo.Users
    WHERE UserId = @UserId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.GetUsers
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        U.UserId,
        U.Name,
        U.Email,
        U.IsActive,
        U.CreatedAt,
        COUNT(T.TaskId) AS TotalTasks,
        ISNULL(SUM(CASE WHEN T.IsCompleted = 1 THEN 1 ELSE 0 END), 0) AS CompletedTasks
    FROM dbo.Users U
    LEFT JOIN dbo.Tasks T ON T.UserId = U.UserId
    WHERE U.Role = N'User'
    GROUP BY
        U.UserId,
        U.Name,
        U.Email,
        U.IsActive,
        U.CreatedAt
    ORDER BY U.CreatedAt DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.ToggleUserStatus
    @UserId INT,
    @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Users
    SET IsActive = @IsActive
    WHERE UserId = @UserId
      AND Role = N'User';

    SELECT @@ROWCOUNT AS Affected;
END;
GO

CREATE OR ALTER PROCEDURE dbo.RegisterUser
    @Name NVARCHAR(100),
    @Email NVARCHAR(150),
    @PasswordHash NVARCHAR(200),
    @PasswordSalt NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Users (Name, Email, PasswordHash, PasswordSalt, Role, IsActive)
    VALUES (@Name, @Email, @PasswordHash, @PasswordSalt, N'User', 1);

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS UserId;
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = N'admin@gmail.com')
BEGIN
    INSERT INTO dbo.Users (Name, Email, PasswordHash, PasswordSalt, Role, IsActive)
    VALUES
    (
        N'Administrator',
        N'admin@gmail.com',
        N'KRt22mCbVayn/4qQ2UGZX7hYdWER8pPvjzgzG53swOs=',
        N'r+z7dIgjA2Uo+fdSi9CR0g==',
        N'Admin',
        1
    );
END;
GO
