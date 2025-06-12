using System.Linq;
using System.Linq.Expressions;
using System.Linq.Dynamic.Core;
using System.Runtime.InteropServices.JavaScript;
using DevHabit.API.Database;
using DevHabit.API.DTOs.Habits;
using DevHabit.API.DTOs.HabitTags;
using DevHabit.API.Entities;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DevHabit.API.Services.Sorting;

namespace DevHabit.API.Controllers;

[ApiController]
[Route("habits")]
public sealed class HabitsController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<HabitsCollectionDto>> GetHabits([FromQuery] HabitQueryParameters query, SortMappingProvider sortMappingProvider)
    {
        if(!sortMappingProvider.ValidateMapping<HabitDto, Habit>(query.Sort))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided sort parameter isnt' valid: '{query.Sort}'");
        }

        query.Search = query.Search?.Trim().ToLower();

        SortMapping[] sortMappings = sortMappingProvider.GetMappings<HabitDto, Habit>();

        IQueryable<Habit> habitsQuery = dbContext.Habits;  

        if(!string.IsNullOrWhiteSpace(query.Search))
        {
            habitsQuery = habitsQuery.Where(h => EF.Functions.ILike(h.Name, $"%{query.Search}%") ||
                 h.Description != null && EF.Functions.ILike(h.Description, $"%{query.Search}%"))
                .QueryHasValue(query.Status.HasValue, h => h.Status == query.Status) // ex method
                .QueryHasValue(query.Type.HasValue, h => h.Type == query.Type)
                .ApplySort(query.Sort, sortMappings);
        }

        List<HabitDto> habits = await habitsQuery
            .Select(HabitQueries.ProjectToDto())
            .AsNoTracking()
            .ToListAsync();

        return Ok(new HabitsCollectionDto
        {
            Data = habits
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<HabitWithTagsDto>> GetHabit(string id)
    {
        HabitWithTagsDto? habit = await dbContext
            .Habits
            .Where(h => h.Id == id)
            .Select(HabitWithTagsQueriesBase.ProjectToHabitWithTagsDto())
            .FirstOrDefaultAsync();

        if (habit is null)
        {
            return NotFound();
        }

        return Ok(habit);
    }

    [HttpPost]
    public async Task<ActionResult<HabitDto>> CreateHabit(
        CreateHabitDto createHabitDto,
        IValidator<CreateHabitDto> validator)
    {

        ValidationResult validationResult = await validator.ValidateAsync(createHabitDto);

        if(!validationResult.IsValid)
        {
            return BadRequest(validationResult.ToDictionary());
        }

        Habit habit = createHabitDto.ToEntity();

        bool exists = await dbContext.Habits.AnyAsync(h => h.Name == habit.Name);

        if(exists)
        {
            return Conflict($"Habit '{habit.Name}' already exists");
        }

        dbContext.Habits.Add(habit);

        await dbContext.SaveChangesAsync();

        HabitDto habitDto = habit.ToDto();     

        return CreatedAtAction(nameof(GetHabit), new { id = habitDto.Id }, habitDto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateHabit(string id, UpdateHabitDto updateHabitDto)
    {
        Habit habit = await dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id);

        if(habit is null)
        {
            return NotFound();
        }

        habit.UpdateFromDto(updateHabitDto);

        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult> PatchHabit(string id, JsonPatchDocument<HabitDto> patchDocument)
    {
        Habit habit = await dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id);

        if (habit is null)
        {
            return NotFound();
        }

        HabitDto habitDto = habit.ToDto();

        patchDocument.ApplyTo(habitDto, ModelState); 

        if(!TryValidateModel(habitDto))
        {
            return ValidationProblem(ModelState);
        }

        habit.Name = habitDto.Name;
        habit.Description = habitDto.Description;
        habit.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteHabit(string id)
    {
        Habit habit = await dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id);

        if (habit is null)
        {
            return NotFound();
        }

        dbContext.Habits.Remove(habit);

        await dbContext.SaveChangesAsync();

        return NoContent();
    }
}
