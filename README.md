# 📦 Inventory Dashboard

A **full-featured inventory management system** built with ASP.NET Core MVC, Entity Framework Core, and **PostgreSQL on Neon**. This dashboard provides real-time stock tracking, order management, audit logging, and CSV exports – all with a beautiful dark/light theme.

**Live Demo**: [Deployed on Render](https://your-app-url.onrender.com)

---

## 🚀 Features

### 📊 Dashboard
- Live statistics: Total Products, Low Stock, Pending Orders, Stock Value, Order Revenue
- Interactive bar chart showing weekly order trends
- Live clock display with user greeting
- Quick access to all major sections

### 📦 Product Management
- **CRUD Operations**: Create, Read, Update, Delete products
- **Search & Filter**: Filter by name, SKU, category, and stock range
- **Low Stock View**: Automatically highlights products with ≤10 units
- **Restock Functionality**: Update stock quantity and price from the Low Stock page
- **10‑Minute Edit Lock**: Products cannot be edited after 10 minutes of creation (prevents accidental changes)
- **Delete Protection**: Products linked to existing orders cannot be deleted

### 📋 Order Management
- **Full Order Lifecycle**: Create, view, approve, reject, and delete orders
- **Order Details**: View all items, status, customer info, and timestamps
- **Approval with Stock Deduction**: Approving an order automatically deducts stock and logs the change
- **Confetti Animation**: Celebrate approved orders with a confetti burst
- **Invoice Download**: Generate plain-text invoices for approved orders
- **Delete Restriction**: Only pending orders can be deleted

### 📓 Notes Journal
- **Categorized Notes**: Log expiries, quality issues, restock reminders, supplier notes, and general memos
- **Product Linking**: Attach notes to specific products
- **Status Tracking**: Mark notes as completed/resolved
- **Filtering**: Filter by category and status

### 📜 Audit Log
- **Complete Activity Trail**: Every action is logged – product creation, updates, deletion, restocks, order creation, approval, rejection, deletion
- **Who Did What**: Records the username and timestamp for each action
- **CSV Export**: Export the entire audit log as a CSV file for reporting
- **Product History**: View stock‑change history for any product (previous stock, new stock, reason, who changed it)

### 📤 CSV Export
- **Export Products**: Download all products as a CSV file
- **Export Orders**: Download all orders as a CSV file
- **Export Audit Log**: Download the full audit trail as CSV

### 🌓 Dark/Light Theme
- **Toggle Mode**: Switch between dark and light themes with one click
- **Persistent Preference**: Theme preference is saved in local storage
- **Figma-Accurate Design**: Navy/gold palette with Instrument Serif and Inter fonts

### ⚡ Real-Time Alerts (SignalR)
- **Low Stock Alerts**: Toast notification when a product drops to ≤10 units
- **Out of Stock Alerts**: Urgent modal popup when a product hits 0 units
- **Instant Updates**: No page refresh needed – live notifications

### 🔐 Authentication
- **Simple Login System**: Session-based authentication
- **Default Credentials**: username: `admin`, password: `Admin123!`
- **Protected Routes**: All pages except Login require authentication

---

## 🛠️ Tech Stack

### Backend
- **Framework**: ASP.NET Core MVC (.NET 9.0)
- **Database**: PostgreSQL (via Neon)
- **ORM**: Entity Framework Core
- **Real-Time**: SignalR for live stock alerts
- **Authentication**: Session-based (custom)

### Frontend
- **CSS Framework**: Tailwind CSS (utility classes)
- **Custom Theme**: Navy/Gold palette with dark/light mode
- **Fonts**: Instrument Serif (headings) + Inter (UI)
- **Charts**: Chart.js for the dashboard bar chart
- **Animations**: canvas-confetti for order approvals

### Cloud Services
- **Hosting**: Render (Web Service)
- **Database**: Neon PostgreSQL
- **CI/CD**: Automatic deployments from GitHub

---

## 📁 Project Structure

```
csharp-inventory-dashboard/
├── Controllers/
│   ├── HomeController.cs          # Dashboard
│   ├── ProductController.cs       # Product CRUD + Restock + Export
│   ├── OrderController.cs         # Order CRUD + Approve/Reject
│   ├── NotesController.cs         # Notes Journal
│   ├── AuditLogController.cs      # Audit Log + CSV Export
│   └── LoginController.cs         # Authentication
├── Models/
│   ├── Product.cs                 # Product entity
│   ├── Order.cs                   # Order entity
│   ├── OrderItem.cs               # Order Item entity
│   ├── Note.cs                    # Note entity
│   ├── AuditLog.cs                # Audit Log entity
│   ├── StockHistory.cs            # Stock Change History entity
│   ├── Customer.cs                # Customer entity
│   └── DashboardData.cs           # Dashboard view model
├── Repositories/
│   ├── IProductRepository.cs      # Product repository interface
│   ├── ProductRepository.cs       # Product repository (with audit logging)
│   ├── IOrderRepository.cs        # Order repository interface
│   ├── OrderRepository.cs         # Order repository
│   ├── INoteRepository.cs         # Note repository interface
│   └── NoteRepository.cs          # Note repository
├── Services/
│   ├── IAuditService.cs           # Audit service interface
│   ├── AuditService.cs            # Audit service (logs actions)
│   ├── DashboardService.cs        # Dashboard data service
│   └── PdfService.cs              # PDF generation service
├── Hubs/
│   └── StockHub.cs                # SignalR hub for stock alerts
├── Data/
│   └── AppDbContext.cs            # EF Core database context
├── Views/
│   ├── Shared/
│   │   └── _Layout.cshtml         # Main layout (navbar, theme toggle)
│   ├── Home/
│   │   └── Index.cshtml           # Dashboard with stats and chart
│   ├── Product/
│   │   ├── Index.cshtml           # Product grid with filters
│   │   ├── Create.cshtml          # Create product form
│   │   ├── Edit.cshtml            # Edit product form (10-min lock)
│   │   ├── Delete.cshtml          # Delete confirmation
│   │   ├── Details.cshtml         # Product details + History button
│   │   ├── History.cshtml         # Stock change history
│   │   ├── LowStock.cshtml        # Products with ≤10 units
│   │   └── Restock.cshtml         # Restock form
│   ├── Order/
│   │   ├── Index.cshtml           # Orders table with status badges
│   │   ├── Create.cshtml          # Create order with dynamic rows
│   │   ├── Details.cshtml         # Order details + Approve/Reject
│   │   ├── Edit.cshtml            # Edit order (status/notes)
│   │   └── Delete.cshtml          # Delete confirmation (pending only)
│   ├── Notes/
│   │   ├── Index.cshtml           # Notes grid with filters
│   │   ├── Create.cshtml          # Create note form
│   │   ├── Edit.cshtml            # Edit note form
│   │   └── Delete.cshtml          # Delete confirmation
│   ├── AuditLog/
│   │   └── Index.cshtml           # Full audit trail with CSV export
│   └── Login/
│       └── Index.cshtml           # Split-screen login page
├── wwwroot/
│   ├── css/
│   │   └── site.css               # Complete Figma-themed styles (dark/light)
├── Program.cs                     # Application entry point + service registration
├── appsettings.json               # Configuration (uses environment variables)
├── appsettings.Development.json   # Development configuration
├── appsettings.Production.json    # Production configuration
├── Dockerfile                     # Container configuration for Render
├── .dockerignore                  # Exclude files from Docker build
└── README.md                      # This file
```

---

## 🚀 Deployment Guide

### Prerequisites

1. **GitHub Account** – for repository hosting
2. **Render Account** – for hosting the web service
3. **Neon Account** – for PostgreSQL database
4. **Git** – for version control

---

### 📦 1. Set Up Neon PostgreSQL

1. **Create a Neon account** at [neon.tech](https://neon.tech)
2. **Create a new project**:
   - Click "New Project"
   - Name: `inventory-dashboard`
   - Region: Choose one close to Render (e.g., `us-east-1`)
   - PostgreSQL version: 15 or 16
3. **Get your connection string**:
   - Click "Connect" on your project
   - Copy the **connection string** (looks like `postgresql://user:password@host/database`)
   - Example: `postgresql://neondb_owner:abc123@ep-frosty-shadow.us-east-1.aws.neon.tech/inventory-db`

---

### 🔐 2. Set Up Render

1. **Create a Render account** at [render.com](https://render.com)
2. **Connect GitHub**: Go to Dashboard → "Connect GitHub" and grant access
3. **Create a new Web Service**:
   - Click "New +" → "Web Service"
   - Connect your GitHub repository
   - Configure:
     - **Name**: `inventory-dashboard`
     - **Environment**: `Docker`
     - **Branch**: `main`
     - **Build Command**: `dotnet publish -c Release -o out`
     - **Start Command**: `dotnet out/csharp-inventory-dashboard.dll`
4. **Add Environment Variables** (in Render dashboard):
   - `ASPNETCORE_ENVIRONMENT`: `Production`
   - `ConnectionStrings__DefaultConnection`: `your-neon-connection-string`
   - `SESSION_SECRET`: (generate a random string)
   - `PORT`: `5000` (Render sets this automatically)

---

### 🐳 3. Create `Dockerfile`

Create a `Dockerfile` in your project root:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .

# Expose port (Render sets PORT env variable)
EXPOSE 5000

# Run the app (Render passes PORT via environment)
ENTRYPOINT ["dotnet", "csharp-inventory-dashboard.dll"]
```

---

### 📄 4. Create `render.yaml` (Optional – Blueprint)

```yaml
services:
  - type: web
    name: inventory-dashboard
    runtime: docker
    repo: https://github.com/yourusername/csharp-inventory-dashboard
    plan: starter
    region: oregon
    dockerCommand: dotnet csharp-inventory-dashboard.dll
    envVars:
      - key: ASPNETCORE_ENVIRONMENT
        value: Production
      - key: ConnectionStrings__DefaultConnection
        sync: false  # You'll set this manually
      - key: SESSION_SECRET
        generateValue: true
      - key: PORT
        value: 5000
```

---

## 🗄️ Database Configuration

### Update `Program.cs` for PostgreSQL

Replace the SQLite connection with PostgreSQL:

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
```

### Create `appsettings.Production.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=your-neon-host;Database=your-db;Username=your-user;Password=your-password;SSL Mode=Require;Trust Server Certificate=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Add the Npgsql NuGet Package

```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

---

## 🔧 5. Environment Variables (Render)

| Variable | Description | Example |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Set to `Production` | `Production` |
| `ConnectionStrings__DefaultConnection` | Neon PostgreSQL connection string | `postgresql://user:pass@host/db` |
| `SESSION_SECRET` | Random string for session security | (auto-generated) |

---

## 🚀 6. Deploy to Render

### Automated Deployment (GitHub Push)

1. **Push your code to GitHub**:
   ```bash
   git add .
   git commit -m "Ready for production deployment"
   git push origin main
   ```

2. **Render automatically detects the push** and deploys the new version.

### Manual Deployment (if needed)

1. **Build the Docker image**:
   ```bash
   docker build -t inventory-dashboard .
   ```

2. **Tag and push to Render** (or let Render build it automatically).

---

## 🧪 7. Verify Deployment

1. **Visit your Render URL**:
   ```
   https://your-app-name.onrender.com
   ```

2. **Login** with:
   - Username: `admin`
   - Password: `Admin123!`

3. **Check the Dashboard**: Verify stats and chart display.

4. **Create a test product**: Ensure database connection works.

5. **Check Audit Log**: Create a product, then go to `/AuditLog` to verify logging works.

---

## 🔑 Login Credentials (Production)

| Role | Username | Password |
|------|----------|----------|
| Admin | `admin` | `Admin123!` |

**⚠️ IMPORTANT**: Change the default password immediately in production!

---

## 📊 Database Schema (Neon PostgreSQL)

The database schema is managed by Entity Framework Core's `EnsureCreated()`. Tables created:

| Table | Description |
|-------|-------------|
| `Products` | Inventory items with price, stock, category |
| `Orders` | Order headers with customer, status, total |
| `OrderItems` | Line items for each order |
| `Customers` | Customer information |
| `Notes` | Journal entries for expiries, quality, etc. |
| `AuditLogs` | Complete activity trail |
| `StockHistories` | Stock change records |

---

## 🔔 Real-Time Alerts (SignalR)

**⚠️ Important for Production**: SignalR with `HttpClient` may require **WebSocket support** on Render. Render supports WebSockets by default, but ensure:

1. Your app uses the `app.MapHub<StockHub>("/stockHub");` endpoint.
2. The client connects to `/stockHub` via the `/negotiate` endpoint.

No additional configuration is needed – Render handles WebSockets automatically.

---

## 📤 CSV Export

All CSV exports work in production:
- `/Product/ExportCsv`
- `/Order/ExportCsv`
- `/AuditLog/ExportCsv`

---

## 🛠️ Troubleshooting

### Database Connection Issues
1. Verify the Neon connection string is correct in Render environment variables.
2. Check if Neon allows connections from Render's IP range.
3. Ensure SSL is enabled (`SSL Mode=Require`).

### SignalR Not Working
1. Check Render logs for WebSocket errors.
2. Verify the `stockHub` endpoint is registered.

### 404 Not Found
1. Check the deployed URL path.
2. Verify the Docker build succeeded.

---

## 📦 Development vs Production

| Aspect | Development (Local) | Production (Render + Neon) |
|--------|---------------------|----------------------------|
| Database | SQLite (`inventory.db`) | PostgreSQL (Neon) |
| Connection String | `Data Source=inventory.db` | Environment Variable |
| Config | `appsettings.Development.json` | `appsettings.Production.json` |
| Port | `5000` | `PORT` (Render sets dynamically) |

---

## 🔐 Security Considerations

### For Production

1. **Change Default Password**: Update the login credentials.
2. **Environment Variables**: Never hardcode secrets.
3. **Neon SSL**: Always use SSL for production connections.
4. **Session Secret**: Use a strong, random secret for `SESSION_SECRET`.
5. **Render Env Vars**: Keep sensitive data in Render's environment variables (not in code).

### Recommended Changes

- Implement real user authentication with password hashing.
- Add rate limiting to prevent brute-force attacks.
- Use HTTPS (Render provides SSL by default).

---

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## 📝 License

This project is open source. Feel free to use, modify, and distribute.

---

## 🙏 Acknowledgments

- **Design Inspiration**: Figma‑designed inventory dashboard with navy/gold theme
- **Fonts**: Google Fonts (Instrument Serif + Inter)
- **Charting**: Chart.js
- **Animations**: canvas-confetti
- **Hosting**: Render + Neon

---

## 📧 Support

For issues, questions, or feature requests:
- Open a GitHub issue
- Contact the maintainer

---

**Happy inventory managing!** 📦🚀