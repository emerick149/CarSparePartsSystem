# Car Spare Parts Inventory & Compatibility Management System

A Windows Forms desktop application for managing car spare parts inventory, vehicle compatibility, and customer orders. Built with C# .NET Framework 4.7.2 and MySQL.

---

## Prerequisites

| Requirement | Details |
|---|---|
| Visual Studio 2019 or later | With .NET desktop development workload |
| XAMPP | MySQL service must be running on `localhost:3306` |
| .NET Framework 4.7.2 | Included with Windows 10/11 |

No manual database setup is needed — the app creates and seeds the database automatically on first launch.

---

## Getting Started

1. **Start XAMPP** and ensure the MySQL service is running.
2. **Open** `CarSparePartsSystem.slnx` in Visual Studio.
3. **Press F5** to build and run.
4. The app will auto-create the `CarSparePartsDB` database, all tables, and seed sample data.
5. Log in with the default admin account:
   - Email: `admin@carparts.com`
   - Password: `admin123`

---

## Default Accounts

| Role | Email | Password |
|---|---|---|
| Administrator | `admin@carparts.com` | `admin123` |
| Customer | Register via the app | — |

---

## Features

### Customer
- **Register & Login** — email/password authentication with BCrypt hashing.
- **Browse Parts** — search by name or SKU, filter by category, toggle in-stock only. Click any column header to sort.
- **Find Parts by Vehicle** — search compatible parts by make, model, and year.
- **Shopping Cart** — add multiple parts, adjust quantities, checkout in a single transaction.
- **Order History** — view all past orders with per-item line totals. Cancel eligible orders (New or Confirmed status).

### Administrator
- **Inventory Dashboard** — live stats: total parts, total stock units, low-stock items, inventory value, fitment record count, and active categories.
- **Manage Spare Parts** — add, update, delete parts with search and sortable grid. SKU uniqueness enforced; deletion blocked if the part is in existing orders or has fitment records.
- **Vehicle Compatibility** — attach make/model/year-range fitment records to parts, delete them, and search parts by vehicle.
- **Order Management** — view all customer orders, advance status through the workflow, or cancel with automatic stock restoration.
- **Customer Management** — view all registered customers with per-customer order counts and lifetime spend.

### Order Status Workflow

```
New → Confirmed → Ready → Completed
 ↓        ↓         ↓
Cancelled Cancelled Cancelled
```

Completed and Cancelled are terminal — no further changes allowed. Stock is restored automatically when any order is cancelled.

---

## Project Structure

```
CarSparePartsSystem/
├── Program.cs                          # Entry point
├── App.config                          # Connection string (NFR-12)
├── Data/
│   ├── AppConfig.cs                    # Reads connection string from App.config
│   └── DatabaseInitializer.cs          # Auto-creates DB, tables, and seed data
├── Models/
│   ├── User.cs
│   ├── SparePart.cs
│   └── CartManager.cs                  # Static in-memory cart
├── Controllers/
│   ├── AuthController.cs               # Register, Login
│   ├── SparePartController.cs          # Inventory CRUD + dashboard stats
│   ├── CustomerController.cs           # Browse, order, cancel, history
│   ├── VehicleCompatibilityController.cs
│   ├── OrderController.cs              # Admin order management + status transitions
│   └── CustomerManagementController.cs
└── Views/
    ├── LoginForm.cs
    ├── RegisterForm.cs
    ├── AdminDashboardForm.cs
    ├── ManageSparePartsForm.cs
    ├── VehicleCompatibilityForm.cs
    ├── SearchByVehicleForm.cs
    ├── OrderManagementForm.cs
    ├── CustomerManagementForm.cs
    ├── CustomerDashboardForm.cs
    ├── BrowsePartsForm.cs
    ├── CartForm.cs
    └── OrderHistoryForm.cs
```

---

## Database Schema

```sql
Users            (Id, Email UNIQUE, PasswordHash, Role)
SpareParts       (Id, Sku UNIQUE, Name, Category, Price, StockQuantity)
VehicleCompatibility (Id, SparePartId FK, Make, Model, YearStart, YearEnd)
Orders           (Id, CustomerId FK, OrderDate, Status, TotalAmount)
OrderItems       (Id, OrderId FK, SparePartId FK, Quantity, UnitPrice)
```

Passwords are stored as BCrypt hashes. Unit prices are saved at the time of order and never change if the catalogue price is updated later.

---

## NuGet Packages

| Package | Version | Purpose |
|---|---|---|
| MySqlConnector | 3.0.0 | MySQL ADO.NET driver |
| BCrypt.Net-Next | 4.2.0 | Password hashing |

---

## Configuration

The database connection string is in `App.config`:

```xml
<connectionStrings>
  <add name="CarSparePartsDB"
       connectionString="Server=localhost;Database=CarSparePartsDB;User ID=root;Password=;" />
</connectionStrings>
```

Change the values there if your MySQL runs on a different host, port, or uses a password. No code changes are needed.

---

## Architecture Notes

- All UI is built in code — no `.Designer.cs` files, no Visual Studio designer.
- Raw ADO.NET SQL throughout — no Entity Framework.
- All stock-modifying operations (place order, cancel order, checkout) run inside MySQL transactions with `FOR UPDATE` row locks.
- The connection string is read from a single place (`Data/AppConfig.cs`) across all controllers and forms.
