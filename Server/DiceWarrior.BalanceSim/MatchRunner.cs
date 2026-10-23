using DiceWarrior.SharedWorld;
using DiceWarrior.SymbolLoop;
using System.Diagnostics;

namespace DiceWarrior.BalanceSim;

public static class MatchRunner
{
    public static MatchExecution Run(CompetitionDefinition definition, BalanceScenario scenario, uint seed, string heroId, RunOptions options = null)
        => Execute(definition, scenario, seed, heroId, options, null, false);

    public static MatchExecution Replay(CompetitionDefinition definition, BalanceScenario scenario, uint seed, string heroId,
        IReadOnlyList<CompetitionCommandEntry> commands, RunOptions options = null)
        => Execute(definition, scenario, seed, heroId, options, commands, true);

    static MatchExecution Execute(CompetitionDefinition definition, BalanceScenario scenario, uint seed, string heroId,
        RunOptions options, IReadOnlyList<CompetitionCommandEntry> replay, bool replaying)
    {
        var watch = Stopwatch.StartNew();
        var execution = new MatchExecution { Result = new MatchResult { Seed = seed, Hero = heroId, Status = "BLOCKED" } };
        CompetitionState state = null;
        string stage = "input";
        int maxEncounterTurns = 0;
        var collector = new OutcomeCollector();
        options ??= new RunOptions();
        try
        {
            ArgumentNullException.ThrowIfNull(scenario);
            scenario.Validate(definition, new[] { seed });
            if (!scenario.Heroes.Contains(heroId, StringComparer.Ordinal)) throw new ArgumentException("Unsupported human adventurer");
            if (options.RestoreAfterCommands == null || options.RestoreAfterCommands.Any(n => n < 1 || n > scenario.MaxCommands))
                throw new ArgumentException("Invalid restore command number");
            if (replaying && replay == null) throw new ArgumentNullException(nameof(replay));
            var restoreAt = options.RestoreAfterCommands.ToHashSet();
            stage = "start";
            state = CompetitionRules.Start(definition, scenario.Profile, seed,
                FormattableString.Invariant($"balance-{scenario.ScenarioId}-{seed}-{heroId}"), heroId);
            while (!state.finished)
            {
                stage = "command";
                if (watch.Elapsed.TotalSeconds >= scenario.MatchTimeoutSeconds) throw new SimulationFailure("match-timeout");
                if (state.actionNumber >= scenario.MaxCommands) throw new SimulationFailure("command-limit");
                if (replaying && execution.Commands.Count >= replay.Count) throw new SimulationFailure("replay-incomplete");
                var command = replaying ? LoopJson.Copy(replay[execution.Commands.Count]) : HumanDecisionPolicies.Next(state, scenario.PolicyId);
                // Retain an attempted bad command in the failure-only minimal replay as well.
                if (command != null) execution.Commands.Add(command);
                if (command == null || command.sequence != state.actionNumber + 1 ||
                    command.targetId != CompetitionCommandRules.Target(state) || command.clientCommandId != "human-command-" + command.sequence)
                    throw new SimulationFailure("command-identity-mismatch");
                maxEncounterTurns = Math.Max(maxEncounterTurns, CompetitionRules.Human(state).encounterTurns);
                if (options.Observe) collector.Before(state, command);
                CompetitionCommandRules.Apply(state, command.kind, command.arg);
                maxEncounterTurns = Math.Max(maxEncounterTurns, CompetitionRules.Human(state).encounterTurns);
                if (options.Observe) collector.After(state, command);
                if (restoreAt.Contains(state.actionNumber))
                {
                    stage = "restore";
                    state = LoopJson.Decode<CompetitionState>(LoopJson.Encode(state));
                    CompetitionRules.Validate(state);
                }
            }
            stage = "completion";
            if (watch.Elapsed.TotalSeconds >= scenario.MatchTimeoutSeconds) throw new SimulationFailure("match-timeout");
            if (replaying && execution.Commands.Count != replay.Count) throw new SimulationFailure("replay-after-finish");
            OutcomeCollector.ValidateEnded(state);
            collector.Observations.MaxEncounterTurns = maxEncounterTurns;
            execution.Result.Participants = OutcomeCollector.Participants(state, collector.Observations);
            execution.Result.TrialWinnerCount = state.participants.Count(p => p.trialSucceeded);
            execution.Result.Status = "PASS";
        }
        catch (Exception error)
        {
            execution.Result.Failure = new MatchFailure {
                Reason = error is SimulationFailure ? error.Message : "execution-error: " + error.Message,
                ExceptionType = error.GetType().Name,
                CommandNumber = state?.actionNumber + 1 ?? 0,
                Phase = stage + ":" + (state == null ? "unstarted" : CompetitionRules.Human(state).phase.ToString()),
                Target = state == null ? null : CompetitionCommandRules.Target(state)
            };
        }
        collector.Observations.MaxEncounterTurns = maxEncounterTurns;
        collector.Observations.Measured = options.Observe;
        if (!options.Observe) collector.Observations.NotMeasured = collector.Observations.NotMeasured.Append("human-detailed-history").ToArray();
        execution.FinalState = state;
        execution.Result.Observations = collector.Observations;
        execution.Result.HumanCommands = state?.actionNumber ?? 0;
        execution.Result.WorldRound = state?.worldRound ?? 0;
        execution.Result.EndReason = state?.endReason;
        execution.Result.StateHash = state == null ? null : LoopJson.Hash(LoopJson.Encode(state));
        execution.Result.RngHash = state == null ? null : LoopJson.Hash(LoopJson.Encode(state.participants.Select(p =>
            new { p.id, p.travelRng, p.combatRng, p.eventRng, p.aiRng }).ToArray()));
        execution.Result.CommandHash = LoopJson.Hash(LoopJson.Encode(execution.Commands));
        execution.Result.ElapsedMs = watch.Elapsed.TotalMilliseconds;
        return execution;
    }

    sealed class SimulationFailure(string reason) : Exception(reason) { }
}
