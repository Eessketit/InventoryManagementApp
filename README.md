# InvenTrack - Dynamic Inventory Management System

Deployed Project: https://inventorymanagementapp-nh9d.onrender.com/

## 🚀 Features

- **Dynamic Data Modeling:** Create specific inventories and dynamically configure up to 15 custom data fields per item (Strings, Long Texts, Numbers, Booleans, and Document Links).
- **Custom ID Generation:** Unique sequential and configurable ID formatting logic globally scoped across inventories. 
- **Full-Text Database Search:** Highly performant search capabilities leveraging PostgreSQL's native vector engines (`NpgsqlTsVector`).
- **OAuth 2.0 & Identity:** Secure user authentication leveraging ASP.NET Core Identity, extended to support Google and GitHub external logins.
- **Third-Party Integrations:**
  - **Salesforce REST API:** Automated provision of linked Salesforce 'Accounts' and 'Contacts' using OAuth Username-Password flows.
  - **Dropbox API:** Seamless asynchronous data ingestion (e.g. system support tickets) pushed to a connected Dropbox account.
- **Role-Based Authorization:** Secure admin panels to manage, block, and provision users in real time utilizing custom request-pipeline Middleware.
- **Localization (i18n):** Deep internationalization support enabling users to switch between English and Russian languages via `.resx` files.
- **Dockerized Ecosystem:** Standardized environment runtime using Docker and `docker-compose`.

## 🛠️ Technology Stack

- **Backend:** C#, ASP.NET Core 8.0, MVC Razor Pages
- **Database:** PostgreSQL via Entity Framework Core (EF Core)
- **External Apis:** Salesforce, Dropbox, Google (OAuth), GitHub (OAuth)
- **Deployment:** Docker, Docker Compose, Render (PaaS)
- **Data Extensibility:** Includes modular support for Odoo (`inventory_importer`)

## 📦 Architecture Details

### The Dynamic Field Challenge
Often, allowing users to define their own data schemas leads to creating EAV (Entity-Attribute-Value) tables or relying on unindexable NoSQL JSON columns, both resulting in poor read performance. 

InvenTrack takes a different approach: **Fixed-Slot Dynamic Field Mapping**.
The `Item` Entity has fixed columns (`Text1`, `Text2`, `Number1`, etc.). When an admin creates an `InventoryField`, the system maps the user's custom label, type, and visibility constraints to one of these fixed slots dynamically. This yields high-performance structured SQL querying natively through EF Core, without the penalty of JOINing thousands of loose attributes.

## ⚙️ Getting Started (Local Development)

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/) & Docker Compose

### 1. Database Setup
The easiest way to get the local PostgreSQL instance running is via the included Docker configuration:
```bash
docker-compose up -d db
```

### 2. Environment Variables & User Secrets
Ensure your `appsettings.Development.json` is configured or use DotNet User Secrets for sensitive keys:

```json
{
  "Authentication": {
    "Google": { "ClientId": "...", "ClientSecret": "..." },
    "GitHub": { "ClientId": "...", "ClientSecret": "..." }
  },
  "Salesforce": {
    "ClientId": "...",
    "ClientSecret": "...",
    "LoginUrl": "..."
  },
  "Dropbox": {
    "AccessToken": "...",
    "AdminEmails": "admin@example.com"
  },
  "Seed": {
    "AdminEmail": "admin@example.com",
    "AdminPassword": "YourDevAdminPassword123!"
  }
}
```

### 3. Run EF Core Migrations
Ensure your database is up to date:
```bash
dotnet ef database update
```

### 4. Run the Application
Start the project via the .NET CLI:
```bash
dotnet run
```
Navigate to `https://localhost:5001` or `http://localhost:5000` in your browser. If configured properly, the seed script will create an initial root Admin user on startup.

## 🚢 Deployment

A `Dockerfile` is included specifically tailored for deploying to PaaS platforms like **Render**.

```yaml
# Build
docker build -t inventrack-app .
# Run
docker run -e ASPNETCORE_ENVIRONMENT=Production -e ConnectionStrings__DefaultConnection="<db_string>" -p 8080:10000 inventrack-app
```
*Note: The Dockerfile utilizes the port 10000 specifically configured for Render web-services.*

## 📜 License
This project is licensed under the MIT License.
