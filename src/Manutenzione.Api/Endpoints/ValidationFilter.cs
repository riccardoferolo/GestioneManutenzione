using System.ComponentModel.DataAnnotations;

namespace Manutenzione.Api.Endpoints;

/// <summary>Valida con DataAnnotations il corpo della richiesta di tipo <typeparamref name="T"/>.</summary>
public class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var body = context.Arguments.OfType<T>().FirstOrDefault();
        if (body is null)
        {
            return TypedResults.Problem("Corpo della richiesta mancante.", statusCode: StatusCodes.Status400BadRequest);
        }

        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(body, new ValidationContext(body), results, validateAllProperties: true))
        {
            var errors = results
                .SelectMany(r => r.MemberNames.DefaultIfEmpty(string.Empty), (r, m) => (Member: m, r.ErrorMessage))
                .GroupBy(x => x.Member)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage ?? "Valore non valido.").ToArray());
            return TypedResults.ValidationProblem(errors);
        }

        return await next(context);
    }
}

public static class ValidationExtensions
{
    public static RouteHandlerBuilder WithValidation<T>(this RouteHandlerBuilder builder) where T : class =>
        builder.AddEndpointFilter<ValidationFilter<T>>().ProducesValidationProblem();
}
