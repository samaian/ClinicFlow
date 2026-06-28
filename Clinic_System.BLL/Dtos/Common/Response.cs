using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic_System;

public class Response<T> 
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }

    public static Response<T> SuccessResponse(T data, string message = "Success")
    {
        return new Response<T> { Success = true, Data = data, Message = message };
    }

    public static Response<T> FailureResponse(string message, List<string>? errors = null)
    {
        return new Response<T> { Success = false, Message = message, Errors = errors };
    }
}
