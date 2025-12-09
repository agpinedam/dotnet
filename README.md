# BattleShip Game

## Authors

* **Fadia ALLANI**
  Email: [fadia.allani@etu.mines-ales.fr](mailto:fadia.allani@etu.mines-ales.fr)
* **Angie Giceth PINEDA MENDOZA**
  Email: [angie-giceth.pineda-mendoza@etu.mines-ales.fr](mailto:angie-giceth.pineda-mendoza@etu.mines-ales.fr)


## Project Overview

This project is a full Battleship game implementation built with **ASP.NET Core**, **Blazor WebAssembly**, and **gRPC**, supporting both **single-player (AI)** and **real-time multiplayer** modes.
It is structured into three independent but interconnected projects:

* **BattleShip.API** – ASP.NET Core Web API (Backend)
* **BattleShip.App** – Blazor WebAssembly Client (Frontend)
* **BattleShip.Models** – Shared Models Library

## Technical Architecture

### Communication Protocols

The system supports **two communication protocols**:

#### **REST (HTTP/JSON)**

* Used for standard CRUD and game commands.
* Compatible with any HTTP client.

#### **gRPC / gRPC-Web**

* Provides high-performance, strongly typed communication.
* Enables the Blazor WebAssembly client to use gRPC directly from the browser.
* Protocol Buffers (`.proto`) ensure compact and fast message exchange.

### Real-Time Multiplayer (SignalR)

Multiplayer mode uses **SignalR** to provide:

* Persistent bi-directional communication between players.
* Instant synchronization of game state.
* Real-time move broadcasting via the `GameHub`.
* Graceful handling of player disconnects and session cleanup.

### Validation (FluentValidation)

All inputs and game actions are validated server-side to guarantee integrity:

* No overlapping ships.
* Valid coordinates.
* Only legal moves are accepted.
* Rules enforced uniformly for both AI and human players.


## AI Difficulty Levels

The AI is built using the **Strategy Pattern**, making each behavior encapsulated and interchangeable.

### **1. Easy – Random**

* Targets any valid cell at random.
* Does not use memory or patterns. Ideal for beginners.

### **2. Medium – Probability Heatmap**

* Calculates a heatmap of likely ship positions.
* Switches to a *hunt mode* after a successful hit.
* More efficient at narrowing down ship locations.

### **3. Hard – Heatmap + Parity Optimization**

* Uses heatmap logic plus parity-based search (checkerboard).
* Cuts search space almost in half.
* Deduces ship orientation after multiple successful hits.
* Plays close to optimal strategy.


## How to Build and Run the Project

### 1. Build the Entire Solution

```bash
dotnet build
```

### 2. Run the Backend (API)

```bash
dotnet run --project BattleShip.API
```

The API will run at:

* [https://localhost:7258](https://localhost:7258)
* [http://localhost:5200](http://localhost:5200)

### 3. Run the Frontend (Blazor App)

```bash
dotnet run --project BattleShip.App
```

The game UI will be available at:

* [https://localhost:7228](https://localhost:7228)
* [http://localhost:5173](http://localhost:5173)


## Game Modes & Core Logic

### **1. Single Player (vs AI)**

* The AI implementation is modular thanks to the Strategy Pattern.
* The `AiService` selects the appropriate difficulty dynamically.
* All gameplay logic (board state, hit detection) is executed on the **server**.
* Prevents cheating and ensures consistency.

### **2. Multiplayer (Player vs Player)**

* Real-time communication powered by SignalR.
* Low latency move broadcasting using WebSockets (with fallbacks).
* The `GameHub` manages:

  * Matchmaking
  * Player connections
  * Game state synchronization
  * Disconnection events

### **3. Hybrid Communication Layer (REST + gRPC)**

The project demonstrates a modern dual-protocol approach:

* **REST**: For classic API consumption.
* **gRPC**: For high-speed operations and structured contracts.

The frontend integrates both seamlessly using an abstraction layer so the UI remains protocol-agnostic.

### **4. Responsive UI (Blazor WebAssembly)**

* Built entirely with reusable components (e.g. `GridBoard`).
* Uses modern CSS Grid and Flexbox layouts.
* Automatically adapts to different screen sizes.
* Clear split between UI state and server-validated game logic.

