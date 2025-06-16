namespace BffAgenda.Application.DTOs.Base;

public class ResponseDto<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }

    public static ResponseDto<T> SuccessResponse(
        T data,
        string message = "Operação realizada com sucesso."
    )
    {
        return new ResponseDto<T>
        {
            Success = true,
            Message = message,
            Data = data,
        };
    }

    public static ResponseDto<T> ErrorResponse(string message)
    {
        return new ResponseDto<T>
        {
            Success = false,
            Message = message,
            Data = default,
        };
    }
}
