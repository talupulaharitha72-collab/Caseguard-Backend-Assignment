# Commands

```bash
# postgres
docker run -d --name caseguard-postgres -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=caseguard -p 5432:5432 postgres:16
docker stop caseguard-postgres
docker start caseguard-postgres
psql -h localhost -U postgres -d caseguard

# build & run
dotnet build
dotnet run --project CaseGuard.Backend.Assignment

# tests
dotnet test CaseGuard.Backend.Assignment.Tests

# migrations
dotnet ef migrations add <Name> --project CaseGuard.Backend.Assignment
dotnet ef database update --project CaseGuard.Backend.Assignment

# packages
dotnet add CaseGuard.Backend.Assignment package <PackageName>
dotnet list CaseGuard.Backend.Assignment package
```
