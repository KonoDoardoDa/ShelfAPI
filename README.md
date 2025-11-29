# ShelfApi

Small ASP.NET Core Web API (NET 9) using Dapper and MySQL to manage Items, Fornecedors (suppliers) and Kits.

Overview
- Language: C# 13 / .NET 9
- Data access: Dapper over MySql.Data
- DI is configured in Program.cs; controllers are minimal and the app exposes Swagger in Development.

Features
- CRUD for Item (controller implemented)
- Repository + Service layers for Item, Fornecedor and Kit
- Kit details aggregation (Kit with its KitItems)
- Validation via services (example: item creation checks supplier existence)
- Swagger UI available in Development

Requirements
- .NET 9 SDK
- MySQL (or compatible) server
- Visual Studio 2022 or dotnet CLI

Quick start (dotnet CLI)
1. Update connection string in appsettings.json (or override with environment variables):
   - Key: ConnectionStrings:DefaultConnection
2. Restore, build and run:
   dotnet restore
   dotnet build
   dotnet run

Quick start (Visual Studio)
1. Open the solution in Visual Studio 2022.
2. Update __appsettings.json__ connection string.
3. Run using __Debug > Start Debugging__ or press __F5__.

Configuration
- appsettings.json contains:
  - ConnectionStrings:DefaultConnection — MySQL connection string used by IDbConnection registration in Program.cs.
- Swagger is added in Program.cs and used when the environment is development.

Database schema (suggested)
The repository code expects the following tables/columns. Adjust types/constraints to your environment.

    CREATE TABLE Fornecedor (
      Id INT AUTO_INCREMENT PRIMARY KEY,
      Nome VARCHAR(255) NOT NULL
    );

    CREATE TABLE Item (
      Id INT AUTO_INCREMENT PRIMARY KEY,
      Nome VARCHAR(255),
      FornecedorId INT,
      Descricao TEXT,
      FOREIGN KEY (FornecedorId) REFERENCES Fornecedor(Id)
    );

    CREATE TABLE Kit (
      Id INT AUTO_INCREMENT PRIMARY KEY,
      Nome VARCHAR(255)
    );

    CREATE TABLE KitItem (
      KitId INT,
      ItemId INT,
      Quantidade INT DEFAULT 0,
      IsPainel TINYINT(1) DEFAULT 0,
      PRIMARY KEY (KitId, ItemId),
      FOREIGN KEY (KitId) REFERENCES Kit(Id),
      FOREIGN KEY (ItemId) REFERENCES Item(Id)
    );

API (implemented endpoints)
Controller: api/Item
- GET  api/Item/GetAllItems
  - Returns: IList<Item>
- GET  api/Item/GetItemById?id={id}
  - Returns: Item or 404
- POST api/Item/CreateItem
  - Body: Item (JSON)
  - Returns: created Id or 404 if supplier does not exist
- PUT  api/Item/UpdateItem
  - Body: Item (JSON)
  - Returns: 204 or 404
- DELETE api/Item/DeleteItem?id={id}
  - Returns: 204 or 404

Examples (curl)
- Get all items:
  curl -sS https://localhost:5001/api/Item/GetAllItems

- Create item:
  curl -X POST https://localhost:5001/api/Item/CreateItem \
    -H "Content-Type: application/json" \
    -d '{"nome":"Item A","fornecedorId":1,"descricao":"desc"}'

Notes & implementation details
- Dapper is used directly; repository methods use parameterized SQL to avoid injection.
- Program.cs registers IDbConnection scoped with a MySqlConnection using the configured connection string.
- Swagger UI is enabled only in development (see Program.cs).
- Service layer throws domain-specific exceptions (e.g., ItemNotFoundException, InexistentProviderException) that controllers map to HTTP responses.
- Replace plain connection string in repo with secure storage (user secrets or environment variables) for production.