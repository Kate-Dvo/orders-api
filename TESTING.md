# API Testing Guide

This document explains how to run automated tests against the Orders API.

## Prerequisites

- API must be running on `http://localhost:5000`
- Database must be seeded with initial data

## Option 1: Automated Bash Script (Recommended for CI/Regression)

### Run all tests automatically:

```bash
./test-api.sh
```

### What it tests:

**Products:**
- ✅ GET all products (verifies seed data)
- ✅ GET product by ID
- ✅ GET non-existent product (404)
- ✅ POST new product
- ✅ POST duplicate SKU (409 conflict)
- ✅ PUT update product
- ✅ DELETE product

**Customers:**
- ✅ GET all customers (verifies seed data)
- ✅ GET customer by ID
- ✅ POST new customer
- ✅ POST duplicate email (409 conflict)

**Orders:**
- ✅ POST new order with multiple lines
- ✅ GET order by ID
- ✅ PATCH order status (Pending → Paid)
- ✅ PATCH invalid transition (409 conflict)
- ✅ POST order with invalid customer (400)
- ✅ POST order with inactive product (400)

### Output:

The script provides color-coded output:
- 🟢 Green = Test passed
- 🔴 Red = Test failed
- 🔵 Blue = Test info

At the end, you'll see a summary:
```
═══════════════════════════════════════
  Test Summary
═══════════════════════════════════════
Total Tests: 25
Passed: 25
Failed: 0

🎉 All tests passed!
```

### Exit codes:
- `0` = All tests passed
- `1` = One or more tests failed

### Use in CI:
```bash
# In your GitHub Actions workflow
- name: Run API tests
  run: ./test-api.sh
```

---

## Option 2: HTTP File (Rider/VSCode)

For interactive testing with visual results, use `api-tests.http`.

### In Rider:
1. Open `api-tests.http`
2. You'll see a ▶️ "Run" button next to each request
3. Click to execute individual requests
4. Results appear in the "Run" tool window

### In VSCode:
1. Install the "REST Client" extension
2. Open `api-tests.http`
3. Click "Send Request" above each request
4. Results appear in a new panel

### Features:
- Run requests one at a time
- See formatted JSON responses
- View headers and status codes
- Easy to modify and experiment
- Test edge cases and validation

---

## Tips

### Before running tests:
1. Make sure the API is running:
   ```bash
   cd src/OrdersApi.Api
   dotnet run
   ```

2. Verify Swagger is accessible:
   ```
   http://localhost:5000/swagger
   ```

### After code changes:
1. Rebuild the solution
2. Restart the API
3. Run `./test-api.sh` to verify nothing broke

### Debugging failures:
- The script shows the full HTTP response body on failures
- Check Rider's console for API logs
- Use `api-tests.http` to replay individual failing requests

### Common issues:
- **"API is not responding"**: API not running or wrong port
- **"Expected at least 5 products, got 0"**: Database not seeded
- **Tests passing when they should fail**: Check validations in controllers

---

## Future Enhancements

When you implement additional features, update the test scripts:

- [ ] JWT authentication tests (401/403 responses)
- [ ] Rate limiting tests (429 responses)
- [ ] Pagination tests (page/pageSize parameters)
- [ ] ETag/concurrency tests (412 responses)
- [ ] API versioning tests (v2 endpoints)
- [ ] Health check tests (/health/live, /health/ready)
- [ ] Caching tests (304 Not Modified)

---

## Quick Reference

| File | Purpose |
|------|---------|
| `test-api.sh` | Automated regression testing (bash script) |
| `api-tests.http` | Interactive manual testing (HTTP file) |
| `TESTING.md` | This documentation |

**Recommended workflow:**
1. Make code changes
2. Run `./test-api.sh` for quick validation
3. Use `api-tests.http` to debug specific failures
4. Commit only when all tests pass ✅
