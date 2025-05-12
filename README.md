```markdown
# NipeX e-Tour Management System  
**Secure Document Workflow Platform for Government Travel Authorization**  

## 🎯 Purpose  
Automates Nigeria's public sector travel approval process with:  
- **Role-Based Access Control** (Admin/Approver/User)  
- **PDF Document Signing** with audit trails  
- **Multi-Level Approval Workflows**  
- **Secure File Storage** with GUID-based naming  
- **Hash Verification** for document integrity  

---

## 🛠️ System Requirements  
| Component       | Specification                          |
|-----------------|----------------------------------------|
| **Backend**     | .NET 6 SDK, SQL Server 2019+, PowerShell 7+ |
| **Frontend**    | Node.js 16.x, npm 8.x+                |
| **Development** | Visual Studio 2022/.NET CLI, VS Code  |
| **OS**          | Windows 10+/Linux (WSL2 recommended)  |

---

## 🚀 Full Setup Guide  

### 1. Repository Setup  
```bash  
git clone https://github.com/[your-org]/NipeX-Etour-Management.git  
cd NipeX-Etour-Management  
```

### 2. Database Configuration (Critical First Step)  
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

*Run:*  
```powershell  
cd e-tour-api  
dotnet ef database update --context AppDbContext  
```  

### 3. Backend Initialization  
**Key Files:**  
- `Data/AppDbContext.cs` - DB configuration  
- `Controllers/DocumentsController.cs` - Core business logic  
- `GenerateHash.cs` - Password hashing utility  

```bash  
# Install dependencies  
dotnet restore  

# Seed initial admin (Run ONCE after DB creation)  
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
npm install --force # Resolves potential react-scripts conflicts  
```  

---

## 🔄 Running the System  

### Development Mode  
| Component  | Command          | Port  | Critical Endpoints               |
|------------|------------------|-------|-----------------------------------|
| **API**    | `dotnet run`     | 5000  | POST /api/auth/login, POST /api/documents |
| **Frontend**| `npm start`      | 3000  | /dashboard (role-based)          |

### Production Build  
```bash  
cd e-tour-frontend  
npm run build  
dotnet publish e-tour-api -c Release -o ./publish  
```  

---

## 🗂️ Critical File Map  

| Component            | Location                               | Purpose                              |
|----------------------|----------------------------------------|--------------------------------------|
| **DB Context**       | `e-tour-api/Data/AppDbContext.cs`     | Entity Framework configuration      |
| **Auth Logic**       | `Controllers/AuthController.cs`       | JWT token generation                |
| **Document Model**   | `Data/Document.cs`                    | PDF metadata schema                 |
| **Approval Workflow**| `DocumentsController.cs:Line 89`      | Multi-stage approval logic          |
| **React Routing**    | `frontend/src/App.js`                 | Dashboard access control            |
| **PDF Viewer**       | `frontend/src/components/DocumentViewer.js` | Signed PDF rendering         |

---

## 🔧 Common Customizations  

### 1. Modify Approval Workflow  
**File:** `DocumentsController.cs`  
```csharp  
// Line 135: Change approval stages  
var requiredApprovals = document.DocumentType switch  
{  
    "International" => 3, // Modify hierarchy levels  
    "Domestic" => 2,  
    _ => 1  
};  
```  

### 2. Add New User Role  
1. Update `Data/User.cs`:  
```csharp  
public enum UserRole { Admin, Approver, Auditor, User } // Add Auditor  
```  
2. Modify `AuthController.cs` role checks  
3. Update frontend `src/components/RoleRouter.js`  

### 3. Change File Storage  
**File:** `DocumentsController.cs`  
```csharp  
// Line 72: Switch to Azure Blob Storage  
await using var fileStream = new FileStream(  
    Path.Combine(_config["Azure:BlobPath"], fileName),  
    FileMode.Create);  
```  

---

## 🚨 Troubleshooting Matrix  

| Issue                | Solution                               | Verification Command                 |
|----------------------|----------------------------------------|--------------------------------------|
| **Migrations Failed**| Delete `Migrations/` folder, run `dotnet ef migrations add InitialCreate` | `dotnet ef migrations list` |
| **PDF Upload 404**   | Check `wwwroot/uploads` exists, add `UseStaticFiles()` in `Program.cs` | `curl -I http://localhost:5000/uploads/test.txt` |
| **JWT Expired**      | Update `appsettings.json` Jwt:ExpiryHours | Check `Response.Headers["X-Token-Expiry"]` |
| **Approval Stuck**   | Audit `Document.Status` enum in `Document.cs` | SQL: `SELECT Status FROM Documents WHERE Id = [ID]` |

---

## 🔒 Security Hardening  

1. **Secret Rotation**  
   - Regenerate `appsettings.json` JWT key weekly  
   - Update salt in `GenerateHash.cs` after deployment  

2. **Database Encryption**  
```sql  
-- Run on SQL Server  
CREATE COLUMN MASTER KEY [CMK_Auto1] WITH (  
    KEY_STORE_PROVIDER_NAME = N'MSSQL_CERTIFICATE_STORE',  
    KEY_PATH = N'CurrentUser/My/AAABBBBCCCDDD'  
);  
```  

3. **Audit Logs**  
```csharp  
// Add in Program.cs  
services.AddAuditLog(config =>  
{  
    config.UseEntityFramework(_ => _  
        .AuditTypeMapper(t => typeof(AuditLog))  
        .AuditEntityAction<AuditLog>((ev, ent) =>  
        {  
            ent.Action = ev.Action;  
            ent.User = ev.User;  
        }));  
});  
```  

---

## 🚀 Deployment Checklist  

1. **Production Environment Variables**  
```bash  
# Linux systemd service  
[Service]  
Environment="ASPNETCORE_ENVIRONMENT=Production"  
Environment="CONNECTIONSTRINGS__DEFAULTCONNECTION=Server=prod-db;Database=eTour..."  
```  

2. **Docker Setup**  
```dockerfile  
# API Dockerfile  
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build  
WORKDIR /src  
COPY ["e-tour-api/*.csproj", "e-tour-api/"]  
RUN dotnet restore "e-tour-api/e-tour-api.csproj"  
COPY . .  
RUN dotnet publish -c Release -o /app  

FROM mcr.microsoft.com/dotnet/aspnet:6.0  
WORKDIR /app  
COPY --from=build /app .  
ENTRYPOINT ["dotnet", "e-tour-api.dll"]  
```  

---

## 📚 Appendix  

- **License:** MIT (See `LICENSE.md` in root)  
- **Support:** etour-support@nipex.gov.ng  
- **API Docs:** `http://localhost:5000/swagger` (Enable in `Program.cs`)  
- **Data Dictionary:** Refer to `e-tour-api/Data/Models_Documentation.md`