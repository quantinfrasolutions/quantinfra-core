using QuantInfra.Common.Interfaces.Api;

namespace UI.Interfaces;

public class ValidationException : Exception
{
    public ValidationException(ValidationProblemDetails details)
    {
        Details = details;
    }
    
    public ValidationProblemDetails Details { get; }
}