# StealAllTheCats

StealAllTheCats is an ASP.NET Core 8.0 Web API project for managing data via TheCatAPI (https://thecatapi.com/), featuring background job processing with Hangfire and SQL Server integration.

## Features

- RESTful API for data management
- Background jobs using Hangfire (e.g., fetching cats)
- Entity Framework Core for data access
- Swagger/OpenAPI documentation
- HTTP client integration
- Automated unit tests for controllers and background jobs
- Secure API key management using ASP.NET Core User Secrets

## Technologies

- .NET 8.0 (C# 12)
- ASP.NET Core Web API
- Entity Framework Core
- Hangfire
- SQL Server / SQL Express
- Serilog (logging)
- xUnit, NSubstitute, MockHttp (for testing)
- Docker 

### Image Storage

The `Image` property in `CatEntity` stores the URL of the cat image returned from TheCatAPI.  
Instead of storing the binary image data, we keep the URL as a reference to the external image resource.  
This simplifies storage and retrieval, while ensuring lightweight database records.

## Getting Started

### Prerequisites

- .NET 8 SDK
- SQL Server Express (local) or Docker

### Database Setup

#### Option 1: SQL Express (Default)
By default, the application connects to a local SQL Express instance.  
Update the connection string in `appsettings.json` if needed.

#### Option 2: Docker
A `docker-compose.yml` file is provided that sets up the entire environment:
- **SQL Server database** 
- **Web API project**
- **Test project**

To build and run the full stack with Docker:
```bash
docker compose up --build
```
This command will start the SQL Server database and the web API project in separate containers.

When running via Docker Compose, the Web API connects to the database using:

```json
"ConnectionStrings": {
  "CatDb": "Server=sqlserver,1433;Database=CatDb;User=sa;Password=Dev@1234;TrustServerCertificate=True;"
}
```

### Setup

1. Clone the repository.
2. Update the connection string in `appsettings.json` if needed.
3. **Set TheCatAPI key using environment variables**:

	Create a `.env` file in the **root folder of the project** (where the `.csproj` file is located) containing:
	```ini
	CatApi__ApiKey=your_api_key_here
	```
	Replace `your_api_key_here` with your actual API key for TheCatAPI.

	Alternatively, you can set the API key in the `appsettings.json` file under the `CatApi` section
   ```json
   "ApiKey": "your_api_key_here"
   ```
   Optional: If you prefer using User Secrets, run:
	```bash
	dotnet user-secrets init
	dotnet user-secrets set "CatApi:ApiKey" "your_api_key_here"  
	```
	For **Docker**, create a separate `.env` file in the **root folder of the solution** (where `docker-compose.yml` and `.sln` files are located) containing:
   ```ini
	CatApi__ApiKey=your_api_key_here
	```

4. No manual migration step is required - the app will ensure the schema on startup.
5. Run the application.
6. Access Swagger UI for API documentation.
	http://localhost:PORT/swagger
7. Access Hangfire Dashboard.
	http://localhost:PORT/hangfire


## Project Structure

- `Controllers/` - API endpoints
- `Entities/` - Data models
- `DTOs/` - Data transfer objects
- `Services/` - Background jobs and business logic
- `Data/` - Entity Framework DbContext
- `StealAllTheCats.Tests/` - Unit tests for controllers and services

## API Documentation

- Swagger is enabled and available at the `/swagger` endpoint for interactive API exploration and testing.

### API Endpoints
- `POST /api/cats/fetch`  
  Starts a background job to fetch and store 25 cat images asynchronously.

- `GET /api/cats/{id}`  
  Retrieves a cat by database ID.

- `GET /api/cats`  
  Retrieves paginated list of cats, supports query params:
  - `page` (default 1)
  - `pageSize` (default 10)
  - `tag` (optional) to filter cats by temperament tag.

- `GET /api/jobs/{id}`  
  Gets the status of a background fetch job.

## Validation

- Model validation is performed for each `CatEntity` during background job processing using data annotation attributes:
  - `CatId` is required.
  - `Width` and `Height` must be positive integers.
  - `ImagePath` must be a valid URL and is required.
- Duplicate cats are avoided based on `CatId`.
- Paging parameters are validated to be positive integers.

## Background Jobs

- Hangfire is used for scheduling and running background jobs, such as fetching data from TheCatAPI.
- The Hangfire dashboard is available at `/hangfire` for monitoring and managing jobs.

## Usage

- Use the Swagger UI to explore and test API endpoints.
- Use the Hangfire Dashboard to monitor and control background jobs.

## Testing

Unit tests are provided to ensure reliability and correctness of the main features:

- **Controllers:**  
Tests cover filtering, paging, and data retrieval logic in `CatsController`.
- **Services:**  
Tests for `CatFetcherJob` cover scenarios such as adding new data, handling invalid data, ignoring duplicates, and error handling.

### Running Tests

To run all unit tests:

Tests are located in the `StealAllTheCats.Tests` project and use xUnit, NSubstitute, and MockHttp for mocking dependencies.