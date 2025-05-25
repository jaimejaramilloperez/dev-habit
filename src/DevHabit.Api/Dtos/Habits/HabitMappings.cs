using DevHabit.Api.Common;
using DevHabit.Api.Dtos.Common;
using DevHabit.Api.Entities;

namespace DevHabit.Api.Dtos.Habits;

internal static class HabitMappings
{
    public static readonly SortMappingDefinition<HabitDto, Habit> SortMapping = new()
    {
        Mappings = [
            new SortMapping(nameof(HabitDto.Id), nameof(Habit.Id)),
            new SortMapping(nameof(HabitDto.Name), nameof(Habit.Name)),
            new SortMapping(nameof(HabitDto.Description), nameof(Habit.Description)),
            new SortMapping(
                $"{nameof(HabitDto.Frequency)}.{nameof(FrequencyDto.Type)}",
                $"{nameof(Habit.Frequency)}.{nameof(Frequency.Type)}"),
            new SortMapping(nameof(HabitDto.Status), nameof(Habit.Status)),
            new SortMapping(nameof(HabitDto.CreatedAtUtc), nameof(Habit.CreatedAtUtc)),
            new SortMapping(nameof(HabitDto.UpdatedAtUtc), nameof(Habit.UpdatedAtUtc)),
            new SortMapping(nameof(HabitDto.LastCompletedAtUtc), nameof(Habit.LastCompletedAtUtc)),
        ],
    };

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
            habit.Milestone ??= new Milestone();
            habit.Milestone.Target = dto.Milestone.Target;
        }

        habit.UpdatedAtUtc = DateTime.UtcNow;
    }
}
