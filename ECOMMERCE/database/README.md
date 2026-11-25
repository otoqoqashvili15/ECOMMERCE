# Database deployment artifacts

EF Core migration source is maintained with the API in `Back/Ecommerce/Ecommerce/Migrations/`. Generate an idempotent SQL Server script after a migration is added:

```powershell
dotnet ef migrations script --idempotent -o ..\..\..\database\ecommerce.sql
```

The Compose API container applies migrations during startup. Keep generated scripts under version control when preparing a database deployment.
