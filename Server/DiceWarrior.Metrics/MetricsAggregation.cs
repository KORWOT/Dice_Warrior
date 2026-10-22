using System.Globalization;

namespace DiceWarrior.Metrics;

public static class MetricsAggregation
{
    public static MeasurementAggregate Overall(IEnumerable<RequestMeasurement> records) => Aggregate("overall",records);
    public static List<MeasurementAggregate> ByEndpoint(IEnumerable<RequestMeasurement> records) => Groups(records,r=>r.endpoint);
    public static List<MeasurementAggregate> ByDay(IEnumerable<RequestMeasurement> records) => Groups(records,r=>r.startedUtc.UtcDateTime.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture));
    public static List<MeasurementAggregate> ByRun(IEnumerable<RequestMeasurement> records) => Groups(records.Where(r=>r.runKey!=null),r=>r.runKey!);
    static List<MeasurementAggregate> Groups(IEnumerable<RequestMeasurement> records,Func<RequestMeasurement,string> key) =>
        records.GroupBy(key,StringComparer.Ordinal).OrderBy(g=>g.Key,StringComparer.Ordinal).Select(g=>Aggregate(g.Key,g)).ToList();
    static MeasurementAggregate Aggregate(string key,IEnumerable<RequestMeasurement> source)
    {
        var records=source.ToList(); var sorted=records.Select(r=>r.durationMs).Order().ToArray();
        var result=new MeasurementAggregate {key=key,requests=records.Count,failures=records.Count(r=>r.outcome!="approved"),
            duplicates=records.Count(r=>Count(r,"duplicate-operation")>0||Count(r,"duplicate-submission")>0),
            requestBytes=records.Sum(r=>r.requestBytes),responseBytes=records.Sum(r=>r.responseBytes),commandCount=records.Sum(r=>(long)r.commandCount),logBytes=records.Sum(r=>r.logBytes),
            activeUsers=records.Where(r=>r.userKey!=null).Select(r=>r.userKey).Distinct(StringComparer.Ordinal).Count(),
            averageMs=sorted.Length==0?0:sorted.Average(),p50Ms=Percentile(sorted,.50),p95Ms=Percentile(sorted,.95),p99Ms=Percentile(sorted,.99),maxMs=sorted.Length==0?0:sorted[^1]};
        foreach(var record in records) foreach(var pair in record.diagnostics)
        {
            if(!result.diagnostics.TryGetValue(pair.Key,out var total)) result.diagnostics[pair.Key]=total=new();
            total.count+=pair.Value.count; total.milliseconds+=pair.Value.milliseconds;
        }
        return result;
    }
    static long Count(RequestMeasurement r,string key) => r.diagnostics.TryGetValue(key,out var value)?value.count:0;
    // Nearest rank, including all successful and failed completed requests in the retained sample.
    static double Percentile(double[] sorted,double p) => sorted.Length==0?0:sorted[Math.Max(0,(int)Math.Ceiling(p*sorted.Length)-1)];
}
