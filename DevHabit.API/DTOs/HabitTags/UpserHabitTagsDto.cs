namespace DevHabit.API.DTOs.HabitTags;

public sealed record UpserHabitTagsDto
{
    public required List<string> TagIds { get; set; }
}
