# Orders API - Demo Project

> ASP.NET Core demo project demonstrating production-ready API patterns.

## 🎯 Project Goals

This project will cover:
- ✅ RESTful API design with proper HTTP semantics
- ✅ Authentication & Authorization (JWT, role-based)
- ✅ Transactional integrity & optimistic concurrency
- ✅ Validation & error handling (RFC 7807 Problem Details)
- ✅ Pagination, filtering, sorting
- ✅ Caching strategies (response caching, ETags)
- ✅ Rate limiting
- ✅ API versioning
- ✅ Health checks & observability
- ✅ Comprehensive testing (unit + integration)

---

## 🏗️ Architecture

```
OrdersApi/
├── src/
│   ├── OrdersApi.Api/           # Controllers/Endpoints, middleware
│   ├── OrdersApi.Domain/        # Entities, enums, domain logic
│   ├── OrdersApi.Infrastructure/# EF Context, migrations, repos
│   └── OrdersApi.Application/   # (Optional) Services/use cases
├── tests/
│   ├── OrdersApi.UnitTests/
│   └── OrdersApi.IntegrationTests/
└── OrdersApi.sln
```

**Tech Stack:**
- ASP.NET Core 8.0 Web API
- Entity Framework Core (SQLite)
- JWT Authentication
- xUnit + WebApplicationFactory
- Swagger/OpenAPI

---

## 🚀 Quick Start

### Prerequisites
- .NET 8 SDK
- (Optional) Docker for containerized run

### Setup

1. **Clone and restore:**
```bash
git clone https://github.com/Kate-Dvo/orders-api.git
cd orders-api
dotnet restore
```

2. **Run migrations (if not auto-applied):**
```bash
dotnet ef database update --project src/OrdersApi.Infrastructure
```

3. **Run the API:**
```bash
cd src/OrdersApi.Api
dotnet run
```

4. **Access Swagger:**
```
https://localhost:5000/swagger
```

5. **Health Checks:**
```bash
curl http://localhost:5000/health/live
curl http://localhost:5000/health/ready
```

---

## 🧪 Testing

### Run All Tests
```bash
dotnet test
```

### Run Unit Tests Only
```bash
dotnet test tests/OrdersApi.UnitTests
```

### Run Integration Tests Only
```bash
dotnet test tests/OrdersApi.IntegrationTests
```

### Test Coverage
_TODO: Add coverage report_

---

## 📊 Design Decisions & Trade-offs

### Why SQLite?
- ✅ Zero setup, file-based
- ✅ Perfect for local dev and demo
- ❌ Not suitable for production multi-user scenarios
- ❌ Limited concurrency features

### Why Optimistic Concurrency (RowVersion)?
- ✅ Prevents lost updates without locking
- ✅ Better for REST APIs (stateless)
- ❌ Requires clients to handle 412 responses

### API Versioning Strategy
- **Chosen:** URL segment (`/api/v1/`, `/api/v2/`)
- **Alternatives:** Header-based, query string
- **Rationale:** Most discoverable, easiest for testing

---

## 🚧 Known Limitations & Future Improvements

- [ ] No actual user authentication endpoint (tokens are pre-generated)
- [ ] Pagination doesn't support cursor-based (only offset)
- [ ] No HATEOAS links
- [ ] No soft deletes
- [ ] Limited audit logging
- [ ] No background job processing for async operations
- [ ] Rate limiting is IP-based (not user-based)

---

## 📝 License

This is a demo project for learning purposes. Use freely.

---

**Status:** 🚧 In Development
