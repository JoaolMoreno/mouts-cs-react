# Employee Management System

A comprehensive full-stack application for managing employees, featuring a .NET 8 Backend, a Next.js 14 Frontend, and a PostgreSQL database.

## 🚀 Quick Start (Docker)

The easiest way to run the entire application is using Docker Compose.

1. **Prerequisites**: Ensure [Docker Desktop](https://www.docker.com/products/docker-desktop) is installed and running.
2. **Run the Application**:

   ```bash
   docker-compose up --build
   ```

3. **Access the Application**:
   - **Frontend**: [http://localhost:3000](http://localhost:3000)
   - **Backend API**: [http://localhost:5198/swagger](http://localhost:5198/swagger) (Swagger UI)

## 📂 Project Structure

This repository is organized into the following modules:

- **[Frontend](./Frontend/README.md)**: Next.js 14 application with a custom "Corporate Light" design system, secure authentication, and responsive layout.
- **[Backend](./Backend/README.md)**: .NET 8 Web API following Clean Architecture principles, utilizing Entity Framework Core and PostgreSQL.

## 🛠️ Tech Stack

### Frontend
- **Framework**: Next.js 14 (App Router)
- **Language**: TypeScript
- **Styling**: SCSS Modules
- **State**: React Context API

### Backend
- **Framework**: .NET 8
- **Database**: PostgreSQL
- **ORM**: Entity Framework Core
- **Auth**: JWT with HttpOnly Cookies

### Infrastructure
- **Containerization**: Docker & Docker Compose
- **Orchestration**: Multi-container setup for Frontend, Backend, and Database.

## ✨ Key Features
- **Secure Authentication**: HttpOnly Cookie-based auth with secure logout.
- **Role-Based Access**: Role management and protection.
- **Employee CRUD**: Complete management of employee records.
- **Modern UI**: Polished, responsive design.
