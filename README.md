# NipeX e-Tour Management System

**Secure Document Workflow Platform for Government Travel Authorization**

---

## 🔍 Quick Start

1. **Clone the repository**

   ```bash
   git clone https://github.com/[your-org]/NipeX-Etour-Management.git
   cd NipeX-Etour-Management
   ```
2. **Install prerequisites**

   * **.NET SDK 8**: Download and install from [https://dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
   * **Node.js 16+ & npm**: Install from [https://nodejs.org/](https://nodejs.org/)
   * **PostgreSQL client (`psql`)**: Ensure it's on your PATH.
3. **Configure environment**

   * Copy the example environment file and fill in your secrets:

     ```bash
     cp .env.example .env
     # Edit .env and fill in your secrets and config
     ```
   * **Never commit your real `.env` file to the repository.**
   * The `.env.example` file provides all required variable names and example values.

4. **Apply database migrations**

   ```bash
   cd e-tour-api
   dotnet tool run dotnet-ef database update --context AppDbContext
   ```
5. **Run the applications (with Docker Compose)**

   ```bash
   docker-compose up --build
   ```
   Or run each service manually as described below.

Browse:

* **React UI** → [http://localhost:3000](http://localhost:3000)
* **Swagger API docs** → [http://localhost:5000/swagger](http://localhost:5000/swagger)

---

## 🎯 Purpose

Automates Nigeria's public-sector travel approval process with:

* **Role-Based Access Control** (Admin / Approver / User)
* **PDF Document Signing** with audit trails
* **Multi-Level Approval Workflows**
* **Secure File Storage** with GUID-based naming
* **Hash Verification** for document integrity

---

## 🛠 System Requirements

| Component     | Specification                      |
| ------------- | ---------------------------------- |
| **Backend**   | .NET SDK 8                         |
| **Frontend**  | Node.js 16.x & npm                 |
| **Database**  | PostgreSQL (client for migrations) |
| **Dev Tools** | VS Code / Visual Studio / .NET CLI |
| **OS**        | Windows 10+ / Linux / macOS        |

---

## 🚀 Full Setup Guide

### 1. Repository Setup

```bash
# Already covered in Quick Start
```

### 2. Environment Variables & Security

- **All secrets and environment-specific values must be set in your `.env` file.**
- **Never commit your real `.env` file.** The `.env.example` file is provided for reference.
- The backend, frontend, and database all read their configuration from environment variables.
- For production, set these variables in your hosting platform's secrets or environment config.

#### Example `.env` (see `.env.example` for all variables):

```env
REACT_APP_API_URL=http://localhost:5000
DB_USER=your_db_user
DB_PASSWORD=your_db_password
DB_NAME=etourdb
DB_HOST=db
JWT_KEY=your_jwt_key
JWT_ISSUER=http://localhost:5000
JWT_AUDIENCE=http://localhost:5000
ASPNETCORE_ENVIRONMENT=Development
```

### 3. Backend Initialization

```bash
cd e-tour-api
# Restore packages and tools
dotnet restore
# (Re-)generate dotnet-ef if needed
# dotnet tool restore
# Apply migrations
dotnet tool run dotnet-ef database update --context AppDbContext
```

### 4. Frontend Setup

```bash
cd e-tour-frontend
# Install exact dependencies
npm ci
```

Ensure `.env` exists and contains at least:

```env
REACT_APP_API_URL=http://localhost:5000
REACT_APP_ENV=development
```

### 5. Running in Development

| Service | Command      | Port | Notes                            |
| ------- | ------------ | ---- | -------------------------------- |
| **API** | `dotnet run` | 5000 | Automatically reloads on changes |
| **UI**  | `npm start`  | 3000 | Hot-reloads React                |

Or use Docker Compose for all services:

```bash
docker-compose up --build
```

---

## 🗂️ Critical File Map

| Component         | Path                                            | Purpose                       |
| ----------------- | ----------------------------------------------- | ----------------------------- |
| **DB Context**    | `e-tour-api/Data/AppDbContext.cs`               | EF Core models & seeding      |
| **Migrations**    | `e-tour-api/Migrations/`                        | Versioned schema changes      |
| **Auth Logic**    | `e-tour-api/Controllers/AuthController.cs`      | JWT token endpoints           |
| **Documents API** | `e-tour-api/Controllers/DocumentsController.cs` | Core document operations      |
| **Hash Utility**  | `e-tour-api/Utilities/HashGenerator.cs`         | Password hashing              |
| **React Entry**   | `e-tour-frontend/src/App.js`                    | Routing & layout              |
| **Env Example**   | `.env.example`                                  | All environment settings      |

---

## 🔧 Customizations & Tips

* **Approval Workflow**: Tweak in `DocumentsController.cs` switch-case on `DocumentType`.
* **Add Roles**: Extend `Data/User.cs` enum and update both backend & UI routing.
* **Change Storage**: Swap local FS to cloud in the upload code path.

---

## 🚨 Troubleshooting

1. **Migrations error**: Drop DB, clear migrations folder, re-create initial migration:

   ```bash
   dotnet tool run dotnet-ef database drop -f --context AppDbContext
   dotnet ef migrations remove # until empty
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```
2. **Duplicates**: Unique key violations mean seed data already exists—use a fresh DB or adjust seed IDs.
3. **Missing fonts**: If PDF rendering errors, ensure iText7 StandardFonts package is referenced.

---

## 📚 Appendix

* **License**: MIT (`LICENSE.md`)
* **Support**: [etour-support@nipex.gov.ng](mailto:etour-support@nipex.gov.ng)
* **Swagger**: `http://localhost:5000/swagger`
* **Data Dictionary**: `e-tour-api/Data/Models_Documentation.md`

---

## 🔒 Security & Best Practices

- **Never commit secrets or real environment files.**
- Use `.env.example` as a template for your own `.env`.
- For production, use your hosting platform's secret management.
- The frontend never exposes secrets—only public config like API URLs.
- The backend always prefers environment variables for sensitive values.
