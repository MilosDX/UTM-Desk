namespace UTMDesk.Api.Exceptions;

public sealed class ApiException : Exception
{
    public ApiException(int statusCode, string mensaje)
        : base(mensaje)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}
