# MatchBy

> A platform that connects people who want to play sports, making it easy to create, find, and join games nearby.

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17.2-336791?logo=postgresql)](https://www.postgresql.org/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## 🎯 Overview

MatchBy centralizes everything (players, time, and location) to remove coordination hassle. The platform enables active individuals to discover nearby matches, create games, and connect with other players in their community.

### Key Features

- **Smart Match Discovery** - Location-based games and player search
- **Easy Game Creation** - Set up matches in seconds
- **Integrated Chat & Notifications** - Seamless coordination via SignalR
- **Reputation & Fair Play System** - Ratings and feedback for trust
- **Team Management** - Organize teams and manage invitations
- **Real-time Updates** - Live notifications and match status updates

## 🛠️ Technology Stack

| Layer | Technology |
|-------|-----------|
| **Frontend & Backend** | Blazor Server (.NET 9) |
| **UI Framework** | Blazorise with Tailwind CSS |
| **Database** | PostgreSQL 17.2 (via Npgsql) |
| **ORM** | Entity Framework Core 9 |
| **Real-time Communication** | SignalR |
| **Background Jobs** | Hangfire |
| **File Storage** | AWS S3 (via Supabase Storage) |
| **Email Service** | Resend |
| **Validation** | FluentValidation |
| **Authentication** | ASP.NET Core Identity |
| **Hosting** | Azure App Service |
| **CI/CD** | GitHub Actions |

## 📋 Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL 17.2](https://www.postgresql.org/download/) or Docker
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [Rider](https://www.jetbrains.com/rider/) (recommended)
- AWS S3 credentials (or Supabase Storage)
- Resend API key (for email functionality)

## 🚀 Getting Started

### 1. Clone the Repository

```bash
git clone <repository-url>
cd LAB
```

### 2. Database Setup

Using Docker Compose (recommended):

```bash
docker-compose up -d matchby.postgres
```

This will start a PostgreSQL container on port `5432` with:
- Database: `database`
- User: `postgres`
- Password: `postgres`

Alternatively, you can use pgAdmin (included in compose.yaml) at `http://localhost:8080`.

### 3. Configuration

1. Copy `appsettings.Development.json` and configure your connection strings and API keys:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=database;Username=postgres;Password=postgres"
  },
  "Resend": {
    "ApiKey": "your-resend-api-key"
  },
  "S3Settings": {
    "AccessKey": "your-access-key",
    "SecretKey": "your-secret-key",
    "BucketName": "your-bucket-name",
    "Region": "your-region"
  },
  "Blazorise": {
    "ProductToken": "your-blazorise-token"
  }
}
```

2. For sensitive configuration, use [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets):

```bash
dotnet user-secrets set "Resend:ApiKey" "your-api-key"
dotnet user-secrets set "S3Settings:AccessKey" "your-access-key"
# ... etc
```

### 4. Database Migrations

The application will automatically apply migrations and seed data in development mode. For manual migration:

```bash
cd MatchBy/MatchBy
dotnet ef database update
```

### 5. Run the Application

**Using Visual Studio:**
1. Open `MatchBy.sln` in Visual Studio
2. Set `MatchBy` as the startup project
3. Press `F5` to run

**Using .NET CLI:**
```bash
cd MatchBy/MatchBy
dotnet run
```

The application will be available at `https://localhost:5001` (or the port specified in `launchSettings.json`).

### 6. Access Hangfire Dashboard

The Hangfire dashboard is available at `/hangfire` for monitoring background jobs.

## 📁 Project Structure

```
MatchBy/
├── MatchBy/                    # Main Blazor Server application
│   ├── Components/            # Razor components (Pages, Layout, Account)
│   ├── Controllers/           # API controllers
│   ├── Data/                  # Data access layer (DbContext, Migrations, Seeders)
│   ├── DTOs/                  # Data Transfer Objects
│   ├── Enums/                 # Enumerations
│   ├── Extensions/            # Extension methods
│   ├── Hubs/                  # SignalR hubs (ChatHub)
│   ├── Models/                # Domain models
│   ├── Services/              # Business logic services
│   │   ├── BackgroundJobs/   # Hangfire job services
│   │   ├── ChatMessages/     # Chat message services
│   │   ├── Conversations/    # Conversation services
│   │   ├── Email/            # Email services
│   │   ├── FileValidator/    # File validation
│   │   ├── Matches/          # Match services
│   │   ├── S3/               # S3 storage services
│   │   └── Users/            # User services
│   ├── Settings/              # Configuration classes
│   └── Validators/            # FluentValidation validators
├── MatchBy.Client/            # Blazor WebAssembly client (optional)
├── MatchBy.UnitTests/         # Unit tests
└── MatchBy.wiki/              # Project documentation
```

## 🏗️ Architecture

MatchBy follows a **layered architecture** pattern:

1. **Presentation Layer** - Blazor Server components and pages
2. **Business Logic Layer** - Service classes handling business rules
3. **Data Access Layer** - Entity Framework Core with PostgreSQL
4. **Domain Layer** - Domain models and entities

For detailed architecture documentation, see [Architecture.md](MatchBy.wiki/Architecture.md).

## 🧪 Testing

Run unit tests using:

```bash
dotnet test
```

Or use Visual Studio's Test Explorer (Ctrl+R, A).

## 🚢 Deployment

### Environments

- **Staging**: [Staging URL](https://matchby-d4eweaajaffthmh6.italynorth-01.azurewebsites.net) (deployed from `main` branch)
- **Production**: [matchby.me](https://matchby.me/) (deployed from `production` branch with version tags)

### CI/CD

- **Staging**: Automatic deployment on push to `main` branch
- **Production**: Deployment triggered by version tags (e.g., `v1.2.0`) on `production` branch

For more details, see [Deployment.md](MatchBy.wiki/Deployment.md).

## 📚 Documentation

Comprehensive documentation is available in the [MatchBy.wiki](MatchBy.wiki/) directory:

- [Home](MatchBy.wiki/Home.md) - Main documentation index
- [Product Vision](MatchBy.wiki/Product-Vision.md) - Vision, target group, and business goals
- [User Stories](MatchBy.wiki/User-Stories.md) - Complete list of user stories
- [Architecture](MatchBy.wiki/Architecture.md) - System architecture and design patterns
- [Database Schema](MatchBy.wiki/Database-Schema.md) - Database structure and relationships
- [Deployment](MatchBy.wiki/Deployment.md) - CI/CD and deployment strategy
- [UI Mockups](MatchBy.wiki/UI‐Mockups.md) - User interface designs
- [Iteration 1](MatchBy.wiki/Iteration-1.md) - Sprint reviews and deliverables

## 🔧 Development Guidelines

### Code Style

- Follow C# coding conventions and .NET best practices
- Use PascalCase for public members, camelCase for private fields
- Prefix interfaces with `I` (e.g., `IUserService`)
- Enable nullable reference types
- Treat warnings as errors (configured in `Directory.Build.props`)

### Best Practices

- **Async/Await**: All I/O operations should be asynchronous
- **Dependency Injection**: Use constructor injection for all dependencies
- **Validation**: Use FluentValidation for server-side validation
- **Error Handling**: Implement proper error handling and logging
- **Security**: Use HTTPS, implement proper CORS policies, and follow OWASP guidelines

## 🤝 Contributing

1. Create a feature branch from `main`
2. Make your changes
3. Ensure all tests pass
4. Submit a pull request

## 📝 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 👥 Team

Developed as part of a Master's degree program.

---

**Made with ❤️ using Blazor and .NET**
