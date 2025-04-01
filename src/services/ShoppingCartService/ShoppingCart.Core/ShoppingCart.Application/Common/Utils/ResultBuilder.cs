namespace ShoppingCart.Application.Common.Utils;

public class ResultBuilder<T>
{
    private T _data = default!;
    private int _statusCode;
    private bool _isSuccessful;
    private List<string> _errors = [];
    private Pagination? _pagination;

    public ResultBuilder<T> WithData(T data)
    {
        _data = data;
        return this;
    }

    public ResultBuilder<T> WithStatusCode(int statusCode)
    {
        _statusCode = statusCode;
        return this;
    }

    public ResultBuilder<T> IsSuccessful(bool isSuccessful = true)
    {
        _isSuccessful = isSuccessful;
        return this;
    }

    public ResultBuilder<T> WithErrors(List<string> errors)
    {
        _errors = errors;
        return this;
    }

    public ResultBuilder<T> WithError(string error)
    {
        _errors.Add(error);
        return this;
    }

    public ResultBuilder<T> WithPagination(int pageNumber, int pageSize, int totalItems)
    {
        _pagination = new Pagination
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalItems = totalItems
        };
        return this;
    }

    public Result<T> Build()
    {
        return new Result<T>
        {
            Data = _data,
            StatusCode = _statusCode,
            IsSuccessful = _isSuccessful,
            Errors = _errors,
            Pagination = _pagination
        };
    }
}