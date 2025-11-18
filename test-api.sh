#!/bin/bash

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color
BLUE='\033[0;34m'

# Configuration
API_URL="http://localhost:5000"
PASS_COUNT=0
FAIL_COUNT=0

# Helper functions
print_test() {
    echo -e "${BLUE}🧪 Testing: $1${NC}"
}

print_pass() {
    echo -e "${GREEN}✅ PASS: $1${NC}"
    ((PASS_COUNT++))
}

print_fail() {
    echo -e "${RED}❌ FAIL: $1${NC}"
    echo -e "${RED}   Response: $2${NC}"
    ((FAIL_COUNT++))
}

print_section() {
    echo ""
    echo -e "${YELLOW}═══════════════════════════════════════${NC}"
    echo -e "${YELLOW}  $1${NC}"
    echo -e "${YELLOW}═══════════════════════════════════════${NC}"
}

# Check if API is running
print_section "API Health Check"
print_test "Checking if API is running at $API_URL"
HEALTH_CHECK=$(curl -s -o /dev/null -w "%{http_code}" "$API_URL/swagger/index.html")
if [ "$HEALTH_CHECK" == "200" ]; then
    print_pass "API is running"
else
    print_fail "API is not responding" "HTTP $HEALTH_CHECK"
    exit 1
fi

# ============================================
# PRODUCTS ENDPOINTS
# ============================================
print_section "Products Endpoints"

# GET all products (should have seed data)
print_test "GET /api/v1/products - Get all products"
RESPONSE=$(curl -s -w "\n%{http_code}" "$API_URL/api/v1/products")
HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | sed '$d')
if [ "$HTTP_CODE" == "200" ]; then
    COUNT=$(echo "$BODY" | grep -o '"id"' | wc -l | xargs)
    if [ "$COUNT" -ge "5" ]; then
        print_pass "Retrieved $COUNT products (seed data present)"
    else
        print_fail "Expected at least 5 products, got $COUNT" "$BODY"
    fi
else
    print_fail "GET all products failed" "HTTP $HTTP_CODE: $BODY"
fi

# GET product by ID
print_test "GET /api/v1/products/1 - Get product by ID"
RESPONSE=$(curl -s -w "\n%{http_code}" "$API_URL/api/v1/products/1")
HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | sed '$d')
if [ "$HTTP_CODE" == "200" ]; then
    if echo "$BODY" | grep -q '"id".*1'; then
        print_pass "Retrieved product with ID 1"
    else
        print_fail "Product data invalid" "$BODY"
    fi
else
    print_fail "GET product by ID failed" "HTTP $HTTP_CODE: $BODY"
fi

# GET non-existent product (should return 404)
print_test "GET /api/v1/products/99999 - Get non-existent product (expect 404)"
RESPONSE=$(curl -s -w "\n%{http_code}" "$API_URL/api/v1/products/99999")
HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
if [ "$HTTP_CODE" == "404" ]; then
    print_pass "Correctly returned 404 for non-existent product"
else
    print_fail "Expected 404, got $HTTP_CODE" "$RESPONSE"
fi

# POST new product
print_test "POST /api/v1/products - Create new product"
NEW_PRODUCT=$(cat <<JSON
{
  "sku": "TEST-$(date +%s)",
  "name": "Test Product",
  "price": 99.99,
  "isActive": true
}
JSON
)
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$API_URL/api/v1/products" \
  -H "Content-Type: application/json" \
  -d "$NEW_PRODUCT")
HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | sed '$d')
if [ "$HTTP_CODE" == "201" ]; then
    CREATED_PRODUCT_ID=$(echo "$BODY" | grep -o '"id":[0-9]*' | grep -o '[0-9]*')
    print_pass "Created product with ID $CREATED_PRODUCT_ID"
else
    print_fail "POST product failed" "HTTP $HTTP_CODE: $BODY"
fi

# POST duplicate SKU (should return 409)
print_test "POST /api/v1/products - Create duplicate SKU (expect 409)"
DUPLICATE_PRODUCT=$(cat <<JSON
{
  "sku": "WIDGET-001",
  "name": "Duplicate Widget",
  "price": 10.00,
  "isActive": true
}
JSON
)
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$API_URL/api/v1/products" \
  -H "Content-Type: application/json" \
  -d "$DUPLICATE_PRODUCT")
HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
if [ "$HTTP_CODE" == "409" ]; then
    print_pass "Correctly returned 409 for duplicate SKU"
else
    print_fail "Expected 409 for duplicate SKU, got $HTTP_CODE" "$RESPONSE"
fi

# PUT update product
if [ -n "$CREATED_PRODUCT_ID" ]; then
    print_test "PUT /api/v1/products/$CREATED_PRODUCT_ID - Update product"
    UPDATE_PRODUCT=$(cat <<JSON
{
  "sku": "TEST-UPDATED-$(date +%s)",
  "name": "Updated Test Product",
  "price": 199.99,
  "isActive": false
}
JSON
)
    RESPONSE=$(curl -s -w "\n%{http_code}" -X PUT "$API_URL/api/v1/products/$CREATED_PRODUCT_ID" \
      -H "Content-Type: application/json" \
      -d "$UPDATE_PRODUCT")
    HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
    if [ "$HTTP_CODE" == "204" ]; then
        print_pass "Updated product $CREATED_PRODUCT_ID"
    else
        print_fail "PUT product failed" "HTTP $HTTP_CODE: $RESPONSE"
    fi

    # DELETE product
    print_test "DELETE /api/v1/products/$CREATED_PRODUCT_ID - Delete product"
    RESPONSE=$(curl -s -w "\n%{http_code}" -X DELETE "$API_URL/api/v1/products/$CREATED_PRODUCT_ID")
    HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
    if [ "$HTTP_CODE" == "204" ]; then
        print_pass "Deleted product $CREATED_PRODUCT_ID"
    else
        print_fail "DELETE product failed" "HTTP $HTTP_CODE: $RESPONSE"
    fi
fi

# ============================================
# CUSTOMERS ENDPOINTS
# ============================================
print_section "Customers Endpoints"

# GET all customers (should have seed data)
print_test "GET /api/v1/customer - Get all customers"
RESPONSE=$(curl -s -w "\n%{http_code}" "$API_URL/api/v1/customer")
HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | sed '$d')
if [ "$HTTP_CODE" == "200" ]; then
    COUNT=$(echo "$BODY" | grep -o '"id"' | wc -l | xargs)
    if [ "$COUNT" -ge "3" ]; then
        print_pass "Retrieved $COUNT customers (seed data present)"
    else
        print_fail "Expected at least 3 customers, got $COUNT" "$BODY"
    fi
else
    print_fail "GET all customers failed" "HTTP $HTTP_CODE: $BODY"
fi

# GET customer by ID
print_test "GET /api/v1/customer/1 - Get customer by ID"
RESPONSE=$(curl -s -w "\n%{http_code}" "$API_URL/api/v1/customer/1")
HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | sed '$d')
if [ "$HTTP_CODE" == "200" ]; then
    if echo "$BODY" | grep -q '"id".*1'; then
        print_pass "Retrieved customer with ID 1"
    else
        print_fail "Customer data invalid" "$BODY"
    fi
else
    print_fail "GET customer by ID failed" "HTTP $HTTP_CODE: $BODY"
fi

# POST new customer
print_test "POST /api/v1/customer - Create new customer"
NEW_CUSTOMER=$(cat <<JSON
{
  "name": "Test Customer",
  "email": "test$(date +%s)@example.com"
}
JSON
)
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$API_URL/api/v1/customer" \
  -H "Content-Type: application/json" \
  -d "$NEW_CUSTOMER")
HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | sed '$d')
if [ "$HTTP_CODE" == "201" ]; then
    CREATED_CUSTOMER_ID=$(echo "$BODY" | grep -o '"id":[0-9]*' | grep -o '[0-9]*')
    print_pass "Created customer with ID $CREATED_CUSTOMER_ID"
else
    print_fail "POST customer failed" "HTTP $HTTP_CODE: $BODY"
fi

# POST duplicate email (should return 409)
print_test "POST /api/v1/customer - Create duplicate email (expect 409)"
DUPLICATE_CUSTOMER=$(cat <<JSON
{
  "name": "Duplicate Alice",
  "email": "alice@example.com"
}
JSON
)
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$API_URL/api/v1/customer" \
  -H "Content-Type: application/json" \
  -d "$DUPLICATE_CUSTOMER")
HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
if [ "$HTTP_CODE" == "409" ]; then
    print_pass "Correctly returned 409 for duplicate email"
else
    print_fail "Expected 409 for duplicate email, got $HTTP_CODE" "$RESPONSE"
fi

# ============================================
# ORDERS ENDPOINTS
# ============================================
print_section "Orders Endpoints"

# POST new order
print_test "POST /api/v1/orders - Create new order"
NEW_ORDER=$(cat <<JSON
{
  "customerId": 1,
  "lines": [
    {
      "productId": 1,
      "quantity": 2
    },
    {
      "productId": 2,
      "quantity": 1
    }
  ]
}
JSON
)
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$API_URL/api/v1/orders" \
  -H "Content-Type: application/json" \
  -d "$NEW_ORDER")
HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | sed '$d')
if [ "$HTTP_CODE" == "201" ]; then
    CREATED_ORDER_ID=$(echo "$BODY" | grep -o '"id":[0-9]*' | grep -o '[0-9]*' | head -1)
    print_pass "Created order with ID $CREATED_ORDER_ID"
else
    print_fail "POST order failed" "HTTP $HTTP_CODE: $BODY"
fi

# GET order by ID
if [ -n "$CREATED_ORDER_ID" ]; then
    print_test "GET /api/v1/orders/$CREATED_ORDER_ID - Get order by ID"
    RESPONSE=$(curl -s -w "\n%{http_code}" "$API_URL/api/v1/orders/$CREATED_ORDER_ID")
    HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
    BODY=$(echo "$RESPONSE" | sed '$d')
    if [ "$HTTP_CODE" == "200" ]; then
        if echo "$BODY" | grep -q '"status".*"Pending"'; then
            print_pass "Retrieved order with ID $CREATED_ORDER_ID (status: Pending)"
        else
            print_fail "Order status not Pending" "$BODY"
        fi
    else
        print_fail "GET order by ID failed" "HTTP $HTTP_CODE: $BODY"
    fi

    # PATCH order status to Paid
    print_test "PATCH /api/v1/orders/$CREATED_ORDER_ID/status - Update to Paid"
    UPDATE_STATUS=$(cat <<JSON
{
  "status": 1
}
JSON
)
    RESPONSE=$(curl -s -w "\n%{http_code}" -X PATCH "$API_URL/api/v1/orders/$CREATED_ORDER_ID/status" \
      -H "Content-Type: application/json" \
      -d "$UPDATE_STATUS")
    HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
    if [ "$HTTP_CODE" == "204" ]; then
        print_pass "Updated order status to Paid"
    else
        print_fail "PATCH order status failed" "HTTP $HTTP_CODE: $RESPONSE"
    fi

    # Try invalid transition (Paid -> Pending, should fail with 409)
    print_test "PATCH /api/v1/orders/$CREATED_ORDER_ID/status - Invalid transition (expect 409)"
    INVALID_STATUS=$(cat <<JSON
{
  "status": 0
}
JSON
)
    RESPONSE=$(curl -s -w "\n%{http_code}" -X PATCH "$API_URL/api/v1/orders/$CREATED_ORDER_ID/status" \
      -H "Content-Type: application/json" \
      -d "$INVALID_STATUS")
    HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
    if [ "$HTTP_CODE" == "409" ]; then
        print_pass "Correctly returned 409 for invalid status transition"
    else
        print_fail "Expected 409 for invalid transition, got $HTTP_CODE" "$RESPONSE"
    fi
fi

# POST order with invalid customer (should return 400)
print_test "POST /api/v1/orders - Create order with invalid customer (expect 400)"
INVALID_ORDER=$(cat <<JSON
{
  "customerId": 99999,
  "lines": [
    {
      "productId": 1,
      "quantity": 1
    }
  ]
}
JSON
)
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$API_URL/api/v1/orders" \
  -H "Content-Type: application/json" \
  -d "$INVALID_ORDER")
HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
if [ "$HTTP_CODE" == "400" ]; then
    print_pass "Correctly returned 400 for invalid customer"
else
    print_fail "Expected 400 for invalid customer, got $HTTP_CODE" "$RESPONSE"
fi

# POST order with inactive product (should return 400)
print_test "POST /api/v1/orders - Create order with inactive product (expect 400)"
INACTIVE_PRODUCT_ORDER=$(cat <<JSON
{
  "customerId": 1,
  "lines": [
    {
      "productId": 5,
      "quantity": 1
    }
  ]
}
JSON
)
RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$API_URL/api/v1/orders" \
  -H "Content-Type: application/json" \
  -d "$INACTIVE_PRODUCT_ORDER")
HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
if [ "$HTTP_CODE" == "400" ]; then
    print_pass "Correctly returned 400 for inactive product"
else
    print_fail "Expected 400 for inactive product, got $HTTP_CODE" "$RESPONSE"
fi

# ============================================
# SUMMARY
# ============================================
print_section "Test Summary"
TOTAL=$((PASS_COUNT + FAIL_COUNT))
echo -e "${BLUE}Total Tests: $TOTAL${NC}"
echo -e "${GREEN}Passed: $PASS_COUNT${NC}"
echo -e "${RED}Failed: $FAIL_COUNT${NC}"
echo ""

if [ $FAIL_COUNT -eq 0 ]; then
    echo -e "${GREEN}🎉 All tests passed!${NC}"
    exit 0
else
    echo -e "${RED}⚠️  Some tests failed. Please review the output above.${NC}"
    exit 1
fi
