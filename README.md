# CaseGuard Backend Assignment

## Overview

This project implements a backend API for an **Organization and License Management System** for a multi-tenant SaaS platform.

Organizations can manage members and invitations, while administrators manage licenses. Licenses support expiration and automatic renewal through a background job.

---

## Tech Stack

* **Language:** C#
* **Framework:** ASP.NET Core Web API
* **Database:** PostgreSQL
* **ORM:** Entity Framework Core
* **Authentication:** JWT
* **API Docs:** OpenAPI + Scalar

---

## Features

* JWT authentication
* Organization management
* Member management
* Invitation system
* License creation and management
* License assignment to members
* Automatic license renewal job
* Global exception handling

---

## Project Structure

```
CaseGuard.Backend.Assignment
 ├── Controllers
 ├── Services
 ├── Models
 ├── Data
 ├── Jobs
 ├── Middleware
 ├── Exceptions
 └── Migrations

CaseGuard.Backend.Assignment.Contracts
  API request/response models

CaseGuard.Backend.Assignment.Tests
  Unit tests

APICollection
  Example API request collections
```

---

## Setup

### Clone repository

```
git clone <repository-url>
cd Caseguard-Backend-Assignment
```

### Create database

Create a PostgreSQL database named:

```
caseguard
```

### Run the application

```
dotnet restore
dotnet ef database update --project CaseGuard.Backend.Assignment
dotnet run --project CaseGuard.Backend.Assignment
```

API runs at:

```
http://localhost:5100
```

---

## API Documentation

Interactive documentation:

```
http://localhost:5100/scalar
```

---

## Authentication Example

```
POST /api/auth/login
```

```json
{
  "userId": "admin1",
  "email": "admin@example.com",
  "role": "Admin"
}
```

Use the returned JWT token for protected endpoints.

---

## License Behavior

* License expiration: **10 minutes (testing)**
* Expired licenses become invalid
* Auto-renew licenses are renewed automatically by a background job

---


