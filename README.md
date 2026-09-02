# Auth, Inventory & Order Microservices

Three independent ASP.NET Core (.NET 8) Web APIs, each with its own SQL
Server database, plus a small shared class library for the pieces every
service needs identically (JWT validation, global exception handling,
common constants/DTOs).

```
AuthInventoryOrderMicroservices/
├── AuthInventoryOrderMicroservices.sln
├── Shared/                     <- referenced by all 3 services
│   ├── Security/                JwtSettings, JWT bearer wire-up, claims helpers
│   ├── Constants/                Roles, OrderStatuses
│   ├── Exceptions/               AppExceptionBase + NotFound/Conflict/Forbidden/...
│   ├── Middleware/                GlobalExceptionMiddleware
│   └── DTOs/PagedResult.cs
├── AuthService/                <- owns auth_db
│   ├── Controllers/AuthController.cs
│   ├── Security/                 PasswordHasher (PBKDF2), JwtTokenGenerator
│   ├── Services/ · Repositories/ · DTOs/ · Entities/ · Data/ · Exceptions/
│   ├── Scripts/auth_db.sql
│   └── Program.cs
├── InventoryService/            <- owns inventory_db
│   ├── Controllers/ProductsController.cs
│   ├── Security/                 InternalApiKeyAuthenticationHandler
│   ├── Services/ · Repositories/ · DTOs/ · Entities/ · Data/
│   ├── Scripts/inventory_db.sql
│   └── Program.cs
└── OrderService/                <- owns order_db, talks to Inventory Service over HTTP only
    ├── Controllers/OrdersController.cs
    ├── Clients/                  IInventoryServiceClient, InventoryServiceClient
    ├── Services/ · Repositories/ · DTOs/ · Entities/ · Data/
    ├── Scripts/order_db.sql
    └── Program.cs
```

## Authentication & RBAC design

**Auth Service** issues JWTs signed with a symmetric key (`Jwt:Key` in
`appsettings.json`). **Inventory Service and Order Service validate that
same token independently** — no network call back to Auth Service is needed
per request. This only works because all three services share identical
`Jwt:Key` / `Jwt:Issuer` / `Jwt:Audience` values. **You must set the same
real secret in all three `appsettings.json` files** before running this for
real; the placeholder value is there only so the projects are consistent
out of the box.

Claims embedded in the token: `uid` (user id), `username`, `role`
(`ADMIN` or `USER`). `Shared/Security/ClaimsPrincipalExtensions.cs` gives
every service `User.GetUserId()`, `User.GetRole()`, `User.IsAdmin()`.

RBAC on Inventory Service's public endpoints:
- `POST /api/products`, `PUT /api/products/{id}`, `DELETE /api/products/{id}`
  → `[Authorize(Roles = "ADMIN")]`
- `GET /api/products`, `GET /api/products/{id}` → any authenticated user

## Why `reduce_stock` / `restore_stock` don't use user JWTs

The spec says *"Only Admin can add/update stock"*, but Order Service must be
able to decrement stock the moment **any** authenticated User places an
order. Forwarding the placing-user's JWT to Inventory Service's
`reduce_stock` endpoint would conflict with the admin-only rule; making
`reduce_stock` a public admin-only endpoint would prevent regular users from
ever completing checkout.

The resolution used here: `reduce_stock` and `restore_stock` are **internal,
service-to-service endpoints**, authenticated with a separate scheme
(`InternalApiKeyAuthenticationHandler`, header `X-Internal-Api-Key`) instead
of the user's JWT. Order Service is configured with this shared key
(`InventoryService:InternalApiKey`) and sends it on every call. This keeps
"an Admin edited stock through the UI" (JWT + role check) and "the order
pipeline moved stock because a user checked out" (internal trust) as two
separate, independently auditable paths — a common pattern in real
microservice systems. **This key must also match exactly between
`InventoryService/appsettings.json` (`InternalApi:ApiKey`) and
`OrderService/appsettings.json` (`InventoryService:InternalApiKey`).**

## Order creation — transactional flow across services

`OrderManagementService.CreateOrderAsync` (Order Service):

1. Group the request's line items by `ProductId` (so the same product
   listed twice in one request is handled as one reservation).
2. For each item, call Inventory Service's `reduce_stock`, which performs an
   **atomic** `UPDATE ... WHERE stock_qty >= @qty` — no read-then-write race,
   so overselling under concurrent orders is prevented.
3. If any item comes back "product not found" or "insufficient stock", every
   item already reduced earlier in the loop is rolled back via
   `restore_stock` (compensating action), and the whole order is rejected —
   nothing is partially charged against stock.
4. Once every item's stock is successfully reduced, the `Order` + all
   `OrderItem` rows are written in **one `SaveChangesAsync` call**, which EF
   Core wraps in a single local database transaction — order and items are
   never partially persisted.
5. If step 4 throws (DB failure) *after* stock was already reduced in step
   2–3, the service compensates by restoring all reserved stock, then
   rethrows.
6. On success the order is created directly with status `CONFIRMED` (stock
   is already guaranteed reserved by that point).

`CancelOrderAsync`: loads the order, checks ownership (owner or Admin),
rejects if already `CANCELLED`, restores stock for every line item via
`restore_stock`, then updates status to `CANCELLED`.

**Known trade-off:** this is a compensating-action / saga-lite pattern, not
a true distributed transaction. If a `restore_stock` compensating call
itself fails (e.g. Inventory Service is down at that exact moment), it's
logged as `Critical` for manual reconciliation rather than blocking the
user. For a production system at larger scale you'd typically formalize
this with an outbox pattern or a saga orchestrator.

## Access rules for orders

- `POST /api/orders` — any authenticated user places an order for themselves
  (the spec calls this a "User" action; Admins are technically also allowed
  to order here since restricting them seemed arbitrary — tell me if you'd
  rather this be `[Authorize(Roles = "USER")]` strictly).
- `GET /api/orders/my-orders` — returns only the caller's own orders
  (paginated).
- `GET /api/orders/{id}` — owner or Admin only; otherwise `403 Forbidden`.
- `PATCH /api/orders/{id}/cancel` — owner or Admin only.

## Mandatory cross-cutting concerns — where they live

- **JWT validation**: `Shared/Security/JwtAuthenticationExtensions.cs`,
  used identically by all three `Program.cs` files.
- **Role-based access**: `[Authorize(Roles = ...)]` on Inventory Service's
  write endpoints; ownership checks in `OrderManagementService`.
- **Global exception handling**: `Shared/Middleware/GlobalExceptionMiddleware.cs`,
  registered first in every pipeline; converts any exception into a
  consistent JSON error body and logs it.
- **Logging**: Serilog in all three services, writing to `Logs/*.log`
  (rolling daily) and the console.

## Setting up SQL Server

```
sqlcmd -S localhost -i AuthService/Scripts/auth_db.sql
sqlcmd -S localhost -i InventoryService/Scripts/inventory_db.sql
sqlcmd -S localhost -i OrderService/Scripts/order_db.sql
```

Update the connection strings in each service's `appsettings.json` if your
SQL Server instance name, auth mode, or credentials differ from the
defaults (`Trusted_Connection=True`).

## Running in Visual Studio

1. Open `AuthInventoryOrderMicroservices.sln`.
2. Restore NuGet packages (prompted automatically, or right-click the
   solution → *Restore NuGet Packages*).
3. **Before running**, make sure `Jwt:Key` is identical in
   `AuthService/appsettings.json`, `InventoryService/appsettings.json`, and
   `OrderService/appsettings.json`; and that `InternalApi:ApiKey` in
   InventoryService matches `InventoryService:InternalApiKey` in
   OrderService. (They ship pre-matched with placeholder values — just
   replace the placeholder with the same real secret in all the right
   places if you change it.)
4. Right-click the solution → *Set Startup Projects* → *Multiple startup
   projects* → set `AuthService`, `InventoryService`, and `OrderService` all
   to **Start**.
5. Run. Ports: Auth `https://localhost:5001`, Inventory
   `https://localhost:5201`, Order `https://localhost:5301`. Order Service
   is pre-configured to call Inventory Service at `https://localhost:5201`.
6. Each service opens its own Swagger UI at `/swagger`. Use the "Authorize"
   button in Inventory/Order Service's Swagger to paste in a JWT obtained
   from Auth Service's `/api/auth/login`.

## Running from the CLI

```
# from AuthInventoryOrderMicroservices/
dotnet restore
dotnet run --project AuthService
dotnet run --project InventoryService   # separate terminal
dotnet run --project OrderService       # separate terminal
```

## Suggested end-to-end test flow

1. `POST /api/auth/register` on Auth Service — register an admin
   (`"role": "ADMIN"`) and a regular user (omit `role`, defaults to `USER`).
2. `POST /api/auth/login` as the admin → copy the returned `token`.
3. Using the admin token, `POST /api/products` on Inventory Service to
   create a product with some `stockQty`.
4. `POST /api/auth/login` as the regular user → copy that token.
5. Using the user token, `POST /api/orders` on Order Service with that
   `productId` and a `quantity`. Check the response is `201` with status
   `CONFIRMED`, then re-check the product's `stockQty` dropped accordingly.
6. Try ordering more than the remaining stock → expect `409 Conflict` and
   stock unchanged.
7. `PATCH /api/orders/{id}/cancel` on the order from step 5 → expect stock
   restored and status `CANCELLED`.

## Notes / known trade-offs

- Public registration currently allows a caller to request `"role": "ADMIN"`
  directly, for ease of testing this assignment. In a real deployment,
  remove `Role` from `RegisterRequest` (force `USER`) and provision admin
  accounts out-of-band (seed script, or an authenticated admin-only
  "create admin" endpoint).
- `dotnet restore` requires network access to nuget.org, which this sandbox
  doesn't have, so every project was written by hand rather than built
  here. They follow standard .NET 8 / EF Core 8 / ASP.NET Core JWT bearer
  conventions and should restore and run as-is in Visual Studio with normal
  internet access — but do a build right after opening the solution and let
  me know if anything doesn't compile so I can fix it.
