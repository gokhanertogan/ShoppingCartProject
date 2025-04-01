namespace ShoppingCart.Application.Common.Utils;

public class Result<T>
{
    public T Data { get; set; } = default!;
    public int StatusCode { get; set; }
    public bool IsSuccessful { get; set; }
    public List<string> Errors { get; set; } = new();
    public Pagination? Pagination { get; set; }

    public static Result<T> Success(T data, int statusCode, Pagination? pagination = null)
    {
        return new Result<T>
        {
            Data = data,
            StatusCode = statusCode,
            IsSuccessful = true,
            Pagination = pagination
        };
    }

    public static Result<T> Fail(List<string> errors, int statusCode)
    {
        return new Result<T>
        {
            Errors = errors,
            StatusCode = statusCode,
            IsSuccessful = false
        };
    }

    public static Result<T> Fail(string error, int statusCode)
    {
        return new Result<T>
        {
            Errors = new List<string> { error },
            StatusCode = statusCode,
            IsSuccessful = false
        };
    }
}