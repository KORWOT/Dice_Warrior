using System.Net;
using DiceWarrior.SymbolLoop;

namespace DiceWarrior.MockServer;

public sealed class MockServerSettings
{
    public required string Url {get;init;}
    public required string DatabasePath {get;init;}
    public required Rules Rules {get;init;}
    public uint? Seed {get;init;}
    public bool EnableFaults {get;init;}
    public const string RulesSha256="A9FF4C352251F537C93752E9AF82ADBA2725465FEC8E868D9659512740910B38";

    public static MockServerSettings Read(IConfiguration config)
    {
        var url=config["urls"]??"http://127.0.0.1:5087";
        if(!Uri.TryCreate(url,UriKind.Absolute,out var uri) || uri.Scheme!="http" || uri.AbsolutePath!="/" ||
            uri.Query.Length!=0 || uri.Fragment.Length!=0 || uri.UserInfo.Length!=0 ||
            !IPAddress.TryParse(uri.Host,out var address) || !IPAddress.IsLoopback(address))
            throw new ArgumentException("Mock URL must be a single numeric HTTP loopback address.");
        var rulesFile=Path.Combine(AppContext.BaseDirectory,"Content","symbol-loop-mvp-1.json");
        var rules=LoopJson.Decode<Rules>(File.ReadAllText(rulesFile));
        if(LoopJson.Hash(LoopJson.Encode(rules))!=RulesSha256)
            throw new InvalidDataException("Versioned rules snapshot changed. Assign and validate a new rules version before serving it.");
        LoopRules.ValidateRules(rules);
        uint? seed=null;
        if(config["Mock:Seed"] is string fixedSeed)
        {
            if(!uint.TryParse(fixedSeed,out var parsed) || parsed==0) throw new ArgumentException("Development seed must be a positive uint32.");
            seed=parsed;
        }
        var db=config["Mock:DatabasePath"]??Path.Combine(AppContext.BaseDirectory,"data","authority.db");
        return new MockServerSettings {Url=url,DatabasePath=Path.GetFullPath(db),Rules=rules,Seed=seed,
            EnableFaults=bool.TryParse(config["Mock:EnableFaults"],out var faults)&&faults};
    }
}
