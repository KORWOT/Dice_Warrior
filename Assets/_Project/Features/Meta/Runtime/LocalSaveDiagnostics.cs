using System;

namespace FateDice
{
#if UNITY_EDITOR
    // Editor measurement only. Not persisted and not included in a Player build.
    public sealed class LocalSaveDiagnostics
    {
        // Primary read, decode/integrity, document validation, encode, primary write, backup write.
        public readonly int[] calls = new int[6];
        public readonly double[] milliseconds = new double[6];
        public void Reset() { Array.Clear(calls, 0, calls.Length); Array.Clear(milliseconds, 0, milliseconds.Length); }
    }
#endif
    public sealed partial class LocalPlayerDataStore
    {
#if UNITY_EDITOR
        public LocalSaveDiagnostics Diagnostics { get; set; }
#endif
        SaveMeasure MeasureSave(int phase)
        {
#if UNITY_EDITOR
            return new SaveMeasure(Diagnostics, phase);
#else
            return default;
#endif
        }
        readonly struct SaveMeasure : IDisposable
        {
#if UNITY_EDITOR
            readonly LocalSaveDiagnostics diagnostics;
            readonly int phase;
            readonly long started;
            public SaveMeasure(LocalSaveDiagnostics diagnostics, int phase)
            {
                this.diagnostics = diagnostics; this.phase = phase;
                started = diagnostics == null ? 0 : System.Diagnostics.Stopwatch.GetTimestamp();
            }
#endif
            public void Dispose()
            {
#if UNITY_EDITOR
                if (diagnostics == null) return;
                diagnostics.calls[phase]++;
                diagnostics.milliseconds[phase] += (System.Diagnostics.Stopwatch.GetTimestamp() - started) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
#endif
            }
        }
    }
}
