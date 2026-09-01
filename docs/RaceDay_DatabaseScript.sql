-- RaceDay Database Script
-- This script creates the RaceDay database, all six tables and the sample data

-- Creating the database
USE master;
GO

IF DB_ID('RaceDayDb') IS NOT NULL
BEGIN
    ALTER DATABASE RaceDayDb SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE RaceDayDb;
END
GO

CREATE DATABASE RaceDayDb;
GO

USE RaceDayDb;
GO