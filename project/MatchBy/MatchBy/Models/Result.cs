namespace MatchBy.Models;

public sealed class Result<T>
{
    public bool Success { get; set; }
    public T Data { get; set; }
    public List<string> ErrorMessages { get; set; }
    public Result()
    {
        Success = false;
        Data = default!;
        ErrorMessages = [];
    }
    
    private Result(bool success, T data, List<string> errors)
    {
        Success = success;
        Data = data;
        ErrorMessages = errors;
    }

    public static Result<T> Ok(T data)
    {
        if (data is null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        return new Result<T>(true, data, []);
    }
    
    public static Result<T> Fail(params string[] errors) => new(false, default!, [..errors]);
}
