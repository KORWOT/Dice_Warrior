#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using Watch = System.Diagnostics.Stopwatch;

namespace FateDice.Tests
{
    [Category("RecoveryPerformance")]
    public sealed class RecoveryStabilityPerformanceTests
    {
        [Serializable] public sealed class Sample
        {
            public double totalMs, nextFrameMs;
            public long allocatedBytes;
            public long monoHeapDeltaBytes;
            public int[] calls;
            public double[] stageMs;
        }
        [Serializable] public sealed class Row
        {
            public int pastRuns, settlements, operations;
            public long fileBytes;
            public List<Sample> lobby = new List<Sample>(), checkpoint = new List<Sample>();
        }
        [Serializable] public sealed class Report
        {
            public string utc, unity, cpu, os, sourceHash;
            public string environment = "Editor PlayMode / Mono; 720x1280; warm OS cache; nextFrameMs includes Editor/test harness scheduling";
            public string history = "Real StartRunAsync/explicit abandon commands; 0/100/1000 abandoned past runs and one active run. Not a win/shop worst-case workload.";
            public string[] phases = { "primaryRead", "decodeAndIntegrity", "storeDocumentValidation", "encode", "primaryAtomicWrite", "backupAtomicWrite" };
            public string timing = "Stages are inside LocalPlayerDataStore only; service/Core validation, copying and UI work remain in totalMs. No cold-cache or device-performance claim.";
            public int warmups = 1, samples = 5;
            public bool allocationCounterSupported;
            public string memory = "allocatedBytes=-1 when the Mono per-thread counter does not advance; monoHeapDeltaBytes is retained heap delta, can be negative after GC, and is not total allocation.";
            public List<Row> rows = new List<Row>();
        }
        sealed class MemoryStore : IPlayerDataStore
        {
            public PlayerSaveDocument document;
            public string ProfileId => "performance-player";
            public bool Exists => document != null;
            public IMetaRequestJournal RequestJournal { get; }
            public MemoryStore(string path) { RequestJournal = new LocalMetaRequestJournal(path, ProfileId); }
            public PlayerSaveDocument Read() => document.Copy();
            public void Write(long revision, PlayerSaveDocument candidate)
            {
                Assert.That(candidate.revision, Is.EqualTo(revision));
                Assert.That(document?.revision ?? 0, Is.EqualTo(revision));
                candidate.Validate(ProfileId);
                document = candidate.Copy(); document.revision++; document.Validate(ProfileId);
            }
        }
        GameApplication app;
        MetaProgressionConfig config;
        string directory, evidence;
        readonly Dictionary<string, byte[]> protectedFiles = new Dictionary<string, byte[]>();
        [UnitySetUp] public IEnumerator SetUp()
        {
            Assert.That(GameApplication.Current, Is.Null);
            directory = Path.Combine(Path.GetTempPath(), "FateDiceRecoveryPerformance", Guid.NewGuid().ToString("N"));
            string profile = LocalPlayerDataStore.PlayerPath(Path.Combine(Application.persistentDataPath, "FateDiceMeta"), "local-development");
            foreach (string path in new[] { profile, profile + ".backup", profile + ".pending", Path.Combine(Application.persistentDataPath, "FateDiceLocal", "run.json") })
                protectedFiles[path] = File.Exists(path) ? File.ReadAllBytes(path) : null;
            config = ScriptableObject.CreateInstance<MetaProgressionConfig>();
            evidence = Path.GetFullPath(Path.Combine(Application.dataPath, "../artifacts/recovery-stability/performance-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".json"));
            yield return null;
        }
        [UnityTearDown] public IEnumerator TearDown()
        {
            if (app) Object.Destroy(app.gameObject);
            if (config) Object.Destroy(config);
            yield return null;
            foreach (var entry in protectedFiles)
            {
                Assert.That(File.Exists(entry.Key), Is.EqualTo(entry.Value != null), entry.Key);
                if (entry.Value != null) CollectionAssert.AreEqual(entry.Value, File.ReadAllBytes(entry.Key), entry.Key);
            }
            protectedFiles.Clear();
            string allowed = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "FateDiceRecoveryPerformance")) + Path.DirectorySeparatorChar;
            Assert.That(Path.GetFullPath(directory).StartsWith(allowed, StringComparison.OrdinalIgnoreCase), Is.True);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ValidAccumulatedHistoryMeasuresLobbyAndCheckpointCosts()
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameApplication>("Assets/_Project/Features/Run/Prefabs/GameApplication.prefab");
            var rules = prefab.controller.config.Snapshot();
            var memory = new MemoryStore(Path.Combine(directory, "generation-unused.json"));
            var generator = new LocalMetaProgressionService(memory, () => rules.DeepCopy(), config, new FixedSeedSource(33));
            var documents = new Dictionary<int, PlayerSaveDocument>();
            for (int i = 0; i <= 1000; i++)
            {
                var task = generator.StartRunAsync(new StartRunRequest { requestId = Guid.NewGuid().ToString("N"),
                    expectedRevision = memory.document?.profile.revision ?? 0, abandonActiveRun = true });
                Assert.That(task.IsCompleted, Is.True, "Generation uses only the synchronous local test authority.");
                task.GetAwaiter().GetResult();
                if (i == 0 || i == 100 || i == 1000)
                {
                    memory.document.Validate(memory.ProfileId);
                    Assert.That(memory.document.settlements.Count, Is.EqualTo(i));
                    Assert.That(memory.document.operations.Count, Is.EqualTo(i + 1));
                    documents.Add(i, memory.document.Copy());
                }
                if (i % 10 == 0) yield return null;
            }
            var report = new Report { utc = DateTime.UtcNow.ToString("O"), unity = Application.unityVersion,
                cpu = SystemInfo.processorType, os = SystemInfo.operatingSystem,
                sourceHash = MetaJson.Hash(string.Join("\n", new[] {
                    "_Project/Features/Run/Presentation/RunUIController.Meta.cs", "_Project/Features/Meta/Runtime/LocalMetaProgressionService.cs",
                    "_Project/Features/Meta/Runtime/PlayerSaveDocument.cs", "_Project/Features/Meta/Runtime/LocalPlayerDataRecovery.cs"
                }.Select(p => File.ReadAllText(Path.Combine(Application.dataPath, p))))) };
            long allocationProbe = GC.GetAllocatedBytesForCurrentThread(); var allocation = new byte[65536]; GC.KeepAlive(allocation);
            report.allocationCounterSupported = GC.GetAllocatedBytesForCurrentThread() > allocationProbe;
            foreach (var pair in documents)
            {
                var disk = new LocalPlayerDataStore(Path.Combine(directory, pair.Key.ToString(), "player.json"), memory.ProfileId);
                // Materialize the exact validated authority document, preserving all revisions/IDs.
                var encode = typeof(LocalPlayerDataStore).GetMethod("EncodeDocument", BindingFlags.Instance | BindingFlags.NonPublic);
                var bytes = (byte[])encode.Invoke(disk, new object[] { pair.Value });
                Directory.CreateDirectory(Path.GetDirectoryName(disk.Path)); File.WriteAllBytes(disk.Path, bytes);
                var loaded = disk.Read(); loaded.Validate(memory.ProfileId);
                Assert.That(MetaJson.Encode(loaded), Is.EqualTo(MetaJson.Encode(pair.Value)));
                var service = new LocalMetaProgressionService(disk, () => rules.DeepCopy(), config, new FixedSeedSource(33));
                UnityEditor.PlayModeWindow.SetCustomRenderingResolution(720, 1280, "Recovery save measurement");
                app = GameApplication.Bootstrap(prefab, metaService: service);
                yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Lobby.unity");
                float until = Time.realtimeSinceStartup + 10;
                while (app.controller.Busy || !(app.controller.UI.ActiveScreen is MenuUI))
                { Assert.That(Time.realtimeSinceStartup, Is.LessThan(until)); yield return null; }
                yield return Layout();
                var diagnostics = new LocalSaveDiagnostics(); disk.Diagnostics = diagnostics;
                var row = new Row { pastRuns = pair.Key, fileBytes = bytes.Length,
                    settlements = loaded.settlements.Count, operations = loaded.operations.Count };
                var run = new RunSession(loaded.run, service.RunStore);
                for (int i = -1; i < report.samples; i++)
                {
                    var sample = Measure(diagnostics, report.allocationCounterSupported, () => { app.controller.RefreshView(); Canvas.ForceUpdateCanvases(); });
                    double frame = Time.realtimeSinceStartupAsDouble; yield return null;
                    sample.nextFrameMs = sample.totalMs + (Time.realtimeSinceStartupAsDouble - frame) * 1000;
                    Assert.That(sample.calls[4] + sample.calls[5], Is.Zero, "Lobby refresh is read-only.");
                    CollectionAssert.AreEqual(bytes, File.ReadAllBytes(disk.Path));
                    if (i >= 0) row.lobby.Add(sample);
                }
                for (int i = -1; i < report.samples; i++)
                {
                    run.RecordElapsed(1);
                    var sample = Measure(diagnostics, report.allocationCounterSupported, () => Assert.That(run.SaveCheckpoint(), Is.True));
                    double frame = Time.realtimeSinceStartupAsDouble; yield return null;
                    sample.nextFrameMs = sample.totalMs + (Time.realtimeSinceStartupAsDouble - frame) * 1000;
                    Assert.That(sample.calls[0], Is.EqualTo(2), "Candidate read and locked pre-commit CAS read must both remain.");
                    Assert.That(sample.calls[4], Is.EqualTo(1)); Assert.That(sample.calls[5], Is.EqualTo(1));
                    if (i >= 0) row.checkpoint.Add(sample);
                }
                var after = disk.Read(); after.Validate(memory.ProfileId);
                Assert.That(after.run.playedSeconds, Is.EqualTo(loaded.run.playedSeconds + 6));
                Assert.That(after.run.sequence, Is.EqualTo(loaded.run.sequence));
                Assert.That(MetaJson.Encode(after.operations), Is.EqualTo(MetaJson.Encode(loaded.operations)));
                Assert.That(MetaJson.Encode(after.settlements), Is.EqualTo(MetaJson.Encode(loaded.settlements)));
                report.rows.Add(row); Directory.CreateDirectory(Path.GetDirectoryName(evidence)); File.WriteAllText(evidence, JsonUtility.ToJson(report, true));
                disk.Diagnostics = null; Object.Destroy(app.gameObject); yield return null;
                Assert.That(GameApplication.Current, Is.Null);
            }
            Debug.Log("Recovery performance evidence: " + evidence);
            Assert.That(report.rows.Count, Is.EqualTo(3));
            // Coherent display contract: one actual profile document read per lobby refresh.
            Assert.That(report.rows.All(r => r.lobby.All(s => s.calls[0] == 1)), Is.True,
                "Duplicate document reads in a single lobby refresh; inspect the completed measurements before optimizing.");
        }
        static Sample Measure(LocalSaveDiagnostics diagnostics, bool allocationCounterSupported, Action action)
        {
            diagnostics.Reset(); long heapBefore = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
            long before = GC.GetAllocatedBytesForCurrentThread(); long started = Watch.GetTimestamp();
            action(); double duration = (Watch.GetTimestamp() - started) * 1000.0 / Watch.Frequency;
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            long heapDelta = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() - heapBefore;
            return new Sample { totalMs = duration, allocatedBytes = allocationCounterSupported ? allocated : -1,
                monoHeapDeltaBytes = heapDelta, calls = diagnostics.calls.ToArray(), stageMs = diagnostics.milliseconds.ToArray() };
        }
        static IEnumerator Layout() { Canvas.ForceUpdateCanvases(); yield return null; yield return null; Canvas.ForceUpdateCanvases(); }
    }
}
#endif
