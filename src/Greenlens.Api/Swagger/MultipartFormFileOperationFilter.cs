using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Greenlens.Api.Swagger;

/// <summary>
/// Swashbuckle fails when an action mixes <c>[FromForm]</c> scalar fields with
/// <c>IFormFile</c> parameters. Rebuild those operations as multipart/form-data.
/// </summary>
public sealed class MultipartFormFileOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var formParameters = context.ApiDescription.ParameterDescriptions
            .Where(p => p.Source == BindingSource.Form || p.Source == BindingSource.FormFile)
            .ToList();

        if (formParameters.Count == 0 || !formParameters.Any(p => IsFileType(p.Type)))
            return;

        var properties = new Dictionary<string, OpenApiSchema>();
        var encoding = new Dictionary<string, OpenApiEncoding>();

        foreach (var parameter in formParameters)
        {
            if (IsFileType(parameter.Type))
            {
                properties[parameter.Name] = new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary"
                };
                encoding[parameter.Name] = new OpenApiEncoding
                {
                    ContentType = "application/octet-stream"
                };
                continue;
            }

            properties[parameter.Name] = context.SchemaGenerator.GenerateSchema(
                parameter.Type,
                context.SchemaRepository);
        }

        operation.RequestBody = new OpenApiRequestBody
        {
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = properties
                    },
                    Encoding = encoding.Count > 0 ? encoding : null
                }
            }
        };

        var consumedNames = formParameters
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (operation.Parameters is { Count: > 0 })
        {
            operation.Parameters = operation.Parameters
                .Where(p => !consumedNames.Contains(p.Name))
                .ToList();
        }
    }

    private static bool IsFileType(Type type)
    {
        if (type == typeof(IFormFile) || type == typeof(IFormFileCollection))
            return true;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            var elementType = type.GetGenericArguments()[0];
            return elementType == typeof(IFormFile);
        }

        return false;
    }
}
