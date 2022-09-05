using CcsSso.Domain.Exceptions;
using CcsSso.Shared.Domain.Constants;
using CcsSso.Shared.Extensions;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.ExternalApi.Middleware
{
  public class InputValidationMiddleware
  {

    private RequestDelegate _next;

    public InputValidationMiddleware(RequestDelegate next)
    {
      _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
      using (var reader = new StreamReader(context.Request.Body, encoding: Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
      {
        var bodyString = await reader.ReadToEndAsync();

        // Reset the request body stream position so the next middleware can read it
        context.Request.Body.Position = 0;
        if (bodyString.IsInvalidCharactorIncluded(RegexExpression.INVALID_CHARACTORS_FOR_API_INPUT))
        {
          throw new CcsSsoException("ERROR_INVALID_INPUT_CHARACTER");
        }
      }
      await _next(context);
    }
  }


}
