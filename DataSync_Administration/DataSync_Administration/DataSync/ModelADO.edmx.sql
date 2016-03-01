
-- --------------------------------------------------
-- Entity Designer DDL Script for SQL Server 2005, 2008, 2012 and Azure
-- --------------------------------------------------
-- Date Created: 05/09/2015 12:17:48
-- Generated from EDMX file: C:\Users\sb\Documents\Visual Studio 2013\ProjectsUmbraco\DataSync\DataSync\ModelADO.edmx
-- --------------------------------------------------

SET QUOTED_IDENTIFIER OFF;
GO
USE [DataSyncDB];
GO
IF SCHEMA_ID(N'dbo') IS NULL EXECUTE(N'CREATE SCHEMA [dbo]');
GO

-- --------------------------------------------------
-- Dropping existing FOREIGN KEY constraints
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[FK_CustomerLog]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[LogSet] DROP CONSTRAINT [FK_CustomerLog];
GO
IF OBJECT_ID(N'[dbo].[FK_CustomerWebSiteInbox]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[WebSiteInboxSet] DROP CONSTRAINT [FK_CustomerWebSiteInbox];
GO

-- --------------------------------------------------
-- Dropping existing tables
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[CustomerSet]', 'U') IS NOT NULL
    DROP TABLE [dbo].[CustomerSet];
GO
IF OBJECT_ID(N'[dbo].[LogSet]', 'U') IS NOT NULL
    DROP TABLE [dbo].[LogSet];
GO
IF OBJECT_ID(N'[dbo].[WebSiteInboxSet]', 'U') IS NOT NULL
    DROP TABLE [dbo].[WebSiteInboxSet];
GO

-- --------------------------------------------------
-- Creating all tables
-- --------------------------------------------------

-- Creating table 'CustomerSet'
CREATE TABLE [dbo].[CustomerSet] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(max)  NULL,
    [WebLogin] nvarchar(max)  NULL,
    [WebPassword] nvarchar(max)  NULL,
    [VTigerUsername] nvarchar(max)  NULL,
    [VTigerUrl] nvarchar(max)  NULL,
    [VTigerAccessKey] nvarchar(max)  NULL,
    [IsVTigerOK] bit  NULL,
    [EconomicPublicAPI] nvarchar(max)  NULL,
    [EconomicPrivateAPI] nvarchar(max)  NULL,
    [IsEconomicOK] bit  NULL,
    [DateCreated] datetime  NULL,
    [DateLastUpdated] datetime  NULL,
    [IsActive] bit  NULL,
    [ForceNewProduct] bit  NULL,
    [ForceNewDebtor] bit  NULL,
    [IsWriteToLogFile] bit  NOT NULL
);
GO

-- Creating table 'LogSet'
CREATE TABLE [dbo].[LogSet] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(max)  NULL,
    [Info] nvarchar(max)  NULL,
    [IsError] bit  NULL,
    [DateCreated] datetime  NULL,
    [CustomerId] int  NOT NULL
);
GO

-- Creating table 'WebSiteInboxSet'
CREATE TABLE [dbo].[WebSiteInboxSet] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [From] nvarchar(max)  NULL,
    [Subject] nvarchar(max)  NOT NULL,
    [Message] nvarchar(max)  NULL,
    [DateCreated] datetime  NOT NULL,
    [DateIsRead] datetime  NULL,
    [IsRead] bit  NOT NULL,
    [CustomerId] int  NOT NULL
);
GO

-- --------------------------------------------------
-- Creating all PRIMARY KEY constraints
-- --------------------------------------------------

-- Creating primary key on [Id] in table 'CustomerSet'
ALTER TABLE [dbo].[CustomerSet]
ADD CONSTRAINT [PK_CustomerSet]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'LogSet'
ALTER TABLE [dbo].[LogSet]
ADD CONSTRAINT [PK_LogSet]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'WebSiteInboxSet'
ALTER TABLE [dbo].[WebSiteInboxSet]
ADD CONSTRAINT [PK_WebSiteInboxSet]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- --------------------------------------------------
-- Creating all FOREIGN KEY constraints
-- --------------------------------------------------

-- Creating foreign key on [CustomerId] in table 'LogSet'
ALTER TABLE [dbo].[LogSet]
ADD CONSTRAINT [FK_CustomerLog]
    FOREIGN KEY ([CustomerId])
    REFERENCES [dbo].[CustomerSet]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_CustomerLog'
CREATE INDEX [IX_FK_CustomerLog]
ON [dbo].[LogSet]
    ([CustomerId]);
GO

-- Creating foreign key on [CustomerId] in table 'WebSiteInboxSet'
ALTER TABLE [dbo].[WebSiteInboxSet]
ADD CONSTRAINT [FK_CustomerWebSiteInbox]
    FOREIGN KEY ([CustomerId])
    REFERENCES [dbo].[CustomerSet]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_CustomerWebSiteInbox'
CREATE INDEX [IX_FK_CustomerWebSiteInbox]
ON [dbo].[WebSiteInboxSet]
    ([CustomerId]);
GO

-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------