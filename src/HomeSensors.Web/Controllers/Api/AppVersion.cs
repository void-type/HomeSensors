namespace HomeSensors.Web.Controllers.Api;

internal sealed record AppVersion(
    string? Version,
    bool IsPublicRelease,
    bool IsPrerelease,
    string GitCommitId,
    DateTime GitCommitDate,
    string AssemblyConfiguration);
