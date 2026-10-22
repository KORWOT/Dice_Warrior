using System;

namespace DiceWarrior.SymbolLoop
{
    public sealed class AuthorityDiagnostic
    {
        public readonly string kind;
        public readonly long count;
        public readonly double milliseconds;
        public AuthorityDiagnostic(string kind,long count=1,double milliseconds=0)
        {this.kind=kind;this.count=count;this.milliseconds=milliseconds;}
    }
    public interface IAuthorityTelemetry { void Record(AuthorityDiagnostic value); }
    public static class AuthorityTelemetry
    {
        public static void Emit(IAuthorityTelemetry observer,string kind,long count=1,double milliseconds=0)
        {
            // Observability must never participate in the authority transaction's success.
            try { observer?.Record(new AuthorityDiagnostic(kind,count,milliseconds)); }
            catch(Exception) { }
        }
    }
}
