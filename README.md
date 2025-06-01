# User Management System (UMS) - SSO Platform Backend

## 🚀 Overview

This project is the backend for a User Management System (UMS), envisioned as a Single Sign-On (SSO) platform. It's built using .NET 8 with a focus on Clean Architecture principles and modern development practices. A key learning objective and feature of this project was the implementation of a custom Mediator pattern from scratch, mimicking functionalities of popular libraries like MediatR.

This system provides a robust foundation for managing users, including registration, and is designed to be extensible for features like login, profile management, account activation, and more.

## ✨ Features Implemented

* **User Registration:** Securely register new users.
* **Custom Mediator Pattern:**
    * Request/Response handling (`ISender`, `IRequest`, `IRequestHandler`).
    * Dedicated `ICommand` / `IQuery` interfaces with integrated Result Pattern.
    * Pipeline Behaviors (e.g., for Logging, Validation).
    * Notification Publishing (`IPublisher`, `INotification`, `INotificationHandler`).
* **Clean Architecture:**
    * **Domain:** Rich domain models (`User` aggregate root), domain events, base entities (`Entity`, `AuditableEntity`, `AggregateRoot`), soft delete (`ISoftDeletable`).
    * **Application:** Application services, commands, queries, validators, DTOs, and core application logic. CQRS pattern adherence.
    * **Infrastructure:** Data persistence (Entity Framework Core with SQL Server), external service implementations (e.g., password hashing, reference code generation).
    * **WebAPI:** Minimal API endpoints for exposing functionality, API versioning.
* **Result Pattern:** Consistent error handling and operation outcomes throughout the application.
* **FluentValidation:** Robust request validation integrated via a pipeline behavior.
* **Human-Readable IDs:** Generation of user-friendly codes for entities (e.g., `USR-YYMMDD-NNNNN`) using a database-backed daily sequence.
* **Password Hashing:** Secure password storage (demonstrated with both a custom PBKDF2 implementation for learning and placeholder for library-based approaches like BCrypt).
* **Unit of Work Pattern:** Ensures atomic operations for database transactions.
* **Organized API Endpoints:** Using Minimal API route groups for better structure.
* **API Versioning:** Configured for future API evolution.
* **Global Exception Handling:** Centralized middleware for consistent error responses.
* **Dependency Injection:** Clean and modular setup using extension methods.
* **Database Migrations:** Managed by EF Core, with configurations for snake_case naming.

## 🛠️ Tech Stack

* **.NET 8**
* **ASP.NET Core** (for WebAPI)
* **Entity Framework Core** (ORM for SQL Server)
* **SQL Server** (Database)
* **FluentValidation** (Request validation)
* **Polly** (For retry policies, e.g., in reference code generation)
* **Custom Mediator** (Implemented from scratch)
* **Minimal APIs**

## 📂 Project Structure

The solution is organized following Clean Architecture principles, with projects located within the `src/` folder:

* **`UMS.Domain`**: Contains domain entities, aggregates, value objects, domain events, and domain service interfaces. This is the core of the business logic, independent of other layers.
* **`UMS.Application`**: Contains application logic, including command/query handlers (CQRS), validation, DTOs, interfaces for infrastructure services (repositories, external services), and application service orchestrations. It depends on `UMS.Domain`.
* **`UMS.Infrastructure`**: Implements interfaces defined in the Application layer. This includes data persistence (EF Core DbContext, repositories, migrations), implementations for external services (e.g., password hashing, email service (if added), reference code generation), and other infrastructure concerns. It depends on `UMS.Application`.
* **`UMS.Mediator`**: (If you kept it separate) The custom mediator library containing `ISender`, `IPublisher`, `IRequest`, `INotification`, pipeline behavior interfaces, etc.
* **`UMS.SharedKernel`**: (If created) Contains shared code/primitives used across multiple layers, like the `Result` pattern or base classes.
* **`UMS.WebAPI`**: The presentation layer, exposing the application's functionality via ASP.NET Core Minimal APIs. It handles HTTP requests, responses, API versioning, and depends on `UMS.Application` and potentially `UMS.Infrastructure` for DI setup.

## ⚙️ Setup & Running

1.  **Prerequisites:**
    * .NET SDK (version matching the project)
    * SQL Server (e.g., LocalDB, SQL Express, or a full instance)
2.  **Clone the repository:**
    ```bash
    git clone https://github.com/gihan-aj/UMS.git
    cd UMS
    ```
3.  **Configure Database Connection:**
    * Open `src/UMS.WebAPI/appsettings.Development.json`.
    * Update the `ConnectionStrings.DefaultConnection` value to point to your SQL Server instance.
4.  **Apply Database Migrations:**
    * Open a terminal or Package Manager Console in the `src/UMS.Infrastructure/` directory (or ensure it's the default project).
    * Run: `dotnet ef database update`
        * (If you need to generate migrations first: `dotnet ef migrations add <MigrationName> -o Persistence/Migrations`)
5.  **Run the Application:**
    * Set `UMS.WebAPI` as the startup project.
    * Run from Visual Studio or use the .NET CLI:
        ```bash
        cd src/UMS.WebAPI
        dotnet run
        ```
6.  **Accessing the API:**
    * The API will typically be available at `https://localhost:<port_number>/`.
    * Swagger UI will be available at `https://localhost:<port_number>/swagger` for exploring and testing endpoints (e.g., `POST /api/v1/users/register`).

## 🔑 Key Architectural Patterns & Concepts

* **Clean Architecture:** Enforces separation of concerns, dependency inversion, and testability.
* **Domain-Driven Design (DDD) Lite:** Focus on rich domain models, aggregates (`User`), and domain events.
* **CQRS (Command Query Responsibility Segregation):** Commands modify state and queries read state, handled by distinct paths.
* **Mediator Pattern:** Decouples senders of requests from their handlers.
* **Result Pattern:** Explicitly handles success and failure states of operations.
* **Unit of Work:** Manages transactional consistency for database operations.
* **Repository Pattern:** Abstracts data access logic.
* **Pipeline Behaviors (Cross-Cutting Concerns):** For logging, validation, etc., applied to requests.
* **Minimal APIs:** Modern approach for building HTTP APIs in ASP.NET Core.
* **API Versioning:** Allows for evolving the API without breaking existing clients.

## 📖 API Endpoints (Examples)

* **`POST /api/v1/users/register`**: Registers a new user.
    * **Request Body:**
        ```json
        {
          "email": "test@example.com",
          "password": "Password123!",
          "confirmPassword": "Password123!",
          "firstName": "Test",
          "lastName": "User"
        }
        ```
    * **Success Response (201 Created):**
        ```json
        {
          "userId": "guid-of-the-new-user"
        }
        ```
        (Location header will also point to the new resource if a GET endpoint is available)
    * **Validation Error Response (400 Bad Request):**
        ```json
        {
          "type": "[https://tools.ietf.org/html/rfc7231#section-6.5.1](https://tools.ietf.org/html/rfc7231#section-6.5.1)",
          "title": "Validation Error",
          "status": 400,
          "detail": "One or more validation errors occurred.",
          "errors": {
            "Password": [
              "Password must be at least 8 characters long."
            ],
            "Email": [
              "Email is required."
            ]
          }
        }
        ```

## 🔮 Future Enhancements / TODO

* Implement User Login & Authentication (e.g., JWT).
* User Account Activation (e.g., email verification).
* Password Reset functionality.
* Role-based Authorization.
* Comprehensive Unit and Integration Testing.
* Implement `ICurrentUserService` for audit fields.
* Replace in-memory `ReferenceCodeGeneratorService` with a fully robust database-backed solution if high concurrency is expected beyond the current retry mechanism.
* More detailed API documentation (beyond Swagger).

---

*This README provides an overview of the User Management System backend. Feel free to explore the code for deeper insights into the implementation details.*
