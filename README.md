# Querier API

This repository contains the API part of the Querier project, a dynamic dashboard builder and database management system. The frontend part is developed separately using Angular and Flutter.

## Features

- **Database Management**
  - Multiple database type support (MySQL, PostgreSQL, SQL Server)
  - Connection string management
  - Automatic API generation for stored procedures
  - Dynamic query execution

- **API Features**
  - RESTful endpoints for dashboard management
  - Database connection handling
  - JSON Schema generation for dynamic forms
  - Secure authentication and authorization

## Tech Stack

### Backend
- .NET Core 8.0
- Entity Framework Core
- JWT Authentication
- Swagger/OpenAPI
- SQLite (default database)

## Getting Started

### Prerequisites
- .NET Core SDK 8.0
- IDE (Visual Studio, VS Code)
- SQL Server/MySQL/PostgreSQL (for connecting to external databases)

### Installation

1. Clone the repository
```bash
git clone https://github.com/yourusername/querier.git
```

2. Backend setup
```bash
cd Querier.Api
dotnet restore
dotnet run
```

## Project Structure

```
Querier.Api/
├── Controllers/     # API endpoints
├── Domain/         # Domain models and interfaces
│   ├── Models/     # Business entities
│   └── Services/   # Service interfaces
├── Infrastructure/ # Implementation of domain interfaces
│   ├── Data/      # Repositories and database context
│   └── Services/  # Service implementations
└── Application/   # Application services and DTOs
```

## Architecture

The .NET Core application follows Clean Architecture principles:
- **Domain Layer**: Contains business entities and interfaces
- **Application Layer**: Contains business logic and service interfaces
- **Infrastructure Layer**: Implements interfaces defined in the domain layer
- **API Layer**: Handles HTTP requests and responses

## Development

### Adding New Features
1. Define the domain models and interfaces in the Domain layer
2. Implement the business logic in the Application layer
3. Add infrastructure implementations as needed
4. Create API endpoints in the Controllers

## Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Related Projects

- [Querier Angular Frontend](link-to-frontend-repo) - The Angular frontend application for Querier