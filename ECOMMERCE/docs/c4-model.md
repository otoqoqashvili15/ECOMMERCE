# C4 model

The diagrams describe the e-commerce application at three C4 zoom levels. The model follows the C4 concepts in the supplied architecture guide: people use a software system; deployable applications and data stores are containers; related implementation responsibilities inside an application are components.

## Level 1 — System context

```mermaid
flowchart LR
    customer["Person<br/>Customer<br/><small>Browses products, manages a basket, places and tracks orders</small>"]
    manager["Person<br/>Store manager / administrator<br/><small>Maintains catalog, categories, stock, and order status</small>"]
    store["Software system<br/>Atelier Commerce<br/><small>Online product catalog and order management</small>"]

    customer -->|Searches, saves, reviews, orders| store
    manager -->|Manages products, inventory, and fulfilment| store

    classDef person fill:#084b79,color:#fff,stroke:#063654,stroke-width:2px;
    classDef system fill:#2e6c50,color:#fff,stroke:#214b38,stroke-width:2px;
    class customer,manager person;
    class store system;
```

## Level 2 — Containers

```mermaid
flowchart LR
    customer["Person<br/>Customer"]
    manager["Person<br/>Store manager"]
    browser["Container: Storefront<br/>Angular 20 SPA<br/>Runs in the user's browser"]
    web["Container: Web delivery<br/>Nginx<br/>Serves Angular files and proxies /api"]
    api["Container: Commerce API<br/>ASP.NET Core Web API<br/>JWT auth, catalog, basket, orders"]
    db[("Container: Commerce database<br/>SQL Server 2022<br/>Identity and commerce data")]

    customer -->|Uses HTTPS / browser| browser
    manager -->|Uses HTTPS / browser| browser
    browser -->|Loads SPA assets| web
    browser -->|JSON over HTTP; Bearer token for private routes| web
    web -->|Forwards /api and /openapi| api
    api -->|EF Core 10 / SQL| db

    classDef person fill:#084b79,color:#fff,stroke:#063654,stroke-width:2px;
    classDef app fill:#4384b5,color:#fff,stroke:#1c5d8e,stroke-width:2px;
    classDef store fill:#22684a,color:#fff,stroke:#16442f,stroke-width:2px;
    class customer,manager person;
    class browser,web,api app;
    class db store;
```

## Level 3 — API components

```mermaid
flowchart TB
    client["Angular storefront<br/>Customer and manager experiences"]
    subgraph api["ASP.NET Core Commerce API"]
        auth["Authentication and users<br/>Registration, login, JWT, roles, profiles"]
        catalog["Catalog management<br/>Products, categories, search, discounts, inventory"]
        reviews["Product reviews<br/>Ratings and comments"]
        basket["Basket management<br/>Per-user cart and quantities"]
        orders["Order management<br/>Checkout, stock reservation, status, history"]
        admin["Store operations<br/>Low-stock view and sales summary"]
        data["Persistence layer<br/>EF Core DbContext, migrations, SQL Server"]
    end
    db[("SQL Server")]

    client -->|Public and protected REST endpoints| auth
    client --> catalog
    client --> reviews
    client --> basket
    client --> orders
    client --> admin
    auth --> data
    catalog --> data
    reviews --> data
    basket --> data
    orders --> data
    admin --> data
    data --> db

    classDef apiComponent fill:#83b6e3,color:#102b40,stroke:#3977a5,stroke-width:1px;
    classDef persistence fill:#22684a,color:#fff,stroke:#16442f,stroke-width:2px;
    class auth,catalog,reviews,basket,orders,admin apiComponent;
    class data,db persistence;
```

## Domain data view

This is a supporting entity view, not a fourth C4 diagram. The C4 guide describes the code-level view as optional; this relationship view makes the SQL model easier to review.

```mermaid
erDiagram
    APPLICATION_USER ||--o{ STORE_ORDER : places
    APPLICATION_USER ||--o| CART : owns
    APPLICATION_USER ||--o{ PRODUCT_REVIEW : writes
    CATEGORY ||--o{ PRODUCT : groups
    CATEGORY o|--o{ CATEGORY : parent_of
    PRODUCT ||--o{ PRODUCT_IMAGE : has
    PRODUCT ||--o{ PRODUCT_REVIEW : receives
    CART ||--o{ CART_ITEM : contains
    PRODUCT ||--o{ CART_ITEM : selected
    STORE_ORDER ||--|{ STORE_ORDER_ITEM : contains
    PRODUCT ||--o{ STORE_ORDER_ITEM : referenced
```

### Boundaries and assumptions

- The Angular application keeps guest basket state in browser storage; signed-in baskets are stored through the API.
- The API owns authorization and validates inventory at checkout. The browser is not trusted to set order totals.
- SQL Server is the durable store. The API applies EF Core migrations at startup and seeds sample catalog data once.
- The brief marks payment-gateway integration optional. This version records cash on delivery and does not connect to a payment provider.
- Product seed photos use remote Unsplash image URLs; replace them with owned or licensed media for a deployed shop.
