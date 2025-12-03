Install-Package Microsoft.EntityFrameworkCore -Version 8.0.0

Install-Package Microsoft.EntityFrameworkCore.SqlServer -Version 8.0.0

Install-Package Microsoft.EntityFrameworkCore.Tools -Version 8.0.0

This installs:

Core EF functionality

SQL Server provider

Tools for migrations and database updates

Apply Migration
------------------------------------------------------------------
Run:

dotnet ef database update


This creates the database and all tables in your SQL Server.

Verify Database
------------------------------------------------------------------
Open SQL Server Management Studio (SSMS)

Connect to your server

Verify the database BloodDonation exists with all tables
