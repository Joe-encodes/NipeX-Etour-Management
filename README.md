# NipeX e-Tour Management System  
**Secure Document Workflow Platform for Government Travel Authorization**

---

## 🎯 Purpose  
Automates Nigeria’s public-sector travel approval process with:  
- **Role-Based Access Control** (Admin / Approver / User)  
- **PDF Document Signing** with audit trails  
- **Multi-Level Approval Workflows**  
- **Secure File Storage** with GUID-based naming  
- **Hash Verification** for document integrity  

---

## 🛠️ System Requirements  

| Component   | Specification                                 |
|-------------|-----------------------------------------------|
| **Backend** | .NET 6 SDK, SQL Server 2019+, PowerShell 7+    |
| **Frontend**| Node.js 16.x, npm 8.x+                        |
| **Dev Tools** | Visual Studio 2022 / .NET CLI, VS Code      |
| **OS**      | Windows 10+ / Linux (WSL2 recommended)         |

---

## 🚀 Full Setup Guide

### 1. Repository Setup  

```bash
git clone https://github.com/[your-org]/NipeX-Etour-Management.git
cd NipeX-Etour-Management
````

### 2. Database Configuration

**File:** `e-tour-api/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=eTourDB;Trusted_Connection=True;"
  },
  "Jwt": {
    "Key": "GENERATE_NEW_256BIT_KEY_HERE",
    "Issuer": "nipex-etour-api"
  },
  "FileStorage": {
    "UploadPath": "wwwroot/uploads",
    "MaxFileSizeMB": 50
  }
}
```

*Run migrations:*

```powershell
cd e-tour-api
dotnet ef database update --context AppDbContext
```

### 3. Backend Initialization

* **Key Files:**

  * `Data/AppDbContext.cs` — DB configuration
  * `Controllers/DocumentsController.cs` — Core business logic
  * `GenerateHash.cs` — Password-hashing utility

```bash
# Install dependencies
dotnet restore

# Seed initial admin (run ONCE after DB creation)
dotnet run --project e-tour-api -- seed-admin
```

### 4. Frontend Setup

**File:** `e-tour-frontend/.env`

```env
REACT_APP_API_URL=http://localhost:5000
REACT_APP_ENV=development
```

```bash
cd e-tour-frontend
npm install --force   # fixes potential react-scripts conflicts
```

---

## 🔄 Running the System

### Development Mode

| Component    | Command      | Port | Key Endpoints                                 |
| ------------ | ------------ | ---- | --------------------------------------------- |
| **API**      | `dotnet run` | 5000 | `POST /api/auth/login`, `POST /api/documents` |
| **Frontend** | `npm start`  | 3000 | `/dashboard` (role-based views)               |

### Production Build

```bash
cd e-tour-frontend
npm run build

dotnet publish e-tour-api -c Release -o ./publish
```

---

## 🗂️ Critical File Map

| Component             | Location                                           | Purpose                    |
| --------------------- | -------------------------------------------------- | -------------------------- |
| **DB Context**        | `e-tour-api/Data/AppDbContext.cs`                  | EF Core configuration      |
| **Auth Logic**        | `Controllers/AuthController.cs`                    | JWT token generation       |
| **Document Model**    | `Data/Document.cs`                                 | PDF metadata schema        |
| **Approval Workflow** | `Controllers/DocumentsController.cs` (line 89)     | Multi-stage approval logic |
| **React Routing**     | `e-tour-frontend/src/App.js`                       | Dashboard access control   |
| **PDF Viewer**        | `e-tour-frontend/src/components/DocumentViewer.js` | Signed PDF rendering       |

---

## 🔧 Common Customizations

### 1. Modify Approval Workflow

**File:** `Controllers/DocumentsController.cs`

```csharp
// Line 135: Change approval stages
var requiredApprovals = document.DocumentType switch
{
    "International" => 3, // add/remove levels
    "Domestic"     => 2,
    _              => 1
};
```

### 2. Add a New User Role

1. **Update** `Data/User.cs`:

   ```csharp
   public enum UserRole { Admin, Approver, Auditor, User } // Added Auditor
   ```
2. **Adjust** `AuthController.cs` role checks
3. **Extend** `e-tour-frontend/src/components/RoleRouter.js`

### 3. Swap to Azure Blob Storage

**File:** `Controllers/DocumentsController.cs`

```csharp
// Line 72: Use Azure Blob instead of local FS
await using var fileStream = new FileStream(
    Path.Combine(_config["Azure:BlobPath"], fileName),
    FileMode.Create);
```

---

## 🚨 Troubleshooting Matrix

| Issue                 | Solution                                                                    | Verification Command                                   |
| --------------------- | --------------------------------------------------------------------------- | ------------------------------------------------------ |
| **Migrations Failed** | Delete `Migrations/`, then run `dotnet ef migrations add InitialCreate`     | `dotnet ef migrations list`                            |
| **PDF Upload 404**    | Ensure `wwwroot/uploads` exists; add `app.UseStaticFiles()` in `Program.cs` | `curl -I http://localhost:5000/uploads/test.txt`       |
| **JWT Expired**       | Increase `Jwt:ExpiryHours` in `appsettings.json`                            | Check `Response.Headers["X-Token-Expiry"]`             |
| **Approval Stuck**    | Audit `Document.Status` enum in `Data/Document.cs`                          | `SELECT Status FROM Documents WHERE Id = [ID];` in SQL |

---

## 🔒 Security Hardening

1. **Secret Rotation**

   * Rotate JWT key weekly in `appsettings.json`
   * Regenerate salt in `GenerateHash.cs` post-deploy

2. **Database Encryption**

   ```sql
   CREATE COLUMN MASTER KEY [CMK_Auto1]  
   WITH (
     KEY_STORE_PROVIDER_NAME = N'MSSQL_CERTIFICATE_STORE',
     KEY_PATH = N'CurrentUser/My/AAABBBBCCCDDD'
   );
   ```

3. **Audit Logs**

   ```csharp
   // In Program.cs
   services.AddAuditLog(config =>
   {
       config.UseEntityFramework(_ => _
           .AuditTypeMapper(t => typeof(AuditLog))
           .AuditEntityAction<AuditLog>((ev, ent) =>
           {
               ent.Action = ev.Action;
               ent.User   = ev.User;
           }));
   });
   ```

---

## 🚀 Deployment Checklist

1. **Set Production ENV vars (systemd example):**

   ```ini
   [Service]
   Environment="ASPNETCORE_ENVIRONMENT=Production"
   Environment="CONNECTIONSTRINGS__DEFAULTCONNECTION=Server=prod-db;Database=eTour..."
   ```

2. **Dockerize API**

   ```dockerfile
   # Build stage
   FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
   WORKDIR /src
   COPY ["e-tour-api/*.csproj", "e-tour-api/"]
   RUN dotnet restore "e-tour-api/e-tour-api.csproj"
   COPY . .
   RUN dotnet publish -c Release -o /app

   # Runtime stage
   FROM mcr.microsoft.com/dotnet/aspnet:6.0
   WORKDIR /app
   COPY --from=build /app .
   ENTRYPOINT ["dotnet", "e-tour-api.dll"]
   ```

---

## 📚 Appendix

* **License:** MIT (`LICENSE.md`)
* **Support:** [etour-support@nipex.gov.ng](mailto:etour-support@nipex.gov.ng)
* **API Docs:** `http://localhost:5000/swagger` (enable in `Program.cs`)
* **Data Dictionary:** `e-tour-api/Data/Models_Documentation.md`

```

Feel free to tweak any sections to match your org’s conventions or add badges (build, coverage, etc.) at the top. This layout keeps everything scannable and Git-friendly!
```

