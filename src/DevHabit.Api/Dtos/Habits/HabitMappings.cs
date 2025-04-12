using DevHabit.Api.Entities;

namespace DevHabit.Api.Dtos.Habits;

public static class HabitMappings
{
    public static Habit ToEntity(this CreateHabitDto dto)
    {
        return new()
        {
            Id = $"h_{Guid.CreateVersion7()}",
            Name = dto.Name,
            Description = dto.Description,
            Type = dto.Type,
            Frequency = new()
            {
                Type = dto.Frequency.Type,
                TimesPerPeriod = dto.Frequency.TimesPerPeriod,
            },
            Target = new()
            {
                Value = dto.Target.Value,
                Unit = dto.Target.Unit,
            },
            Status = HabitStatus.Ongoing,
            IsArchived = false,
            EndDate = dto.EndDate,
            Milestone = dto.Milestone is null ? null : new()
            {
                Target = dto.Milestone.Target,
                Current = 0,
            },
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public static HabitDto ToDto(this Habit x)
    {
        return new()
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            Type = x.Type,
            Frequency = new()
            {
                Type = x.Frequency.Type,
                TimesPerPeriod = x.Frequency.TimesPerPeriod,
            },
            Target = new()
            {
                Value = x.Target.Value,
                Unit = x.Target.Unit,
            },
            Status = x.Status,
            IsArchived = x.IsArchived,
            EndDate = x.EndDate,
            Milestone = x.Milestone is null ? null : new()
            {
                Target = x.Milestone.Target,
                Current = x.Milestone.Current,
            },
            CreatedAtUtc = x.CreatedAtUtc,
            UpdatedAtUtc = x.UpdatedAtUtc,
            LastCompletedAtUtc = x.LastCompletedAtUtc,
        };
    }

    public static void UpdateFromDto(this Habit habit, UpdateHabitDto dto)
    {
        habit.Name = dto.Name;
        habit.Name = dto.Name;
        habit.Description = dto.Description;
        habit.Type = dto.Type;

        habit.Frequency = new()
        {
            Type = dto.Frequency.Type,
            TimesPerPeriod = dto.Frequency.TimesPerPeriod,
        };

        habit.Target = new()
        {
            Value = dto.Target.Value,
            Unit = dto.Target.Unit,
        };

        habit.EndDate = dto.EndDate;

        if (dto.Milestone is not null)
        {
            habit.Milestone = new()
            {
                Target = dto.Milestone.Target,
            };
        }

        habit.UpdatedAtUtc = DateTime.UtcNow;
    }
}
