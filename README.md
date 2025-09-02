# 🏠 Million Real Estate API

**Million Real Estate API** is a technical test implementation for a Senior .NET Developer role.  
It provides a clean and scalable REST API for managing properties, owners, and transactions in the real estate domain.  

The project is built with **.NET 8** using **C# 11**, following **Clean Architecture** and **CQRS** principles, with strong focus on code quality, performance, security, and testability.

---

## ✅ Requirements

- .NET 5 or higher (developed and tested with **.NET 8**)  
- SQL Server 2019+  
- **C# 11** (language version included in .NET 7 and .NET 8)  
- nUnit / xUnit for unit testing  

---

## ✨ Features Implemented

- **Create Property Building** – Add new property records with required metadata.  
- **Add Image to Property** – Upload and associate property images.  
- **Change Price** – Update property price and keep a change history.  
- **Update Property** – Edit existing property details.  
- **List Properties with Filters** – Query properties using filters (price, address, owner, etc.).  

Additional considerations included:  

- JWT Authentication & Role-based Authorization  
- Swagger/OpenAPI interactive documentation  
- EF Core with migrations and SQL Server support  
- Validation pipeline using FluentValidation  
- Centralized exception handling with ProblemDetails  
- Unit tests for controllers and application services  

---

## 🏗️ Architecture

The solution follows **Clean Architecture** with clear separation of concerns:

```
Million/
├── Properties.API/            # Presentation Layer (Controllers, Swagger, Middlewares)
├── Properties.Application/    # Application Layer (CQRS, DTOs, Validators, Handlers)
├── Properties.Domain/         # Domain Layer (Entities, Value Objects, Interfaces)
├── Properties.Infrastructure/ # Infrastructure Layer (EF Core, Repositories, Services)
└── Properties.UnitTests/      # Unit and Integration Tests
```

Key design patterns:
- **CQRS with MediatR** for clear separation of commands and queries.  
- **Repository & Unit of Work** for data persistence.  
- **Dependency Injection** with Scrutor for automatic registrations.  
- **Value Objects** for domain expressiveness.  

---

## 🛠️ Tech Stack

- **.NET 8 / C# 11**  
- **Entity Framework Core 8** (SQL Server)  
- **MediatR** (CQRS)  
- **FluentValidation** (validation)  
- **AutoMapper** (mapping DTOs ↔ Entities)  
- **Serilog** (logging)  
- **xUnit / Moq / FluentAssertions** (testing)  
- **Docker Compose** for local SQL Server instance  

---

## 🚀 Getting Started

### 1. Clone the Repository
```bash
git clone https://github.com/RafaPerdomo/Million-Project.git
cd Million-Project
```

### 2. Run SQL Server with Docker
```bash
docker-compose up -d
```
### 3. Run the API
```bash
Run API project in VS```

The API will be available at:  
- `https://localhost:7200`  
- `http://localhost:5200`  

---

## 🔑 Default Credentials

- **Username:** `admin`  
- **Password:** `Admin123`  

> ⚠️ Change the default password after first login.

---

## 📚 API Documentation

Swagger UI is available once the API is running:  
- [https://localhost:7200/swagger](https://localhost:7200/swagger)  

---

## 🔑 Authenticating in Swagger

To test protected endpoints in Swagger:  

1. Run the API and open [Swagger UI](https://localhost:7200/swagger).  
2. Click the **Authorize** button (top-right).  
3. Enter your token as: `Bearer {your_jwt_token}`.  
4. After authorizing, all protected endpoints will include the Authorization header automatically.  

---

## 🩺 Health Checks

The API exposes a health check endpoint at `/health`, useful for liveness and readiness probes.  

- **sql** → Verifies the SQL Server connection.  
- **memory_cache** → Verifies that IMemoryCache is working correctly.  

Example response when healthy:

```json
{
  "status": "Healthy",
  "checks": [
    { "name": "sql", "status": "Healthy", "description": "SQL Server is available" },
    { "name": "memory_cache", "status": "Healthy", "description": "Memory cache is working" }
  ]
}
```

---

## 🧪 Running Tests

```bash
dotnet test
```

Covers:
- Unit tests for Application commands/queries  
- Controller tests using WebApplicationFactory  
- Repository and EF Core integration tests  

---

## 🔒 Security Considerations

- JWT Bearer authentication with role-based access control  
- Secure file upload (image validation, size limits)  
- Sensitive settings managed via environment variables (no secrets in repo)  
- Audit logging for key operations  

---

## 📄 License

This project is provided under the **MIT License**.  
See the [LICENSE](LICENSE) file for details.  

---

<div align="center">
  Made with 💻 using .NET 8 and C# 11 – Technical Test Implementation
</div>
