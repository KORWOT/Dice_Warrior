using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FateDice
{
    public enum PresentationPlaybackState { Idle, Playing, Completed, Cancelled, Faulted, TimedOut }

    /// <summary>Owns and supervises one presentation, including every nested iterator.</summary>
    public sealed class PresentationPlayback
    {
        public const double TimeoutSeconds = 10;
        const int StepsPerFrame = 128;
        readonly Func<double> clock;
        PlaybackIterator active;
        int generation;

        public string Label { get; private set; } = string.Empty;
        public PresentationPlaybackState State { get; private set; } = PresentationPlaybackState.Idle;
        public double StartedAt { get; private set; } = double.NaN;
        public double EndedAt { get; private set; } = double.NaN;
        public Exception Error { get; private set; }
        public bool IsPlaying => State == PresentationPlaybackState.Playing;
        public event Action<PresentationPlayback> Started;
        public event Action<PresentationPlayback> Ended;

        public PresentationPlayback(Func<double> clock = null)
        {
            this.clock = clock ?? (() => Time.realtimeSinceStartupAsDouble);
        }

        // Starts immediately; the returned iterator must be advanced or disposed by its owner.
        public IEnumerator Play(IEnumerator sequence, string label)
        {
            if (sequence == null) throw new ArgumentNullException(nameof(sequence));
            if (active != null && (active.Advancing || active.Cleaning))
                throw new InvalidOperationException("Start the next presentation after the current iterator has returned.");
            Cancel();
            if (active != null)
                throw new InvalidOperationException("An Ended observer started another presentation; it must finish or be cancelled before replacement.");
            var runner = new PlaybackIterator(this, sequence, ++generation);
            active = runner;
            Label = label ?? string.Empty;
            State = PresentationPlaybackState.Playing;
            StartedAt = clock();
            EndedAt = double.NaN;
            Error = null;
            var error = Notify(Started);
            if (error != null) runner.Finish(PresentationPlaybackState.Faulted, error);
            return runner;
        }

        public void Cancel() => active?.Finish(PresentationPlaybackState.Cancelled, null);

        Exception Notify(Action<PresentationPlayback> handlers)
        {
            if (handlers == null) return null;
            Exception error = null;
            foreach (Action<PresentationPlayback> handler in handlers.GetInvocationList())
            {
                try { handler(this); }
                catch (Exception failure) { error = Combine(error, failure); }
            }
            return error;
        }

        static Exception Combine(Exception first, Exception next) =>
            first == null ? next : next == null ? first : new AggregateException(first, next);

        sealed class PlaybackIterator : IEnumerator, IDisposable
        {
            readonly PresentationPlayback owner;
            readonly Stack<IEnumerator> stack = new Stack<IEnumerator>();
            readonly int generation;
            bool requested, terminal;
            PresentationPlaybackState reason;
            Exception error;
            public bool Advancing { get; private set; }
            public bool Cleaning { get; private set; }
            public object Current => null;

            public PlaybackIterator(PresentationPlayback owner, IEnumerator root, int generation)
            {
                this.owner = owner;
                this.generation = generation;
                stack.Push(root);
            }

            public bool MoveNext()
            {
                if (terminal || owner.active != this) return false;
                Advancing = true;
                try
                {
                    for (int step = 0; step < StepsPerFrame && !requested; step++)
                    {
                        if (CheckTimeout()) break;
                        if (stack.Count == 0)
                        {
                            Finish(PresentationPlaybackState.Completed, null);
                            break;
                        }

                        IEnumerator sequence = stack.Peek();
                        bool moved = sequence.MoveNext();
                        if (requested || CheckTimeout()) break;
                        if (!moved)
                        {
                            stack.Pop();
                            (sequence as IDisposable)?.Dispose();
                            if (stack.Count == 0) Finish(PresentationPlaybackState.Completed, null);
                            continue;
                        }

                        object yielded = sequence.Current;
                        if (requested) break;
                        if (yielded == null) break;
                        if (yielded is IEnumerator nested)
                        {
                            if (stack.Contains(nested))
                                throw new InvalidOperationException("A presentation cannot yield an iterator that is already active.");
                            stack.Push(nested);
                        }
                        else if (yielded is AsyncOperation operation) stack.Push(AwaitOperation(operation));
                        else throw new NotSupportedException(
                            $"Presentation '{owner.Label}' yielded unsupported {yielded.GetType().FullName}. " +
                            "Use null, IEnumerator, CustomYieldInstruction or AsyncOperation so the watchdog can observe every frame.");
                    }
                }
                catch (Exception failure) { Finish(PresentationPlaybackState.Faulted, failure); }
                finally
                {
                    Advancing = false;
                    if (requested) Complete();
                }
                return !terminal;
            }

            bool CheckTimeout()
            {
                double elapsed = owner.clock() - owner.StartedAt;
                if (elapsed < TimeoutSeconds) return false;
                Finish(PresentationPlaybackState.TimedOut,
                    new TimeoutException($"Presentation '{owner.Label}' exceeded {TimeoutSeconds:0}s (elapsed {elapsed:0.000}s)."));
                return true;
            }

            public void Finish(PresentationPlaybackState state, Exception failure)
            {
                if (terminal) return;
                if (!requested)
                {
                    requested = true;
                    reason = state;
                }
                error = Combine(error, failure);
                if (!Advancing && !Cleaning) Complete();
            }

            void Complete()
            {
                if (terminal || Cleaning) return;
                Cleaning = true;
                // Pop before disposing: a finally block may request cancellation again.
                while (stack.Count > 0)
                {
                    var sequence = stack.Pop();
                    try { (sequence as IDisposable)?.Dispose(); }
                    catch (Exception failure)
                    {
                        error = Combine(error, failure);
                        if (reason == PresentationPlaybackState.Completed) reason = PresentationPlaybackState.Faulted;
                    }
                }
                terminal = true;
                Cleaning = false;
                if (owner.active != this) return;
                owner.active = null;
                owner.State = reason;
                owner.EndedAt = owner.clock();
                owner.Error = error;
                var notificationError = owner.Notify(owner.Ended);
                if (notificationError != null && owner.generation == generation)
                {
                    owner.Error = Combine(owner.Error, notificationError);
                    if (owner.State == PresentationPlaybackState.Completed) owner.State = PresentationPlaybackState.Faulted;
                }
            }

            static IEnumerator AwaitOperation(AsyncOperation operation)
            {
                while (!operation.isDone) yield return null;
            }

            public void Dispose() => Finish(PresentationPlaybackState.Cancelled, null);
            public void Reset() => throw new NotSupportedException();
        }
    }
}
