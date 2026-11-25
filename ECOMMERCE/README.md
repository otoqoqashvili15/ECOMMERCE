# Atelier Commerce

An Angular storefront and ASP.NET Core Web API for a small online shop. It is based on the supplied e-commerce project brief and C4 architecture guide.

## What is included

- Responsive Angular storefront with product search, category/price filters, sorting, and product ratings.
- Customer registration and sign-in with JWT bearer authentication; administrator and store-manager roles.
- Product and category management, stock thresholds, and scheduled discount windows in the data model.
- Persistent account basket, checkout with standard or express delivery, order history, and order-status updates.
- Product reviews and a manager dashboard for orders, low stock, and tracked sales.
- ASP.NET Core REST endpoints, interactive Swagger UI with JWT authorization, OpenAPI JSON, EF Core SQL Server persistence, migrations, and structured error responses/logging.
- Separate frontend and backend Dockerfiles with one Compose file for the full stack.
- C4 context, container, and component diagrams plus a supporting entity relationship view in [docs/c4-model.md](docs/c4-model.md).

The brief marks online payment-provider integration as optional. This version uses cash on delivery and does not process card payments. Sample product photography is hosted by Unsplash; replace those demo URLs with owned or licensed assets before deployment.

## Run the full application with Docker Compose

Requirements: Docker Desktop with the Linux container engine and Docker Compose.

1. Start the stack with the included local development settings:

   ```powershell
   docker compose up --build -d
   ```

2. Open the storefront at [http://localhost:8080](http://localhost:8080). Swagger UI is at [http://localhost:8080/swagger](http://localhost:8080/swagger) or `http://localhost:8081/swagger`; the API readiness check is `/health/ready`, the OpenAPI document is `/openapi/v1.json`, and the Swagger document is `/swagger/v1/swagger.json`.
3. Sign in with the local demo administrator: `admin@atelier.local` / `AtelierDev!2026`. New registrations receive the Customer role.

The first API startup waits for SQL Server, applies EF Core migrations, creates the roles and configured administrator, and seeds sample categories and products. SQL Server data persists in the `ecommerce-sql-data` Docker volume.

Useful commands:

```powershell
docker compose ps
docker compose logs -f api
docker compose down
```

`docker compose down` keeps the database volume. To reset the local demo database, remove the volume with `docker compose down -v`.

## Run the API directly on Windows

This mode requires the .NET 10 SDK and SQL Server Express LocalDB. The Development profile connects to `(localdb)\MSSQLLocalDB`, which starts automatically when the API opens the connection.

```powershell
cd Back\Ecommerce\Ecommerce
dotnet run --launch-profile http
```

The API listens at `http://localhost:8081`, with Swagger UI at `http://localhost:8081/swagger`. Run Angular with `npm start` in `Front` to use the configured development proxy.

## Run the Angular development server

Start SQL Server and the API in containers, then run Angular on the host:

```powershell
docker compose up --build -d db api
cd Front
npm ci
npm start
```

The Angular development proxy forwards `/api`, `/openapi`, and `/swagger` to `http://localhost:8081`. Open `http://localhost:4200`.

## API overview

| Area | Routes | Access |
| --- | --- | --- |
| Authentication | `POST /api/auth/register`, `POST /api/auth/login`, `GET /api/auth/me` | Public / signed-in |
| Products | `GET /api/products`, `GET /api/products/{id}`, `POST`, `PUT`, `DELETE /api/products/{id}` | Read public; changes manager/admin |
| Categories | `GET /api/categories`, `POST`, `PUT`, `DELETE /api/categories/{id}` | Read public; changes manager/admin |
| Basket | `GET /api/cart`, `POST /api/cart/items`, `PUT /api/cart/items/{id}/quantity`, `DELETE /api/cart/items/{id}` | Signed-in |
| Orders | `GET /api/orders`, `GET /api/orders/{id}`, `POST /api/orders`, `PUT /api/orders/{id}/status` | Signed-in; status updates manager/admin |
| Reviews | `GET /api/products/{id}/reviews`, `POST /api/products/{id}/reviews` | Read public; write signed-in |
| Users | `GET /api/users`, `PUT /api/users/{id}`, `GET /api/users/{id}/orders` | Own profile/history; user list manager/admin |
| Store operations | `GET /api/admin/summary`, `GET /api/admin/inventory/low-stock` | Manager/admin |

Product list filters include `search`, `categoryId`, `brand`, `minPrice`, `maxPrice`, and `sort` (`featured`, `price-asc`, `price-desc`, `newest`, or `name`). Public checkout settings are available at `GET /api/store/checkout-settings`. The demo tax rate defaults to 8% of the merchandise subtotal and is configured in `docker-compose.yml` and `appsettings.Development.json`; set a rate appropriate to the shop's market before deployment. Standard shipping is free above the configured threshold, and express shipping has a separate fee. Enum values in JSON use their names, for example `"Shipped"`.

## Database and migrations

The API uses SQL Server with EF Core models for Identity users/roles, categories and subcategories, products and images, product reviews, baskets, orders, and order lines. The app runs `Database.MigrateAsync()` during startup.

Migration source files live in `Back/Ecommerce/Ecommerce/Migrations/`. To add a migration after changing the model, install the EF CLI if needed and run from `Back/Ecommerce/Ecommerce`:

```powershell
dotnet ef migrations add DescribeTheChange
dotnet ef migrations script --idempotent -o ..\..\..\database\ecommerce.sql
```

The SQL script is a reviewable deployment artifact; Compose applies the migration classes automatically. Do not run a generated SQL script against a production database without reviewing it and taking a backup.

## Configuration

Local JWT, administrator, and tax settings are included in `docker-compose.yml` and `Back/Ecommerce/Ecommerce/appsettings.Development.json`. Compose has local demo defaults and accepts overrides through `MSSQL_SA_PASSWORD`, `JWT_SECRET`, `ADMIN_EMAIL`, `ADMIN_PASSWORD`, and `STORE_TAX_RATE`. Running the API directly with the Development profile uses Windows SQL Server LocalDB (`(localdb)\MSSQLLocalDB`); the full Compose stack uses its own SQL Server container. These values are for local demos; replace them with deployment secrets and set `ASPNETCORE_ENVIRONMENT=Production` before deployment. The administrator password must meet the Identity policy (10+ characters with uppercase, lowercase, number, and symbol).

Production deployments should use a secrets manager, HTTPS termination, `ASPNETCORE_ENVIRONMENT=Production`, restricted database networking, and non-demo image assets. The Compose environment is a local development setup, not a production security profile.

## Project layout

```text
Back/Ecommerce/Ecommerce/   ASP.NET Core API, domain, EF Core context and migrations
Front/                      Angular storefront and Nginx image
docker-compose.yml           SQL Server + API + storefront
docs/c4-model.md             C4 model and entity relationship view
database/                    Generated database deployment scripts
```

## Build checks

Frontend build: `cd Front; npm run build`  
Backend build: `cd Back/Ecommerce/Ecommerce; dotnet build`

Automated test projects are not included in this starter.
