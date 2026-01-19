using Microsoft.AspNetCore.Mvc.Razor;

namespace HomeSensors.Web.Startup;

/// <summary>
/// Expands view search locations to include feature folders under Controllers.
/// </summary>
public class FeatureFolderViewLocationExpander : IViewLocationExpander
{
    public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
    {
        // Add feature folder locations before the default locations
        var featureFolderLocations = new[]
        {
            "/Controllers/{1}/{0}.cshtml",
            "/Controllers/{1}/Views/{0}.cshtml",
            "/Controllers/Shared/{0}.cshtml",
            "/Controllers/Shared/Views/{0}.cshtml"
        };

        return featureFolderLocations.Concat(viewLocations);
    }

    public void PopulateValues(ViewLocationExpanderContext context)
    {
        // Not needed for static view locations
    }
}
