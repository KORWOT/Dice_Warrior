using System.Net;
using System.Text.RegularExpressions;

namespace DiceWarrior.MockServer;

public interface IDevelopmentIdentityResolver { string? Resolve(HttpContext context); }

// A local testing identity selector, never an authentication system for an untrusted deployment.
public sealed class DevelopmentIdentityResolver : IDevelopmentIdentityResolver
{
    public const string Header="X-DiceWarrior-Dev-User";
    static readonly Regex ValidUser=new("\\A[a-zA-Z0-9_-]{1,64}\\z",RegexOptions.CultureInvariant);
    public string? Resolve(HttpContext context)
    {
        if(context.Connection.RemoteIpAddress is not IPAddress peer || !IPAddress.IsLoopback(peer)) return null;
        if(!context.Request.Headers.TryGetValue(Header,out var value) || value.Count!=1) return null;
        var user=value[0];
        return user!=null && ValidUser.IsMatch(user)?user:null;
    }
}
