using DevHabit.API.Database;
using DevHabit.API.DTOs.HabitTags;
using DevHabit.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.API.Controllers;

[ApiController]
[Route("habits/{habitId}/tags")]
public sealed class HabitTagsController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpPut]
    public async Task<ActionResult> UpserHabbitTags(string habitId, UpserHabitTagsDto upserHabitTagsDto)
    {
        Habit? habit = await dbContext.Habits
            .Include(h => h.HabitTags)
            .FirstOrDefaultAsync(h => h.Id == habitId);

        if (habit is null)
        {
            return NotFound();
        }

        // we compare a new tag list with current tag list 
        var currentTagIds = habit.HabitTags.Select(ht => ht.TagId).ToHashSet();
        if(currentTagIds.SetEquals(upserHabitTagsDto.TagIds))
        {
            return NoContent();
        }

        // we are searching that we can use actual tags to add to our new tag list
        List<string> existingTagIds = await dbContext
            .Tags
            .Where(t => upserHabitTagsDto.TagIds.Contains(t.Id))
            .Select(t => t.Id)
            .ToListAsync();

        // if number of elements not equal we didnt chose a good input
        if(existingTagIds.Count != upserHabitTagsDto.TagIds.Count)
        {
            return BadRequest("One or more tag IDs is invalid");
        }

        // we remove HabitTags which doesnt contain a new tagId(from db, not new as created)
        habit.HabitTags.RemoveAll(ht => !upserHabitTagsDto.TagIds.Contains(ht.TagId));

        // we add tags which are not assigned to new tag list
        string[] tagIDsToAdd = upserHabitTagsDto.TagIds.Except(currentTagIds).ToArray();
        habit.HabitTags.AddRange(tagIDsToAdd.Select(tagId => new HabitTag
        {
            HabitId = habitId,
            TagId = tagId,
            CreatedAtUtc = DateTime.UtcNow
        }));

        await dbContext.SaveChangesAsync();

        return Ok();
    }

    [HttpDelete("{tagId}")]
    public async Task<ActionResult> DeleteHabitTag(string habitId, string tagId)
    {
        HabitTag? habitTag = await dbContext.HabitTags
            .SingleOrDefaultAsync(ht => ht.HabitId == habitId && ht.TagId == tagId);

        if(habitTag is null)
        {
            return NotFound();
        }

        dbContext.HabitTags.Remove(habitTag);

        await dbContext.SaveChangesAsync();

        return NoContent();
    }
}
