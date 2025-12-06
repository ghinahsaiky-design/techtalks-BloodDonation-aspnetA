Apply Migration
------------------------------------------------------------------
make sure your connection string in appsettings.json is correct.

Run:
in PMC :
Update-Database

OR

dotnet ef database update

This creates the database and all tables in your SQL Server.

Verify Database
------------------------------------------------------------------
Open SQL Server Management Studio (SSMS)

Connect to your server

Verify the database BloodDonation exists with all tables
