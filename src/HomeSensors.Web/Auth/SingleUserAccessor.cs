using VoidCore.Model.Auth;

namespace HomeSensors.Web.Auth;

public class SingleUserAccessor : ICurrentUserAccessor
{
    private static readonly DomainUser _singleUser = new("SingleUser", Array.Empty<string>());

    public DomainUser User => _singleUser;
}
