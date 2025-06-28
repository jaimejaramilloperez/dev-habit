using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace DevHabit.Api.Common.Telemetry;

public sealed class DevHabitMetrics
{
    public const string MeterName = "DevHabit.Metrics";

    private readonly Counter<long> _habitRequestCounter;
    private readonly Counter<long> _newUserRegistrationsCounter;

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "All Meter objects are managed by IMeterFactory")]
    public DevHabitMetrics(IMeterFactory meterFactory)
    {
        Meter meter = meterFactory.Create(MeterName);
        _habitRequestCounter = meter.CreateCounter<long>("devhabit.api.habit_requests.count");
        _newUserRegistrationsCounter = meter.CreateCounter<long>("devhabit.api.user_registrations.count");
    }

    public void IncreaseHabitsRequestCount(TagList? tags = null)
    {
        _habitRequestCounter.Add(1, tags ?? []);
    }

    public void IncreaseUserRegistrationsCount(TagList? tags = null)
    {
        _newUserRegistrationsCounter.Add(1, tags ?? []);
    }
}
