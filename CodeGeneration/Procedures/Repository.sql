--------------------------------------------------------------------------------------------------------------------------------
IF OBJECT_ID('GetAllConfigurationEntry') IS NOT NULL
BEGIN--IF
	DROP PROCEDURE [GetAllConfigurationEntry]
END--IF
GO

CREATE PROCEDURE [GetAllConfigurationEntry] AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

    /* Generated by AsapWiki-ADODAL-Procedures */
	
	
	SELECT
		[Id] as [Id],
		[ConfigurationGroupId] as [ConfigurationGroupId],
		[Name] as [Name],
		[Value] as [Value],
		[Description] as [Description]
	FROM
		[ConfigurationEntry]

END--PROCEDURE
GO

--------------------------------------------------------------------------------------------------------------------------------
IF OBJECT_ID('DeleteConfigurationEntryById') IS NOT NULL
BEGIN--IF
	DROP PROCEDURE [DeleteConfigurationEntryById]
END--IF
GO

CREATE PROCEDURE [DeleteConfigurationEntryById]
(
	@Id Int
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

    /* Generated by AsapWiki-ADODAL-Procedures */
	
	DELETE FROM
		[ConfigurationEntry]
	WHERE
		Id = @Id

END--PROCEDURE
GO


--------------------------------------------------------------------------------------------------------------------------------
IF OBJECT_ID('GetConfigurationEntryById') IS NOT NULL
BEGIN--IF
	DROP PROCEDURE [GetConfigurationEntryById]
END--IF
GO

CREATE PROCEDURE [GetConfigurationEntryById]
(
	@Id Int
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

    /* Generated by AsapWiki-ADODAL-Procedures */
	
	
	SELECT
		[Id] as [Id],
		[ConfigurationGroupId] as [ConfigurationGroupId],
		[Name] as [Name],
		[Value] as [Value],
		[Description] as [Description]
	FROM
		[ConfigurationEntry]
	WHERE
		Id = @Id

END--PROCEDURE
GO

--------------------------------------------------------------------------------------------------------------------------------
IF OBJECT_ID('UpdateConfigurationEntryById') IS NOT NULL
BEGIN--IF
	DROP PROCEDURE [UpdateConfigurationEntryById]
END--IF
GO



CREATE PROCEDURE [UpdateConfigurationEntryById]
(
	@Id as int,
	@ConfigurationGroupId as int,
	@Name as nvarchar (128),
	@Value as nvarchar (255) = NULL,
	@Description as nvarchar (1000) = NULL
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

    /* Generated by AsapWiki-ADODAL-Procedures */
	
	UPDATE
		[ConfigurationEntry]
	SET
		[ConfigurationGroupId] = @ConfigurationGroupId,
		[Name] = @Name,
		[Value] = @Value,
		[Description] = @Description
	FROM
		[ConfigurationEntry]
	WHERE
		Id = @Id

END--PROCEDURE
GO

--------------------------------------------------------------------------------------------------------------------------------
IF OBJECT_ID('InsertConfigurationEntry') IS NOT NULL
BEGIN--IF
	DROP PROCEDURE [InsertConfigurationEntry]
END--IF
GO



CREATE PROCEDURE [InsertConfigurationEntry]
(
	@ConfigurationGroupId as int,
	@Name as nvarchar (128),
	@Value as nvarchar (255) = NULL,
	@Description as nvarchar (1000) = NULL
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;
	
    /* Generated by AsapWiki-ADODAL-Procedures */

	INSERT INTO [ConfigurationEntry]
	(
		[ConfigurationGroupId],
		[Name],
		[Value],
		[Description]
	)
	VALUES
	(
		@ConfigurationGroupId,
		@Name,
		@Value,
		@Description
	)

    SELECT cast(SCOPE_IDENTITY() as int) as [NewId]

END--PROCEDURE
GO



--------------------------------------------------------------------------------------------------------------------------------
IF OBJECT_ID('GetAllConfigurationGroup') IS NOT NULL
BEGIN--IF
	DROP PROCEDURE [GetAllConfigurationGroup]
END--IF
GO

CREATE PROCEDURE [GetAllConfigurationGroup] AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

    /* Generated by AsapWiki-ADODAL-Procedures */
	
	
	SELECT
		[Id] as [Id],
		[Name] as [Name],
		[Description] as [Description]
	FROM
		[ConfigurationGroup]

END--PROCEDURE
GO

--------------------------------------------------------------------------------------------------------------------------------
IF OBJECT_ID('DeleteConfigurationGroupById') IS NOT NULL
BEGIN--IF
	DROP PROCEDURE [DeleteConfigurationGroupById]
END--IF
GO

CREATE PROCEDURE [DeleteConfigurationGroupById]
(
	@Id Int
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

    /* Generated by AsapWiki-ADODAL-Procedures */
	
	DELETE FROM
		[ConfigurationGroup]
	WHERE
		Id = @Id

END--PROCEDURE
GO


--------------------------------------------------------------------------------------------------------------------------------
IF OBJECT_ID('GetConfigurationGroupById') IS NOT NULL
BEGIN--IF
	DROP PROCEDURE [GetConfigurationGroupById]
END--IF
GO

CREATE PROCEDURE [GetConfigurationGroupById]
(
	@Id Int
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

    /* Generated by AsapWiki-ADODAL-Procedures */
	
	
	SELECT
		[Id] as [Id],
		[Name] as [Name],
		[Description] as [Description]
	FROM
		[ConfigurationGroup]
	WHERE
		Id = @Id

END--PROCEDURE
GO

--------------------------------------------------------------------------------------------------------------------------------
IF OBJECT_ID('UpdateConfigurationGroupById') IS NOT NULL
BEGIN--IF
	DROP PROCEDURE [UpdateConfigurationGroupById]
END--IF
GO



CREATE PROCEDURE [UpdateConfigurationGroupById]
(
	@Id as int,
	@Name as nvarchar (128),
	@Description as nvarchar (1000) = NULL
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

    /* Generated by AsapWiki-ADODAL-Procedures */
	
	UPDATE
		[ConfigurationGroup]
	SET
		[Name] = @Name,
		[Description] = @Description
	FROM
		[ConfigurationGroup]
	WHERE
		Id = @Id

END--PROCEDURE
GO

--------------------------------------------------------------------------------------------------------------------------------
IF OBJECT_ID('InsertConfigurationGroup') IS NOT NULL
BEGIN--IF
	DROP PROCEDURE [InsertConfigurationGroup]
END--IF
GO



CREATE PROCEDURE [InsertConfigurationGroup]
(
	@Name as nvarchar (128),
	@Description as nvarchar (1000) = NULL
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;
	
    /* Generated by AsapWiki-ADODAL-Procedures */

	INSERT INTO [ConfigurationGroup]
	(
		[Name],
		[Description]
	)
	VALUES
	(
		@Name,
		@Description
	)

    SELECT cast(SCOPE_IDENTITY() as int) as [NewId]

END--PROCEDURE
GO



--------------------------------------------------------------------------------------------------------------------------------
IF OBJECT_ID('GetAllPage') IS NOT NULL
BEGIN--IF
	DROP PROCEDURE [GetAllPage]
END--IF
GO

CREATE PROCEDURE [GetAllPage] AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

    /* Generated by AsapWiki-ADODAL-Procedures */
	
	
	SELECT
		[Id] as [Id],
		[Name] as [Name],
		[Navigation] as [Navigation],
		[Description] as [Description],
		[Body] as [Body],
		[CreatedByUserId] as [CreatedByUserId],
		[CreatedDate] as [CreatedDate],
		[ModifiedByUserId] as [ModifiedByUserId],
		[ModifiedDate] as [ModifiedDate]
	FROM
		[Page]

END--PROCEDURE
GO

--------------------------------------------------------------------------------------------------------------------------------
IF OBJECT_ID('DeletePageById') IS NOT NULL
BEGIN--IF
	DROP PROCEDURE [DeletePageById]
END--IF
GO

CREATE PROCEDURE [DeletePageById]
(
	@Id Int
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

    /* Generated by AsapWiki-ADODAL-Procedures */
	
	DELETE FROM
		[Page]
	WHERE
		Id = @Id

END--PROCEDURE
GO


--------------------------------------------------------------------------------------------------------------------------------
IF OBJECT_ID('GetPageById') IS NOT NULL
BEGIN--IF
	DROP PROCEDURE [GetPageById]
END--IF
GO

CREATE PROCEDURE [GetPageById]
(
	@Id Int
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

    /* Generated by AsapWiki-ADODAL-Procedures */
	
	
	SELECT
		[Id] as [Id],
		[Name] as [Name],
		[Navigation] as [Navigation],
		[Description] as [Description],
		[Body] as [Body],
		[CreatedByUserId] as [CreatedByUserId],
		[CreatedDate] as [CreatedDate],
		[ModifiedByUserId] as [ModifiedByUserId],
		[ModifiedDate] as [ModifiedDate]
	FROM
		[Page]
	WHERE
		Id = @Id

END--PROCEDURE
GO

--------------------------------------------------------------------------------------------------------------------------------
IF OBJECT_ID('UpdatePageById') IS NOT NULL
BEGIN--IF
	DROP PROCEDURE [UpdatePageById]
END--IF
GO



CREATE PROCEDURE [UpdatePageById]
(
	@Id as int,
	@Name as nvarchar (128),
	@Navigation as nvarchar (128),
	@Description as nvarchar (MAX),
	@Body as nvarchar (MAX),
	@CreatedByUserId as int,
	@CreatedDate as datetime,
	@ModifiedByUserId as int,
	@ModifiedDate as datetime
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

    /* Generated by AsapWiki-ADODAL-Procedures */
	
	UPDATE
		[Page]
	SET
		[Name] = @Name,
		[Navigation] = @Navigation,
		[Description] = @Description,
		[Body] = @Body,
		[CreatedByUserId] = @CreatedByUserId,
		[CreatedDate] = @CreatedDate,
		[ModifiedByUserId] = @ModifiedByUserId,
		[ModifiedDate] = @ModifiedDate
	FROM
		[Page]
	WHERE
		Id = @Id

END--PROCEDURE
GO

--------------------------------------------------------------------------------------------------------------------------------
IF OBJECT_ID('InsertPage') IS NOT NULL
BEGIN--IF
	DROP PROCEDURE [InsertPage]
END--IF
GO



CREATE PROCEDURE [InsertPage]
(
	@Name as nvarchar (128),
	@Navigation as nvarchar (128),
	@Description as nvarchar (MAX),
	@Body as nvarchar (MAX),
	@CreatedByUserId as int,
	@CreatedDate as datetime,
	@ModifiedByUserId as int,
	@ModifiedDate as datetime
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;
	
    /* Generated by AsapWiki-ADODAL-Procedures */

	INSERT INTO [Page]
	(
		[Name],
		[Navigation],
		[Description],
		[Body],
		[CreatedByUserId],
		[CreatedDate],
		[ModifiedByUserId],
		[ModifiedDate]
	)
	VALUES
	(
		@Name,
		@Navigation,
		@Description,
		@Body,
		@CreatedByUserId,
		@CreatedDate,
		@ModifiedByUserId,
		@ModifiedDate
	)

    SELECT cast(SCOPE_IDENTITY() as int) as [NewId]

END--PROCEDURE
GO



--------------------------------------------------------------------------------------------------------------------------------
IF OBJECT_ID('GetAllPageFile') IS NOT NULL
BEGIN--IF
	DROP PROCEDURE [GetAllPageFile]
END--IF
GO

CREATE PROCEDURE [GetAllPageFile] AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

    /* Generated by AsapWiki-ADODAL-Procedures */
	
	
	SELECT
		[Id] as [Id],
		[PageId] as [PageId],
		[Name] as [Name],
		[Size] as [Size],
		[CreatedDate] as [CreatedDate],
		[Data] as [Data]
	FROM
		[PageFile]

END--PROCEDURE
GO

--------------------------------------------------------------------------------------------------------------------------------
IF OBJECT_ID('DeletePageFileById') IS NOT NULL
BEGIN--IF
	DROP PROCEDURE [DeletePageFileById]
END--IF
GO

CREATE PROCEDURE [DeletePageFileById]
(
	@Id Int
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

    /* Generated by AsapWiki-ADODAL-Procedures */
	
	DELETE FROM
		[PageFile]
	WHERE
		Id = @Id

END--PROCEDURE
GO


--------------------------------------------------------------------------------------------------------------------------------
IF OBJECT_ID('GetPageFileById') IS NOT NULL
BEGIN--IF
	DROP PROCEDURE [GetPageFileById]
END--IF
GO

CREATE PROCEDURE [GetPageFileById]
(
	@Id Int
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

    /* Generated by AsapWiki-ADODAL-Procedures */
	
	
	SELECT
		[Id] as [Id],
		[PageId] as [PageId],
		[Name] as [Name],
		[Size] as [Size],
		[CreatedDate] as [CreatedDate],
		[Data] as [Data]
	FROM
		[PageFile]
	WHERE
		Id = @Id

END--PROCEDURE
GO

--------------------------------------------------------------------------------------------------------------------------------
IF OBJECT_ID('UpdatePageFileById') IS NOT NULL
BEGIN--IF
	DROP PROCEDURE [UpdatePageFileById]
END--IF
GO



CREATE PROCEDURE [UpdatePageFileById]
(
	@Id as int,
	@PageId as int,
	@Name as nvarchar (500),
	@Size as int,
	@CreatedDate as datetime,
	@Data as varbinary (MAX)
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

    /* Generated by AsapWiki-ADODAL-Procedures */
	
	UPDATE
		[PageFile]
	SET
		[PageId] = @PageId,
		[Name] = @Name,
		[Size] = @Size,
		[CreatedDate] = @CreatedDate,
		[Data] = @Data
	FROM
		[PageFile]
	WHERE
		Id = @Id

END--PROCEDURE
GO

--------------------------------------------------------------------------------------------------------------------------------
IF OBJECT_ID('InsertPageFile') IS NOT NULL
BEGIN--IF
	DROP PROCEDURE [InsertPageFile]
END--IF
GO



CREATE PROCEDURE [InsertPageFile]
(
	@PageId as int,
	@Name as nvarchar (500),
	@Size as int,
	@CreatedDate as datetime,
	@Data as varbinary (MAX)
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;
	
    /* Generated by AsapWiki-ADODAL-Procedures */

	INSERT INTO [PageFile]
	(
		[PageId],
		[Name],
		[Size],
		[CreatedDate],
		[Data]
	)
	VALUES
	(
		@PageId,
		@Name,
		@Size,
		@CreatedDate,
		@Data
	)

    SELECT cast(SCOPE_IDENTITY() as int) as [NewId]

END--PROCEDURE
GO



--------------------------------------------------------------------------------------------------------------------------------
IF OBJECT_ID('GetAllRole') IS NOT NULL
BEGIN--IF
	DROP PROCEDURE [GetAllRole]
END--IF
GO

CREATE PROCEDURE [GetAllRole] AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

    /* Generated by AsapWiki-ADODAL-Procedures */
	
	
	SELECT
		[Id] as [Id],
		[Name] as [Name],
		[Description] as [Description]
	FROM
		[Role]

END--PROCEDURE
GO

--------------------------------------------------------------------------------------------------------------------------------
IF OBJECT_ID('DeleteRoleById') IS NOT NULL
BEGIN--IF
	DROP PROCEDURE [DeleteRoleById]
END--IF
GO

CREATE PROCEDURE [DeleteRoleById]
(
	@Id Int
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

    /* Generated by AsapWiki-ADODAL-Procedures */
	
	DELETE FROM
		[Role]
	WHERE
		Id = @Id

END--PROCEDURE
GO


--------------------------------------------------------------------------------------------------------------------------------
IF OBJECT_ID('GetRoleById') IS NOT NULL
BEGIN--IF
	DROP PROCEDURE [GetRoleById]
END--IF
GO

CREATE PROCEDURE [GetRoleById]
(
	@Id Int
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

    /* Generated by AsapWiki-ADODAL-Procedures */
	
	
	SELECT
		[Id] as [Id],
		[Name] as [Name],
		[Description] as [Description]
	FROM
		[Role]
	WHERE
		Id = @Id

END--PROCEDURE
GO

--------------------------------------------------------------------------------------------------------------------------------
IF OBJECT_ID('UpdateRoleById') IS NOT NULL
BEGIN--IF
	DROP PROCEDURE [UpdateRoleById]
END--IF
GO



CREATE PROCEDURE [UpdateRoleById]
(
	@Id as int,
	@Name as nvarchar (128),
	@Description as nvarchar (1000) = NULL
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

    /* Generated by AsapWiki-ADODAL-Procedures */
	
	UPDATE
		[Role]
	SET
		[Name] = @Name,
		[Description] = @Description
	FROM
		[Role]
	WHERE
		Id = @Id

END--PROCEDURE
GO

--------------------------------------------------------------------------------------------------------------------------------
IF OBJECT_ID('InsertRole') IS NOT NULL
BEGIN--IF
	DROP PROCEDURE [InsertRole]
END--IF
GO



CREATE PROCEDURE [InsertRole]
(
	@Name as nvarchar (128),
	@Description as nvarchar (1000) = NULL
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;
	
    /* Generated by AsapWiki-ADODAL-Procedures */

	INSERT INTO [Role]
	(
		[Name],
		[Description]
	)
	VALUES
	(
		@Name,
		@Description
	)

    SELECT cast(SCOPE_IDENTITY() as int) as [NewId]

END--PROCEDURE
GO



--------------------------------------------------------------------------------------------------------------------------------
IF OBJECT_ID('GetAllUser') IS NOT NULL
BEGIN--IF
	DROP PROCEDURE [GetAllUser]
END--IF
GO

CREATE PROCEDURE [GetAllUser] AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

    /* Generated by AsapWiki-ADODAL-Procedures */
	
	
	SELECT
		[Id] as [Id],
		[EmailAddress] as [EmailAddress],
		[DisplayName] as [DisplayName],
		[PasswordHash] as [PasswordHash]
	FROM
		[User]

END--PROCEDURE
GO

--------------------------------------------------------------------------------------------------------------------------------
IF OBJECT_ID('DeleteUserById') IS NOT NULL
BEGIN--IF
	DROP PROCEDURE [DeleteUserById]
END--IF
GO

CREATE PROCEDURE [DeleteUserById]
(
	@Id Int
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

    /* Generated by AsapWiki-ADODAL-Procedures */
	
	DELETE FROM
		[User]
	WHERE
		Id = @Id

END--PROCEDURE
GO


--------------------------------------------------------------------------------------------------------------------------------
IF OBJECT_ID('GetUserById') IS NOT NULL
BEGIN--IF
	DROP PROCEDURE [GetUserById]
END--IF
GO

CREATE PROCEDURE [GetUserById]
(
	@Id Int
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

    /* Generated by AsapWiki-ADODAL-Procedures */
	
	
	SELECT
		[Id] as [Id],
		[EmailAddress] as [EmailAddress],
		[DisplayName] as [DisplayName],
		[PasswordHash] as [PasswordHash]
	FROM
		[User]
	WHERE
		Id = @Id

END--PROCEDURE
GO

--------------------------------------------------------------------------------------------------------------------------------
IF OBJECT_ID('UpdateUserById') IS NOT NULL
BEGIN--IF
	DROP PROCEDURE [UpdateUserById]
END--IF
GO



CREATE PROCEDURE [UpdateUserById]
(
	@Id as int,
	@EmailAddress as nvarchar (128),
	@DisplayName as nvarchar (128),
	@PasswordHash as nvarchar (128) = NULL
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

    /* Generated by AsapWiki-ADODAL-Procedures */
	
	UPDATE
		[User]
	SET
		[EmailAddress] = @EmailAddress,
		[DisplayName] = @DisplayName,
		[PasswordHash] = @PasswordHash
	FROM
		[User]
	WHERE
		Id = @Id

END--PROCEDURE
GO

--------------------------------------------------------------------------------------------------------------------------------
IF OBJECT_ID('InsertUser') IS NOT NULL
BEGIN--IF
	DROP PROCEDURE [InsertUser]
END--IF
GO



CREATE PROCEDURE [InsertUser]
(
	@EmailAddress as nvarchar (128),
	@DisplayName as nvarchar (128),
	@PasswordHash as nvarchar (128) = NULL
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;
	
    /* Generated by AsapWiki-ADODAL-Procedures */

	INSERT INTO [User]
	(
		[EmailAddress],
		[DisplayName],
		[PasswordHash]
	)
	VALUES
	(
		@EmailAddress,
		@DisplayName,
		@PasswordHash
	)

    SELECT cast(SCOPE_IDENTITY() as int) as [NewId]

END--PROCEDURE
GO



--------------------------------------------------------------------------------------------------------------------------------
IF OBJECT_ID('GetAllUserRole') IS NOT NULL
BEGIN--IF
	DROP PROCEDURE [GetAllUserRole]
END--IF
GO

CREATE PROCEDURE [GetAllUserRole] AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

    /* Generated by AsapWiki-ADODAL-Procedures */
	
	
	SELECT
		[Id] as [Id],
		[UserId] as [UserId],
		[RoleId] as [RoleId]
	FROM
		[UserRole]

END--PROCEDURE
GO

--------------------------------------------------------------------------------------------------------------------------------
IF OBJECT_ID('DeleteUserRoleById') IS NOT NULL
BEGIN--IF
	DROP PROCEDURE [DeleteUserRoleById]
END--IF
GO

CREATE PROCEDURE [DeleteUserRoleById]
(
	@Id Int
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

    /* Generated by AsapWiki-ADODAL-Procedures */
	
	DELETE FROM
		[UserRole]
	WHERE
		Id = @Id

END--PROCEDURE
GO


--------------------------------------------------------------------------------------------------------------------------------
IF OBJECT_ID('GetUserRoleById') IS NOT NULL
BEGIN--IF
	DROP PROCEDURE [GetUserRoleById]
END--IF
GO

CREATE PROCEDURE [GetUserRoleById]
(
	@Id Int
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

    /* Generated by AsapWiki-ADODAL-Procedures */
	
	
	SELECT
		[Id] as [Id],
		[UserId] as [UserId],
		[RoleId] as [RoleId]
	FROM
		[UserRole]
	WHERE
		Id = @Id

END--PROCEDURE
GO

--------------------------------------------------------------------------------------------------------------------------------
IF OBJECT_ID('UpdateUserRoleById') IS NOT NULL
BEGIN--IF
	DROP PROCEDURE [UpdateUserRoleById]
END--IF
GO



CREATE PROCEDURE [UpdateUserRoleById]
(
	@Id as int,
	@UserId as int,
	@RoleId as int
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

    /* Generated by AsapWiki-ADODAL-Procedures */
	
	UPDATE
		[UserRole]
	SET
		[UserId] = @UserId,
		[RoleId] = @RoleId
	FROM
		[UserRole]
	WHERE
		Id = @Id

END--PROCEDURE
GO

--------------------------------------------------------------------------------------------------------------------------------
IF OBJECT_ID('InsertUserRole') IS NOT NULL
BEGIN--IF
	DROP PROCEDURE [InsertUserRole]
END--IF
GO



CREATE PROCEDURE [InsertUserRole]
(
	@UserId as int,
	@RoleId as int
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;
	
    /* Generated by AsapWiki-ADODAL-Procedures */

	INSERT INTO [UserRole]
	(
		[UserId],
		[RoleId]
	)
	VALUES
	(
		@UserId,
		@RoleId
	)

    SELECT cast(SCOPE_IDENTITY() as int) as [NewId]

END--PROCEDURE
GO

