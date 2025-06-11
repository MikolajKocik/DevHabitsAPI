using System.Linq.Expressions;
using DevHabit.API.Entities;

namespace DevHabit.API.DTOs.Tags;

public static class TagsQueries
{
    public static Expression<Func<Tag, TagDto>> ProjectToDto()
    {
        return t => new TagDto
        { 
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            CreatedAtUtc = t.CreatedAtUtc,
            UpdatedAtUtc = t.UpdatedAtUtc
        };

    }
}
