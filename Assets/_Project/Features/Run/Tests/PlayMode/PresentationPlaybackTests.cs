using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace FateDice.Tests
{
    public sealed class PresentationPlaybackTests
    {
        [Test] public void SynchronousCompletionHasStartAndEndOnceWithoutAnArtificialFrame()
        {
            double clock = 23;
            var playback = new PresentationPlayback(() => clock);
            int started = 0, ended = 0;
            playback.Started += value => { Assert.That(value, Is.SameAs(playback)); started++; };
            playback.Ended += value => { Assert.That(value.State, Is.EqualTo(PresentationPlaybackState.Completed)); ended++; };
            var runner = playback.Play(Empty(), "instant");
            Assert.That(playback.State, Is.EqualTo(PresentationPlaybackState.Playing));
            Assert.That(playback.IsPlaying, Is.True);
            Assert.That(playback.StartedAt, Is.EqualTo(23));
            Assert.That(double.IsNaN(playback.EndedAt), Is.True);
            Assert.That(runner.MoveNext(), Is.False);
            Assert.That(runner.MoveNext(), Is.False);
            (runner as IDisposable)?.Dispose();
            Assert.That(playback.State, Is.EqualTo(PresentationPlaybackState.Completed));
            Assert.That(playback.EndedAt, Is.EqualTo(23));
            Assert.That(playback.Label, Is.EqualTo("instant"));
            Assert.That(playback.Error, Is.Null);
            Assert.That(started, Is.EqualTo(1));
            Assert.That(ended, Is.EqualTo(1));
        }

        [Test] public void NullYieldWaitsOneFrameAndNestedCompletionDisposesInsideOut()
        {
            double clock = 4;
            var cleaned = new List<string>();
            var playback = new PresentationPlayback(() => clock);
            var runner = playback.Play(Outer(OneFrame(cleaned), cleaned), "nested");
            Assert.That(runner.MoveNext(), Is.True);
            Assert.That(runner.Current, Is.Null, "Only a frame yield may escape the watchdog.");
            Assert.That(cleaned, Is.Empty);
            clock = 4.25;
            Assert.That(runner.MoveNext(), Is.False);
            CollectionAssert.AreEqual(new[] { "inner", "outer" }, cleaned);
            Assert.That(playback.State, Is.EqualTo(PresentationPlaybackState.Completed));
            Assert.That(playback.EndedAt, Is.EqualTo(4.25));
        }

        [Test] public void TenSecondBoundaryTimesOutNestedCustomWaitAndUnwindsBeforeEnd()
        {
            double clock = 100;
            var cleaned = new List<string>();
            var wait = new NeverReady();
            var playback = new PresentationPlayback(() => clock);
            int ended = 0;
            playback.Ended += _ =>
            {
                CollectionAssert.AreEqual(new[] { "inner", "outer" }, cleaned);
                ended++;
            };
            var runner = playback.Play(Outer(Wait(wait, cleaned), cleaned), "fate result");
            Assert.That(runner.MoveNext(), Is.True);
            Assert.That(wait.Polls, Is.EqualTo(1));
            clock = 109.999;
            Assert.That(runner.MoveNext(), Is.True);
            Assert.That(playback.IsPlaying, Is.True);
            clock = 110;
            Assert.That(runner.MoveNext(), Is.False);
            Assert.That(playback.State, Is.EqualTo(PresentationPlaybackState.TimedOut));
            Assert.That(playback.Error, Is.TypeOf<TimeoutException>());
            Assert.That(playback.Error.Message, Does.Contain("fate result"));
            Assert.That(playback.EndedAt, Is.EqualTo(110));
            int polls = wait.Polls;
            Assert.That(runner.MoveNext(), Is.False);
            playback.Cancel();
            Assert.That(wait.Polls, Is.EqualTo(polls));
            Assert.That(ended, Is.EqualTo(1));
        }

        [Test] public void WaitForSecondsRealtimeIsSupervisedInsteadOfYieldedToUnity()
        {
            double clock = 0;
            var cleaned = new List<string>();
            var playback = new PresentationPlayback(() => clock);
            var runner = playback.Play(Wait(new WaitForSecondsRealtime(1000), cleaned), "long realtime wait");
            Assert.That(runner.MoveNext(), Is.True);
            Assert.That(runner.Current, Is.Null);
            clock = 10;
            Assert.That(runner.MoveNext(), Is.False);
            Assert.That(playback.State, Is.EqualTo(PresentationPlaybackState.TimedOut));
            CollectionAssert.AreEqual(new[] { "inner" }, cleaned);
        }

        [Test] public void NestedMoveNextExceptionFaultsAndDisposesParentFinally()
        {
            var failure = new InvalidOperationException("animation failure");
            var cleaned = new List<string>();
            var playback = new PresentationPlayback(() => 0);
            var runner = playback.Play(Outer(ThrowAfterFrame(failure, cleaned), cleaned), "attack");
            Assert.That(runner.MoveNext(), Is.True);
            Assert.That(runner.MoveNext(), Is.False);
            Assert.That(playback.State, Is.EqualTo(PresentationPlaybackState.Faulted));
            Assert.That(playback.Error, Is.SameAs(failure));
            CollectionAssert.AreEqual(new[] { "inner", "outer" }, cleaned);
        }

        [Test] public void CurrentGetterExceptionAlsoFaultsAndDisposesItsEnumerator()
        {
            var sequence = new ThrowingCurrent();
            var playback = new PresentationPlayback(() => 0);
            var runner = playback.Play(sequence, "bad current");
            Assert.That(runner.MoveNext(), Is.False);
            Assert.That(playback.State, Is.EqualTo(PresentationPlaybackState.Faulted));
            Assert.That(playback.Error, Is.SameAs(sequence.Failure));
            Assert.That(sequence.Disposals, Is.EqualTo(1));
        }

        [Test] public void OpaqueUnityWaitFaultsClearlyRatherThanBypassingTimeout()
        {
            var cleaned = new List<string>();
            var playback = new PresentationPlayback(() => 0);
            var runner = playback.Play(Wait(new WaitForSeconds(500), cleaned), "opaque wait");
            Assert.That(runner.MoveNext(), Is.False);
            Assert.That(playback.State, Is.EqualTo(PresentationPlaybackState.Faulted));
            Assert.That(playback.Error, Is.TypeOf<NotSupportedException>());
            Assert.That(playback.Error.Message, Does.Contain("WaitForSeconds"));
            CollectionAssert.AreEqual(new[] { "inner" }, cleaned);
        }

        [Test] public void CancelImmediatelyDisposesNestedWaitingIteratorsAndCannotCompleteLater()
        {
            double clock = 8;
            var cleaned = new List<string>();
            var playback = new PresentationPlayback(() => clock);
            int ended = 0;
            playback.Ended += _ => ended++;
            var runner = playback.Play(Outer(Wait(new NeverReady(), cleaned), cleaned), "close popup");
            Assert.That(runner.MoveNext(), Is.True);
            clock = 9;
            playback.Cancel();
            CollectionAssert.AreEqual(new[] { "inner", "outer" }, cleaned);
            Assert.That(playback.State, Is.EqualTo(PresentationPlaybackState.Cancelled));
            Assert.That(playback.EndedAt, Is.EqualTo(9));
            Assert.That(playback.IsPlaying, Is.False);
            playback.Cancel();
            Assert.That(runner.MoveNext(), Is.False);
            (runner as IDisposable)?.Dispose();
            Assert.That(ended, Is.EqualTo(1));
            Assert.That(playback.Error, Is.Null);
        }

        [Test] public void DisposingTheReturnedRunnerCancelsTheOwnedSequenceExactlyOnce()
        {
            var cleaned = new List<string>();
            var playback = new PresentationPlayback(() => 0);
            var runner = playback.Play(Wait(new NeverReady(), cleaned), "coroutine stop");
            Assert.That(runner.MoveNext(), Is.True);
            Assert.That(runner, Is.InstanceOf<IDisposable>());
            ((IDisposable)runner).Dispose();
            ((IDisposable)runner).Dispose();
            Assert.That(playback.State, Is.EqualTo(PresentationPlaybackState.Cancelled));
            CollectionAssert.AreEqual(new[] { "inner" }, cleaned);
        }

        [Test] public void ReuseCancelsOldRunnerAndOldResumeCannotOverwriteNewState()
        {
            double clock = 0;
            var cleaned = new List<string>();
            var playback = new PresentationPlayback(() => clock);
            int started = 0, ended = 0;
            playback.Started += _ => started++;
            playback.Ended += _ => ended++;
            var old = playback.Play(Wait(new NeverReady(), cleaned), "old");
            Assert.That(old.MoveNext(), Is.True);
            clock = 1;
            var current = playback.Play(OneFrame(new List<string>()), "new");
            Assert.That(ended, Is.EqualTo(1));
            Assert.That(old.MoveNext(), Is.False);
            ((IDisposable)old).Dispose();
            Assert.That(playback.Label, Is.EqualTo("new"));
            Assert.That(playback.State, Is.EqualTo(PresentationPlaybackState.Playing));
            Assert.That(playback.Error, Is.Null);
            Assert.That(current.MoveNext(), Is.True);
            clock = 2;
            Assert.That(current.MoveNext(), Is.False);
            Assert.That(playback.StartedAt, Is.EqualTo(1));
            Assert.That(playback.EndedAt, Is.EqualTo(2));
            Assert.That(started, Is.EqualTo(2));
            Assert.That(ended, Is.EqualTo(2));
            CollectionAssert.AreEqual(new[] { "inner" }, cleaned);
        }

        [Test] public void CancelInsideMoveNextDefersDisposalUntilIteratorReturnsAndNeverCompletes()
        {
            var cleaned = new List<string>();
            var playback = new PresentationPlayback(() => 0);
            int ended = 0;
            playback.Ended += _ => { CollectionAssert.AreEqual(new[] { "inner" }, cleaned); ended++; };
            var runner = playback.Play(CancelInside(playback, cleaned), "self cancel");
            Assert.That(runner.MoveNext(), Is.False);
            Assert.That(playback.State, Is.EqualTo(PresentationPlaybackState.Cancelled));
            Assert.That(ended, Is.EqualTo(1));
            Assert.That(runner.MoveNext(), Is.False);
        }

        [Test] public void InfiniteSynchronousChildChainYieldsFramesSoWatchdogStillRuns()
        {
            double clock = 0;
            int children = 0, disposed = 0;
            var playback = new PresentationPlayback(() => clock);
            var runner = playback.Play(EndlessChildren(() => children++, () => disposed++), "busy iterator");
            Assert.That(runner.MoveNext(), Is.True);
            Assert.That(children, Is.GreaterThan(0).And.LessThanOrEqualTo(256));
            Assert.That(runner.Current, Is.Null);
            clock = 10;
            Assert.That(runner.MoveNext(), Is.False);
            Assert.That(playback.State, Is.EqualTo(PresentationPlaybackState.TimedOut));
            Assert.That(disposed, Is.EqualTo(1));
        }

        [Test] public void CleanupFailureStillDisposesAncestorsAndReportsFault()
        {
            var cleaned = new List<string>();
            var playback = new PresentationPlayback(() => 0);
            var runner = playback.Play(Outer(new ThrowingDispose(), cleaned), "cleanup fault");
            Assert.That(runner.MoveNext(), Is.False);
            Assert.That(playback.State, Is.EqualTo(PresentationPlaybackState.Faulted));
            Assert.That(playback.Error.Message, Does.Contain("dispose failure"));
            CollectionAssert.AreEqual(new[] { "outer" }, cleaned);
        }

        [Test] public void NullSequenceIsRejectedBeforeAnActivePresentationIsCancelled()
        {
            var playback = new PresentationPlayback(() => 0);
            var runner = playback.Play(Wait(new NeverReady(), new List<string>()), "active");
            Assert.That(() => playback.Play(null, "invalid"), Throws.ArgumentNullException);
            Assert.That(playback.IsPlaying, Is.True);
            Assert.That(playback.Label, Is.EqualTo("active"));
            ((IDisposable)runner).Dispose();
        }

        [Test] public void SynchronousStepThatCrossesDeadlineCannotReportSuccessfulCompletion()
        {
            double clock = 0;
            var playback = new PresentationPlayback(() => clock);
            var runner = playback.Play(FinishAfter(() => clock = 10), "expensive step");
            Assert.That(runner.MoveNext(), Is.False);
            Assert.That(playback.State, Is.EqualTo(PresentationPlaybackState.TimedOut));
            Assert.That(playback.Error, Is.TypeOf<TimeoutException>());
        }

        [Test] public void ReplacementCannotSilentlyOrphanPlaybackStartedByAnEndedObserver()
        {
            var playback = new PresentationPlayback(() => 0);
            IEnumerator observerRunner = null;
            playback.Play(Empty(), "old");
            playback.Ended += _ =>
            {
                if (playback.Label == "old")
                    observerRunner = playback.Play(Wait(new NeverReady(), new List<string>()), "observer");
            };
            Assert.That(() => playback.Play(Empty(), "replacement"), Throws.InvalidOperationException);
            Assert.That(playback.Label, Is.EqualTo("observer"));
            Assert.That(playback.IsPlaying, Is.True);
            Assert.That(observerRunner.MoveNext(), Is.True);
            ((IDisposable)observerRunner).Dispose();
            Assert.That(playback.State, Is.EqualTo(PresentationPlaybackState.Cancelled));
        }

        static IEnumerator Empty() { yield break; }
        static IEnumerator FinishAfter(Action action) { action(); yield break; }
        static IEnumerator OneFrame(List<string> cleaned)
        {
            try { yield return null; }
            finally { cleaned.Add("inner"); }
        }
        static IEnumerator Wait(object wait, List<string> cleaned)
        {
            try { yield return wait; }
            finally { cleaned.Add("inner"); }
        }
        static IEnumerator Outer(IEnumerator inner, List<string> cleaned)
        {
            try { yield return inner; }
            finally { cleaned.Add("outer"); }
        }
        static IEnumerator ThrowAfterFrame(Exception failure, List<string> cleaned)
        {
            try { yield return null; throw failure; }
            finally { cleaned.Add("inner"); }
        }
        static IEnumerator CancelInside(PresentationPlayback playback, List<string> cleaned)
        {
            try { playback.Cancel(); yield return null; }
            finally { cleaned.Add("inner"); }
        }
        static IEnumerator EndlessChildren(Action visited, Action disposed)
        {
            try { while (true) { visited(); yield return Empty(); } }
            finally { disposed(); }
        }
        sealed class NeverReady : CustomYieldInstruction
        {
            public int Polls;
            public override bool keepWaiting { get { Polls++; return true; } }
        }
        sealed class ThrowingCurrent : IEnumerator, IDisposable
        {
            public readonly Exception Failure = new InvalidOperationException("current failure");
            public int Disposals;
            public object Current => throw Failure;
            public bool MoveNext() => true;
            public void Reset() => throw new NotSupportedException();
            public void Dispose() => Disposals++;
        }
        sealed class ThrowingDispose : IEnumerator, IDisposable
        {
            public object Current => null;
            public bool MoveNext() => false;
            public void Reset() => throw new NotSupportedException();
            public void Dispose() => throw new InvalidOperationException("dispose failure");
        }
    }
}
