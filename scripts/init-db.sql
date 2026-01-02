-- Initialize development database for SQL Server
-- This script creates the database and lets EF Core handle table creation

-- Create database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'ChanzupDb')
BEGIN
    CREATE DATABASE ChanzupDb;
END

USE ChanzupDb;

-- Enable automatic migrations by creating a simple table
-- EF Core will handle the rest when the application starts
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '__EFMigrationsHistory')
BEGIN
    CREATE TABLE __EFMigrationsHistory (
        MigrationId NVARCHAR(150) NOT NULL,
        ProductVersion NVARCHAR(32) NOT NULL,
        CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY (MigrationId)
    );
END