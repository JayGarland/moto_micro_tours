# Moto Micro Tours 🏍️

A full-stack web application for managing motorcycle tours, bookings, and communications between riders (motards) and tour organizers.

## 📋 Table of Contents

- [About](#about)
- [Features](#features)
- [Tech Stack](#tech-stack)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Configuration](#configuration)
- [Running the Application](#running-the-application)
- [Project Structure](#project-structure)
- [API Documentation](#api-documentation)
- [Contributing](#contributing)

## 🚀 About

Moto Micro Tours is a platform that connects motorcycle enthusiasts with tour organizers. It facilitates tour bookings, real-time chat communications, payment processing, and review systems.

## ✨ Features

- **User Authentication** - Secure registration and login for riders and tour organizers
- **Tour Management** - Create, browse, and manage motorcycle tours
- **Booking System** - Complete booking workflow with approval/cancellation
- **Real-time Chat** - Pre-booking and post-booking conversation system
- **Payment Processing** - Integrated payment handling with refund support
- **Review System** - Rate and review completed tours
- **Authorization** - Role-based access control for different user types

## 🛠️ Tech Stack

### Backend
- **.NET 8.0** - Web API framework
- **Entity Framework Core 8.0** - ORM
- **MySQL** - Database (via Pomelo.EntityFrameworkCore.MySql)
- **ASP.NET Core Identity** - Authentication and authorization
- **Swagger/OpenAPI** - API documentation

### Frontend
- **Angular 20.3** - Frontend framework
- **Angular Material** - UI component library
- **TypeScript** - Type-safe JavaScript
- **RxJS** - Reactive programming

## 📦 Prerequisites

Before you begin, ensure you have the following installed:

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/) (v18 or higher)
- [npm](https://www.npmjs.com/) (comes with Node.js)
- [MySQL Server](https://dev.mysql.com/downloads/mysql/) (v8.0 or higher)
- [Angular CLI](https://angular.io/cli) (optional, but recommended)

### Verify Installation

```powershell
# Check .NET version
dotnet --version

# Check Node.js version
node --version

# Check npm version
npm --version

# Check MySQL
mysql --version
```

## 🔧 Installation

### 1. Clone the Repository

```powershell
git clone https://github.com/JayGarland/moto_micro_tours.git
cd moto_micro_tours
```

### 2. Setup MySQL Database

Start your MySQL server

### 3. Backend Setup

```powershell
# Navigate to backend project
cd moto_micro_tours

# Restore NuGet packages
dotnet restore

# Restore local .NET tools (dotnet-ef)
dotnet tool restore

# Apply database migrations
dotnet ef database update
```

### 4. Frontend Setup

```powershell
# Navigate to frontend project
cd ..\moto-micro-tours-frontend

# Install dependencies
npm install
```

## ⚙️ Configuration

### Backend Configuration

Edit `moto_micro_tours/appsettings.json` and update the connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;database=moto_micro_tours;user=your_mysql_user;password=your_mysql_password;"
  }
}
```

For development, you can also modify `appsettings.Development.json`.

### Local Tooling (dotnet-ef)

This repo uses a local tool manifest so new machines can restore the EF CLI with a pinned version:

```powershell
dotnet tool restore
```

### Frontend Configuration

The frontend is configured to use a proxy for API calls. Check `moto-micro-tours-frontend/proxy.conf.json`:

```json
{
  "/api": {
    "target": "http://localhost:5000",
    "secure": false
  }
}
```

Update the target URL if your backend runs on a different port.

## 🚀 Running the Application

### Option 1: Run Both (Recommended)

Open two terminal windows:

**Terminal 1 - Backend:**
```powershell
cd moto_micro_tours
dotnet run
```

**Terminal 2 - Frontend:**
```powershell
cd moto-micro-tours-frontend
npm run start:proxy
```

### Option 2: Using Visual Studio

1. Open `moto_micro_tours.sln` in Visual Studio
2. Set `moto_micro_tours` as the startup project
3. Press F5 to run the backend
4. In a separate terminal, navigate to `moto-micro-tours-frontend` and run `npm run start:proxy`

### Access the Application

- **Backend API:** http://localhost:5000 (or check your console output)
- **Frontend:** http://localhost:4200
- **Swagger UI:** http://localhost:5000/swagger

### **Critical**

- Because now we are in development mode with Angular proxy setup so:
- **You need to configure the environment API URL:** in the path [environment.ts](./moto-micro-tours-frontend/src/environments/environment.ts) for the frontend to connect to the backend properly.
- **You need to configure the URL for the CORS for Angular frontend:** in the path [Program.cs](./moto_micro_tours/Program.cs) for the backend to accept requests from the frontend.


## 📁 Project Structure

```
moto_micro_tours/
├── moto_micro_tours/           # Backend (.NET Web API)
│   ├── Controllers/            # API endpoints
│   ├── Domain/                 # Domain models and entities
│   │   ├── Entities/          # Database entities
│   │   └── Enums/             # Enumerations
│   ├── DTOs/                   # Data Transfer Objects
│   ├── Services/               # Business logic services
│   ├── Authorization/          # Custom authorization handlers
│   ├── Middleware/             # Custom middleware
│   ├── Migrations/             # EF Core migrations
│   ├── Tests/                  # Unit and integration tests
│   └── Program.cs              # Application entry point
│
├── moto-micro-tours-frontend/  # Frontend (Angular)
│   ├── src/
│   │   ├── app/               # Angular components and services
│   │   └── styles.scss        # Global styles
│   ├── angular.json           # Angular configuration
│   └── proxy.conf.json        # Development proxy config
│
└── docs/                       # Documentation
    ├── backend/               # Backend documentation
    └── frontend/              # Frontend documentation
```

## 📚 API Documentation

Once the backend is running, visit the Swagger UI at `http://localhost:5000/swagger` to explore and test the API endpoints.

### Main API Endpoints

- **Auth:** `/api/auth` - Authentication (login, register)
- **Tours:** `/api/tours` - Tour management
- **Bookings:** `/api/bookings` - Booking operations
- **Chats:** `/api/chats` - Messaging system
- **Payments:** `/api/payments` - Payment processing
- **Motards:** `/api/motards` - Rider profiles

### Testing Endpoints

The project includes HTTP files for testing:
- `test_auth.http`
- `test_tour_creation.http`
- `test_booking_approval.http`
- `test_chat_authorization.http`
- And more...

Use these with VS Code's REST Client extension or similar tools.

## 🧪 Running Tests

### Backend Tests

```powershell
cd moto_micro_tours
dotnet test
```

### Frontend Tests

```powershell
cd moto-micro-tours-frontend
npm test
```

## 🔨 Building for Production

### Backend

```powershell
cd moto_micro_tours
dotnet publish -c Release -o ./publish
```

### Frontend

```powershell
cd moto-micro-tours-frontend
npm run build
```

The production build will be in the `dist/` folder.

## 🐛 Troubleshooting

### Common Issues

**Database Connection Issues:**
- Verify MySQL server is running
- Check connection string credentials
- Ensure the database exists

**Port Already in Use:**
- Backend: Change port in `Properties/launchSettings.json`
- Frontend: Use `ng serve --port 4201`
- If you use Uwamp/Apache and port 80 is taken (IIS), change Apache to 8080 in `httpd.conf` and use `http://localhost:8080`

**Migration Issues:**
```powershell
# Reset database
dotnet ef database drop
dotnet ef database update
```

**Frontend Dependencies:**
```powershell
# Clear cache and reinstall
rm -rf node_modules package-lock.json
npm install
```

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 👥 Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📧 Contact

For questions or support, please open an issue in the GitHub repository.

---

**Happy Riding! 🏍️💨**
