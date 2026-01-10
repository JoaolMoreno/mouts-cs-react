# Employee Management System API

This project is a robust API for employee management, built with .NET 8, focusing on security, scalability, and architectural best practices (Clean Architecture).

## 🚀 Technologies Used

- **Runtime**: .NET 8 (C# 12)
- **Database**: PostgreSQL
- **ORM**: Entity Framework Core
- **Authentication**: JWT with HttpOnly Cookies
- **Documentation**: Swagger/OpenAPI
- **Logs**: Serilog (Structured Logging)
- **Tests**: xUnit, Moq, and FluentAssertions
- **Containerization**: Docker & Docker Compose

## 🏗️ Architecture and Modeling

The system uses UUIDs (Guid) as primary keys for all entities.

### 1. Role Hierarchy
Defined by a `Rank` (integer); lower numbers indicate higher authority.
- **Director**: Rank 1
- **Leader**: Rank 2
- **Employee**: Rank 3

### 2. Main Entities
- **Employee**: Includes FirstName, LastName, Email, Document, Role, Manager (Hierarchy).
- **Role**: Defines permissions based on Rank.

## 🔒 Security

- **JWT + HttpOnly Cookies**: Tokens are stored in secure cookies to mitigate XSS.
- **Password Hashing**: BCrypt is used for secure password storage.
- **Validation**: Strict input validation prevents invalid data entry.

## 🛠️ Business Rules

- **Age Validation**: Employees must be at least 18 years old.
- **Hierarchy Management**: Users can only manage employees with a lower rank (higher `Rank` number).
- **Visibility Scopes**:
    - **Directors**: View all employees.
    - **Leaders/Employees**: View only their direct subordinates.

## 🐳 How to Run

The project is configured for immediate execution via Docker Compose.

```bash
# Start services (API + PostgreSQL)
docker-compose up --build
```

The API will be available at: **http://localhost:5000/swagger**

### Initial Access (Seed Data)
A default administrator account is created automatically:
- **Email**: `admin@example.com`
- **Password**: `Admin@123`

## 🧪 Tests

Unit tests cover critical business logic in `EmployeeService`, including:
- Visibility filters.
- Age validation.
- Rank hierarchy rules.

## 📄 Key Endpoints

| Method | Endpoint              | Description                                      |
|--------|-----------------------|--------------------------------------------------|
| POST   | `/api/auth/login`       | Authenticates and sets the HttpOnly Cookie.      |
| GET    | `/api/auth/me`          | Retrieves current user details.                  |
| POST   | `/api/auth/refresh`     | Refreshes the access token using the refresh cookie.|
| POST   | `/api/auth/logout`      | Logs out, clearing cookies and revoking tokens.  |
| GET    | `/api/employees`        | Lists employees (Filtered by hierarchy).         |
| GET    | `/api/employees/{id}`   | Employee details (Validates permission).         |
| POST   | `/api/employees`        | Registers a new employee.                        |
| PATCH  | `/api/employees/{id}`   | Partial update of data.                          |
| DELETE | `/api/employees/{id}`   | Removes an employee.                             |
| GET    | `/api/roles`            | Lists available roles (Filtered by rank).        |
