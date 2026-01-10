# Employee Management System - Frontend

This is the frontend application for the Employee Management System, built with **Next.js 14** (App Router), **TypeScript**, and **SCSS**. It features a premium "Corporate Premium" design aesthetic.

## 🚀 Technologies Used

-   **Framework**: [Next.js 14](https://nextjs.org/) (App Router)
-   **Language**: TypeScript
-   **Styling**: SCSS Modules + Global Design System
-   **Icons**: React Icons (Lucide)
-   **State/Data**: Server Actions (implied by Next.js structure) or Client Components

## 🎨 Design System

The application follows a **"Corporate Premium"** aesthetic, characterized by:
-   **Deep Forest** and **Corporate Green** color palette.
-   **Outfit** typography for a modern, professional look.
-   **Glassmorphism** and subtle gradients.
-   **Dark Mode** by default.

See [DESIGN_SYSTEM.md](DESIGN_SYSTEM.md) for full details on tokens, typography, and SCSS architecture.

## 📂 Project Structure

```text
src/
├── app/
│   ├── (dashboard)/        # Protected application layout (Sidebar, Navbar)
│   │   ├── employees/      # Employee management (List, Create, Edit)
│   │   └── ...
│   ├── login/              # Authentication page
│   ├── globals.css         # Global styles/resets
│   └── layout.tsx          # Root layout
├── components/             # Reusable UI components
│   ├── EmployeeForm.tsx    # Shared form for Create/Edit
│   ├── Navbar.tsx          # Top navigation
│   └── Sidebar.tsx         # Side navigation
├── styles/                 # Global SCSS resources
│   ├── abstract/           # Variables, Mixins
│   └── base/               # Reset, Typography
└── services/               # API integration services
```

## 🛠️ Features

-   **Authentication**: Login interface with JWT integration capabilities.
    -   Secure HttpOnly Cookie storage.
    -   Logout functionality clearing server-side cookies.
    -   "Show/Hide Password" toggle.
-   **Layout**:
    -   **Corporate Light Theme**: Clean light background for workspace, Dark "Deep Forest" Sidebar for brand identity.
    -   Responsive Navbar with User Profile and Logout.
-   **Employee Management**:
    -   **List**: View all employees with sorting/filtering capabilities.
    -   **Create**: Add new employees with role selection.
    -   **Edit**: Update existing employee details (Role, Rank, Manager, etc.).
    -   **Delete**: Remove records with confirmation.

## 🐳 How to Run (Docker)

The entire application (Frontend + Backend + Search + Database) is containerized.

```bash

docker-compose up --build
```

Access the frontend at: [http://localhost:3000](http://localhost:3000)

## 💻 Local Development

### Prerequisites
- Node.js 18+
- Backend API running on port 5198

### Run Frontend Only
```bash
# Install dependencies
npm install

# Run development server
npm run dev
```

Open [http://localhost:3000](http://localhost:3000) with your browser to see the result.
