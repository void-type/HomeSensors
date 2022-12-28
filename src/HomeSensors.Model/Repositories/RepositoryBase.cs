using System.Runtime.CompilerServices;

namespace HomeSensors.Model.Repositories;

public class RepositoryBase
{
    protected string ThisClassName { get; }

    public RepositoryBase()
    {
        ThisClassName = GetType().Name;
    }

    protected virtual string GetTag([CallerMemberName] string callerMemberName = "unknown") => $"Query called from {ThisClassName}.{callerMemberName}.";
}
