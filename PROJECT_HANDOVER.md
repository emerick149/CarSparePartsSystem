# Car Spare Parts System — Project Handover Document

## 1. Project Overview
- **Name:** Car Spare Parts Inventory & Compatibility Management System (Lab 3)
- **Type:** C# .NET Windows Forms (WinForms) desktop app, built in Visual Studio
- **Architecture:** MVC — folders `Models/`, `Views/`, `Controllers/`, `Data/`
- **Database:** MySQL via XAMPP (localhost, user root, no password), database `CarSparePartsDB`
- **Data access:** MySqlConnector NuGet package (raw ADO.NET SQL — NOT Entity Framework)
- **Security:** BCrypt.Net-Next NuGet package for password hashing
- **Source SRS:** Car_Spare_Parts_Functional_and_Subfunctional_Requirements.pdf (FR-01…FR-11, SFR-01…SFR-20, NFR-01…NFR-12)

---

## 2. STRICT Conventions (do not violate)
- **Code-only UI.** All forms create controls programmatically in a private `InitializeComponents()` method called from the constructor. Form classes inherit `Form` directly, are NOT partial, and have NO `.Designer.cs` files. Never use the Visual Studio designer/toolbox.
- **No console commands.** No Package Manager Console / dotnet CLI. Packages installed via NuGet GUI; database created automatically at startup.
- **Auto DB creation:** `Program.Main()` calls `DatabaseInitializer.InitializeDatabase()` before `Application.Run(new LoginForm())`. It runs `CREATE DATABASE IF NOT EXISTS`, `CREATE TABLE IF NOT EXISTS` for all tables, then seeds data only if tables are empty.
- `Program.cs` uses `Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);` (NOT `ApplicationConfiguration.Initialize()` — that caused CS0103).
- **Connection string** (read from `App.config` key `ConnectionStrings/CarSparePartsDB`):
  ```
  Server=localhost;Database=CarSparePartsDB;User ID=root;Password=;
  ```
  All controllers and forms read it via `AppConfig.ConnectionString` (see `Data/AppConfig.cs`).

---

## 3. Database Schema (MySQL)

```sql
Users(Id INT AI PK, Email VARCHAR(100) UNIQUE, PasswordHash VARCHAR(255), Role VARCHAR(20))
-- Role: 'Customer' | 'Administrator'

SpareParts(Id INT AI PK, Sku VARCHAR(50) UNIQUE, Name VARCHAR(100), Category VARCHAR(50), Price DECIMAL(18,2), StockQuantity INT)

VehicleCompatibility(Id INT AI PK, SparePartId FK→SpareParts ON DELETE CASCADE, Make VARCHAR(50), Model VARCHAR(50), YearStart INT, YearEnd INT)

Orders(Id INT AI PK, CustomerId FK→Users, OrderDate DATETIME DEFAULT CURRENT_TIMESTAMP, Status VARCHAR(20) DEFAULT 'New', TotalAmount DECIMAL(18,2))
-- Status: New | Confirmed | Ready | Completed | Cancelled

OrderItems(Id INT AI PK, OrderId FK→Orders ON DELETE CASCADE, SparePartId FK→SpareParts, Quantity INT, UnitPrice DECIMAL(18,2))
```

**Seeded data:** admin `admin@carparts.com` / `admin123` (BCrypt-hashed, Role `Administrator`) + 12 sample spare parts (categories: Brakes, Engine, Filters, Suspension, Electrical).

---

## 4. Current File Inventory

| File | Purpose |
|---|---|
| `Program.cs` | Entry point; runs DatabaseInitializer; starts LoginForm |
| `Data/DatabaseInitializer.cs` | Creates DB + tables; seeds admin + 12 parts if empty |
| `Data/AppConfig.cs` | Centralised connection string read from App.config (NFR-12) |
| `Models/User.cs` | Id, Email, PasswordHash, Role |
| `Models/SparePart.cs` | Id, Sku, Name, Category, Price, StockQuantity |
| `Models/CartManager.cs` | CartItem + static cart (AddToCart, GetTotal, Clear) |
| `Controllers/AuthController.cs` | Register (trim+lowercase email, duplicate check, BCrypt hash, Role=Customer), Login (BCrypt verify, returns User or null) |
| `Controllers/SparePartController.cs` | GetDashboardStats (incl. fitment count + category list), GetAllParts(search, sort), AddPart (unique SKU), UpdatePart (self-SKU allowed), DeletePart (blocks if in VehicleCompatibility OR OrderItems) |
| `Controllers/CustomerController.cs` | GetAvailableParts(search, category, inStockOnly, sort), GetCategories(), GetCompatibleVehicles(partId), GetCustomerOrders(id), PlaceOrder, CancelOrder |
| `Controllers/VehicleCompatibilityController.cs` | GetFitments(partId), AddFitment(partId, make, model, yearStart, yearEnd), DeleteFitment(id), SearchCompatibleParts(make, model, year) |
| `Controllers/OrderController.cs` | GetAllOrders(search), UpdateOrderStatus(orderId, newStatus), CancelOrderAsAdmin(orderId) |
| `Controllers/CustomerManagementController.cs` | GetCustomerList(search), GetCustomerStats() |
| `Views/LoginForm.cs` | Login UI — routes to Admin or Customer dashboard |
| `Views/RegisterForm.cs` | Registration UI (password max-length enforced) |
| `Views/AdminDashboardForm.cs` | Stats labels (TotalParts/TotalStock/LowStock/Value/Fitments/Categories), buttons: Manage Parts, Vehicle Compat., Order Management, Customers, Logout |
| `Views/ManageSparePartsForm.cs` | DataGridView CRUD for parts + search + sort; back button passes currentUser |
| `Views/VehicleCompatibilityForm.cs` | Admin form: list/add/delete fitments per part; validate YearStart ≤ YearEnd |
| `Views/SearchByVehicleForm.cs` | Customer/admin: find parts compatible with a make/model/year |
| `Views/OrderManagementForm.cs` | Admin grid of ALL orders; status transition buttons with rules |
| `Views/CustomerManagementForm.cs` | Admin view of customers with stats |
| `Views/CustomerDashboardForm.cs` | Buttons: Browse Parts, Order History, Find By Vehicle, Logout |
| `Views/BrowsePartsForm.cs` | Search + category combo + in-stock checkbox + sort; Add to Cart; View Cart |
| `Views/CartForm.cs` | Cart grid + transactional multi-item checkout |
| `Views/OrderHistoryForm.cs` | Customer's own orders grid with line-item detail + cancel button |

---

## 5. Requirements Status

**DONE:** FR-01, FR-02, FR-03, FR-04, FR-05, FR-06, FR-07, FR-08, FR-09, FR-10, FR-11, NFR-02, NFR-04, NFR-12, SFR-01 through SFR-20.

---

## 6. Order Status Transition Rules (SFR-20)

| From | Allowed transitions |
|---|---|
| New | → Confirmed, → Cancelled |
| Confirmed | → Ready, → Cancelled |
| Ready | → Completed, → Cancelled |
| Completed | (terminal — no changes) |
| Cancelled | (terminal — no changes) |

Stock is restored exactly once when an order is cancelled (regardless of who cancels — admin or customer).

---

## 7. Default Credentials
- **Admin:** `admin@carparts.com` / `admin123`
- **Customer:** register via the Register screen
