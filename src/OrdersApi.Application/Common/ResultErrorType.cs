namespace OrdersApi.Application.Common;

public enum ResultErrorType
{
    None,
    NotFound,
    Validation,
    Conflict,
    ConcurrencyConflict,
    BusinessRule
}