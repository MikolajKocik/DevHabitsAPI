using DevHabit.API.DTOs.Habits;
using DevHabit.API.Entities;
using System.Linq.Expressions;

namespace DevHabit.API.DTOs.HabitTags;

internal static class HabitWithTagsQueriesBase
{
    public static Expression<Func<Habit, HabitWithTagsDto>> ProjectToHabitWithTagsDto()
    {
        return h => new HabitWithTagsDto
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
            LastCompletedAtUtc = h.LastCompletedAtUtc,
            Tags = h.Tags.Select(t => t.Name).ToArray()
        };
    }
}
