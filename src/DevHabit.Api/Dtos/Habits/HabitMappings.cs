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

    public static HabitDto ToDto(this Habit habit)
    {
        return new()
        {
            Id = habit.Id,
            Name = habit.Name,
            Description = habit.Description,
            Type = habit.Type,
            Frequency = new()
            {
                Type = habit.Frequency.Type,
                TimesPerPeriod = habit.Frequency.TimesPerPeriod,
            },
            Target = new()
            {
                Value = habit.Target.Value,
                Unit = habit.Target.Unit,
            },
            Status = habit.Status,
            IsArchived = habit.IsArchived,
            EndDate = habit.EndDate,
            Milestone = habit.Milestone is null ? null : new()
            {
                Target = habit.Milestone.Target,
                Current = habit.Milestone.Current,
            },
            CreatedAtUtc = habit.CreatedAtUtc,
            UpdatedAtUtc = habit.UpdatedAtUtc,
            LastCompletedAtUtc = habit.LastCompletedAtUtc,
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
