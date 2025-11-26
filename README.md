# BattleShip Game

## Project Structure

- **BattleShip.API**: ASP.NET Core Web API (Backend).
- **BattleShip.App**: Blazor WebAssembly (Frontend).
- **BattleShip.Models**: Shared Class Library (Models).

## How to Run

You need to run both the API and the App projects.

### 1. Run the API (Backend)

Open a terminal and run:

```bash
dotnet run --project BattleShip.API
```

The API will be available at:
- https://localhost:7258
- http://localhost:5200

### 2. Run the App (Frontend)

Open a **new** terminal and run:

```bash
dotnet run --project BattleShip.App
```

The Application will be available at:
- https://localhost:7228
- http://localhost:5173
