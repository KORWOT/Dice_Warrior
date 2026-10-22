using System.Collections.ObjectModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DiceWarrior.Metrics;

public sealed record PriceTier(decimal? upTo,decimal usd);
public sealed record PriceRate(string unit,decimal? usd,string source,IReadOnlyList<PriceTier> tiers);
public sealed record FreeQuota(decimal firestoreReadsPerDay,decimal firestoreWritesPerDay,decimal firestoreDeletesPerDay,
    decimal firestoreStorageGiB,decimal firestoreInternetEgressGiBPerMonth,decimal cloudRunCpuCreditUsdPerMonth,
    decimal cloudRunMemoryCreditUsdPerMonth,decimal cloudRunRequestCreditUsdPerMonth);

public sealed class PricingCatalog
{
    public string version { get; }
    public string verifiedOn { get; }
    public string region { get; }
    public string edition { get; }
    public string currency { get; }
    public string cloudRunBilling { get; }
    public string egressDestination { get; }
    public int storageMonthDays { get; }
    public IReadOnlyList<string> sources { get; }
    public IReadOnlyList<string> notes { get; }
    public IReadOnlyDictionary<string,PriceRate> rates { get; }
    public FreeQuota freeQuota { get; }

    static readonly Dictionary<string,string> Units=new(StringComparer.Ordinal) {
        ["firestore-reads"]="per-100000-documents",["firestore-writes"]="per-100000-documents",["firestore-deletes"]="per-100000-documents",
        ["firestore-storage"]="per-GiB-month",["firestore-same-region-egress"]="per-GiB",["cloud-run-egress"]="per-GiB",
        ["cloud-run-cpu"]="per-vCPU-second",["cloud-run-memory"]="per-GiB-second",["cloud-run-requests"]="per-million-requests"
    };

    PricingCatalog(JObject data)
    {
        version=Text(data,"version");verifiedOn=Text(data,"verifiedOn");region=Text(data,"region");edition=Text(data,"edition");currency=Text(data,"currency");
        cloudRunBilling=Text(data,"cloudRunBilling");egressDestination=Text(data,"egressDestination");
        if(!DateOnly.TryParseExact(verifiedOn,"yyyy-MM-dd",out _) || region!="asia-northeast3" || edition!="Standard" || currency!="USD"
            || cloudRunBilling!="request-based" || egressDestination!="Korea") throw new InvalidDataException("Unsupported pricing scope.");
        storageMonthDays=checked((int)Number(data,"storageMonthDays",28,31));
        if(Number(data,"storageMonthDays",28,31)!=storageMonthDays) throw new InvalidDataException("Fractional month length.");
        sources=Array.AsReadOnly(StringArray(data,"sources",1,8));notes=Array.AsReadOnly(StringArray(data,"notes",1,32));
        foreach(var source in sources) CheckSource(source);
        var loaded=new Dictionary<string,PriceRate>(StringComparer.Ordinal);
        var raw=data["rates"] as JObject ?? throw new InvalidDataException("Missing rates object.");
        if(raw.Count>Units.Count) throw new InvalidDataException("Unexpected rate count.");
        foreach(var property in raw.Properties())
        {
            if(!Units.TryGetValue(property.Name,out var expectedUnit) || property.Value is not JObject value) throw new InvalidDataException("Unknown rate.");
            string unit=Text(value,"unit"),source=Text(value,"source");CheckSource(source);
            if(unit!=expectedUnit || !sources.Contains(source)) throw new InvalidDataException("Unverified rate unit or source.");
            var tiers=new List<PriceTier>();decimal? price=null;
            if(value["usd"] is JValue usd && usd.Type!=JTokenType.Null) price=Number(value,"usd",0,1000);
            if(value["tiers"] is JArray steps)
            {
                if(price!=null || property.Name!="cloud-run-egress" || steps.Count is <1 or >10) throw new InvalidDataException("Invalid tiered rate.");
                decimal previous=0;
                for(int i=0;i<steps.Count;i++)
                {
                    var step=steps[i] as JObject ?? throw new InvalidDataException("Invalid tier.");
                    decimal? limit=step["upTo"]?.Type==JTokenType.Null?null:Number(step,"upTo",1,1000000000);
                    if(i==steps.Count-1 ? limit!=null : limit==null || limit<=previous) throw new InvalidDataException("Tier bounds must increase and end unbounded.");
                    tiers.Add(new(limit,Number(step,"usd",0,1000)));previous=limit??previous;
                }
            }
            if(price==null && tiers.Count==0) throw new InvalidDataException("Rate has no price.");
            loaded.Add(property.Name,new(unit,price,source,tiers.AsReadOnly()));
        }
        rates=new ReadOnlyDictionary<string,PriceRate>(loaded);
        var free=data["freeQuota"] as JObject ?? throw new InvalidDataException("Missing free quotas.");
        freeQuota=new(Number(free,"firestoreReadsPerDay",0,1000000000),Number(free,"firestoreWritesPerDay",0,1000000000),
            Number(free,"firestoreDeletesPerDay",0,1000000000),Number(free,"firestoreStorageGiB",0,1000000),
            Number(free,"firestoreInternetEgressGiBPerMonth",0,1000000),Number(free,"cloudRunCpuCreditUsdPerMonth",0,10000),
            Number(free,"cloudRunMemoryCreditUsdPerMonth",0,10000),Number(free,"cloudRunRequestCreditUsdPerMonth",0,10000));
    }

    public static string DefaultPath=>Path.Combine(AppContext.BaseDirectory,"Pricing","seoul-standard-usd.json");
    public static PricingCatalog LoadDefault()=>Load(DefaultPath);
    public static PricingCatalog Load(string path)
    {
        if(new FileInfo(path).Length>65536) throw new InvalidDataException("Pricing file exceeds 64 KiB.");
        try
        {
            using var input=File.OpenText(path);
            using var reader=new JsonTextReader(input){MaxDepth=16,DateParseHandling=DateParseHandling.None,FloatParseHandling=FloatParseHandling.Decimal};
            var data=JObject.Load(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
            if(reader.Read()) throw new InvalidDataException("Trailing pricing data.");
            return new PricingCatalog(data);
        }
        catch(Exception error) when(error is JsonException or FormatException or OverflowException or InvalidCastException)
        { throw new InvalidDataException("Malformed pricing catalog.",error); }
    }

    static string Text(JObject value,string key)=>value[key]?.Type==JTokenType.String && value[key]!.Value<string>() is string text
        && text.Length is >0 and <=1024 ? text : throw new InvalidDataException("Missing or invalid "+key);
    static decimal Number(JObject value,string key,decimal min,decimal max)
    {
        if(value[key]?.Type is not (JTokenType.Integer or JTokenType.Float)) throw new InvalidDataException("Missing numeric "+key);
        decimal result=value[key]!.Value<decimal>();
        return result>=min && result<=max?result:throw new InvalidDataException("Invalid range for "+key);
    }
    static string[] StringArray(JObject value,string key,int min,int max)
    {
        if(value[key] is not JArray array || array.Count<min || array.Count>max || array.Any(item=>item.Type!=JTokenType.String || item.Value<string>()!.Length>1024))
            throw new InvalidDataException("Invalid "+key);
        return array.Values<string>().Select(item=>item!).ToArray();
    }
    static void CheckSource(string source)
    {
        if(!Uri.TryCreate(source,UriKind.Absolute,out var uri) || uri.Scheme!="https" || uri.UserInfo.Length>0
            || (uri.Host!="cloud.google.com" && uri.Host!="firebase.google.com")) throw new InvalidDataException("Source must be official HTTPS pricing documentation.");
    }
}
