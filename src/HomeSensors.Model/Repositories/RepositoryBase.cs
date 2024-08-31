using System.Runtime.CompilerServices;

namespace HomeSensors.Model.Repositories;

public class RepositoryBase
{
    protected string ThisClassName { get; }

    public RepositoryBase()
    {
        ThisClassName = GetType().Name;
    }

    protected string GetCaller([CallerMemberName] string callerMemberName = "unknown")
    {
        return $"{ThisClassName}.{callerMemberName}";
    }

    protected virtual string GetTag([CallerMemberName] string callerMemberName = "unknown") => $"Query called from {GetCaller(callerMemberName)}.";
}
