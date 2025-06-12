using System.Linq.Expressions;
using DevHabit.API.Entities;
using DevHabit.API.Services.Sorting;

namespace DevHabit.API.DTOs.Habits;

internal static class HabitMappings
{
    public static readonly SortMappingDefinition<HabitDto, Habit> SortMapping = new()
    {
        Mappings =

        [
            new SortMapping(nameof(HabitDto.Name), nameof(Habit.Name)),

            new SortMapping(nameof(HabitDto.Description), nameof(Habit. Description)),

            new SortMapping(nameof(HabitDto.Type), nameof(Habit. Type)),

            new SortMapping(

                $"{nameof(HabitDto.Frequency)}.{nameof(FrequencyDto. Type)}",

                $"{nameof(Habit.Frequency)}.{nameof(Frequency. Type)}"),

            new SortMapping(

                $"{nameof(HabitDto.Frequency)}.{nameof(FrequencyDto. TimesPerPeriod)}",

                $"{nameof(Habit.Frequency)}.{nameof(Frequency. TimesPerPeriod)}"),

            new SortMapping(

                $"{nameof(HabitDto.Target)}.{nameof(TargetDto.Value)}",

                $"{nameof(Habit.Target)}.{nameof(Target.Value)}"),

            new SortMapping(

                $"{nameof(HabitDto.Target)}.{nameof(TargetDto. Unit)}",

                $"{nameof(Habit.Target)}.{nameof(Target.Unit)}"),

            new SortMapping(nameof(HabitDto.Status), nameof(Habit.Status)),

            new SortMapping(nameof(HabitDto.EndDate), nameof(Habit.EndDate)),

            new SortMapping(nameof(HabitDto.CreatedAtUtc), nameof(Habit.CreatedAtUtc)),

            new SortMapping(nameof(HabitDto.UpdatedAtUtc), nameof(Habit.UpdatedAtUtc)),

            new SortMapping(nameof(HabitDto.LastCompletedAtUtc), nameof(Habit.LastCompletedAtUtc))
        ]
    };

    public static HabitDto ToDto(this Habit h)
    {
        return new HabitDto
        {
            Id = h.Id,
            Name = h.Name,
            Description = h.Description,
            Type = h.Type,
            Frequency = new FrequencyDto
            {
                Type = h.Frequency.Type,
                TimesPerPeriod = h.Frequency.TimesPerPeriod
            },
            Target = new TargetDto
            {
                Value = h.Target.Value,
                Unit = h.Target.Unit
            },
            Status = h.Status,
            IsArchived = h.IsArchived,
            EndDate = h.EndDate,
            Milestone = h.Milestone == null
                   ? null : new MilestoneDto
                   {
                       Target = h.Milestone.Target,
                       Current = h.Milestone.Current
                   },
            CreatedAtUtc = h.CreatedAtUtc,
            UpdatedAtUtc = h.UpdatedAtUtc,
            LastCompletedAtUtc = h.LastCompletedAtUtc
        };
    }

    public static Habit ToEntity(this CreateHabitDto dto)
    {
        Habit habit = new()
        {
            Id = $"h_{Guid.CreateVersion7()}",
            Name = dto.Name,
            Description = dto.Description,
            Type = dto.Type,
            Frequency = new Frequency
            {
                Type = dto.Frequency.Type,
                TimesPerPeriod = dto.Frequency.TimesPerPeriod
            },
            Target = new Target
            {
                Value = dto.Target.Value,
                Unit = dto.Target.Unit
            },
            Status = HabitStatus.Ongoing,
            IsArchived = false,
            EndDate = dto.EndDate,
            Milestone = dto.Milestone is not null 
                ? new Milestone
                {
                    Target = dto.Milestone.Target,
                    Current = 0
                }
                : null,
            CreatedAtUtc = DateTime.UtcNow
        };

        return habit;
    }

    public static void UpdateFromDto(this Habit habit, UpdateHabitDto dto)
    {
        habit.Name = dto.Name;
        habit.Description = dto.Description;
        habit.Type = dto.Type;
        habit.EndDate = dto.EndDate;

        habit.Frequency = new Frequency
        {
            Type = dto.Frequency.Type,
            TimesPerPeriod = dto.Frequency.TimesPerPeriod
        };

        habit.Target = new Target
        {
            Value = dto.Target.Value,
            Unit = dto.Target.Unit
        };

        if(dto.Milestone != null)
        {
            habit.Milestone ??= new Milestone();
            habit.Milestone.Target = dto.Milestone.Target;
        }

        habit.UpdatedAtUtc = DateTime.UtcNow;
    }
}
