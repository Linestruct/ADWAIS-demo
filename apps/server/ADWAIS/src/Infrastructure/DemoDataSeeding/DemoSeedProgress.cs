// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System.Diagnostics;

namespace Adwais.Infrastructure.DemoDataSeeding;

public sealed class DemoSeedProgress(int totalSteps)
{
    private readonly Stopwatch _totalStopwatch = Stopwatch.StartNew();
    private readonly List<TimeSpan> _stepDurations = [];
    private Stopwatch? _stepStopwatch;
    private int _currentStep;
    private string? _currentName;

    public void StartStep(int step, string name)
    {
        if (_stepStopwatch is not null)
            throw new InvalidOperationException($"Seed step {_currentStep}/{totalSteps} is still running.");

        _currentStep = step;
        _currentName = name;
        _stepStopwatch = Stopwatch.StartNew();
        Console.WriteLine($"[{step}/{totalSteps}] {name}...");
    }

    public void CompleteStep(string? detail = null)
    {
        var duration = FinishCurrentStep();
        var suffix = string.IsNullOrWhiteSpace(detail) ? string.Empty : $" {detail}";
        Console.WriteLine(
            $"[{_currentStep}/{totalSteps}] {_currentName} completed in {Format(duration)} " +
            $"(total {Format(_totalStopwatch.Elapsed)}).{suffix}");
    }

    public void FailStep(Exception exception)
    {
        var duration = FinishCurrentStep();
        Console.WriteLine(
            $"[{_currentStep}/{totalSteps}] {_currentName} failed after {Format(duration)} " +
            $"(total {Format(_totalStopwatch.Elapsed)}): {exception.Message}");
    }

    public void SkipStep(int step, string name, string reason)
    {
        StartStep(step, name);
        CompleteStep($"Skipped: {reason}");
    }

    public void Finish()
    {
        if (_stepStopwatch is not null)
            throw new InvalidOperationException($"Seed step {_currentStep}/{totalSteps} is still running.");

        _totalStopwatch.Stop();
        var measured = TimeSpan.FromTicks(_stepDurations.Sum(duration => duration.Ticks));
        var overhead = _totalStopwatch.Elapsed - measured;
        if (overhead < TimeSpan.Zero) overhead = TimeSpan.Zero;

        Console.WriteLine(
            $"Seed pipeline finished in {Format(_totalStopwatch.Elapsed)}: " +
            $"{Format(measured)} measured across {totalSteps} steps, " +
            $"{Format(overhead)} between-step overhead.");
    }

    private TimeSpan FinishCurrentStep()
    {
        if (_stepStopwatch is null || _currentName is null)
            throw new InvalidOperationException("No seed step is currently running.");

        _stepStopwatch.Stop();
        var duration = _stepStopwatch.Elapsed;
        _stepDurations.Add(duration);
        _stepStopwatch = null;
        return duration;
    }

    private static string Format(TimeSpan duration)
        => duration.TotalMinutes >= 1
            ? $"{(int)duration.TotalMinutes}m {duration.Seconds:00}.{duration.Milliseconds / 100}s"
            : $"{duration.TotalSeconds:F1}s";
}
