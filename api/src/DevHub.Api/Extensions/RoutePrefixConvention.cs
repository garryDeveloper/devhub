using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace DevHub.Api.Extensions;

/// <summary>
/// Prefixes every controller route with a common base path, so controllers declare only their
/// own segment: <c>[Route("projects/{projectId}/issues")]</c> serves
/// <c>/api/projects/{projectId}/issues</c>.
/// </summary>
/// <remarks>
/// A convention rather than a base class with <c>[Route("api/[controller]")]</c>: a convention
/// cannot be forgotten by the next controller, and it keeps the prefix in one place if the API
/// is ever versioned under <c>/api/v2</c>.
/// </remarks>
public sealed class RoutePrefixConvention(string prefix) : IApplicationModelConvention
{
    private readonly AttributeRouteModel _prefix = new(new RouteAttribute(prefix));

    public void Apply(ApplicationModel application)
    {
        foreach (var selector in application.Controllers.SelectMany(controller => controller.Selectors))
        {
            selector.AttributeRouteModel = selector.AttributeRouteModel is null
                ? _prefix
                : AttributeRouteModel.CombineAttributeRouteModel(_prefix, selector.AttributeRouteModel);
        }
    }
}
