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
using DevHabit.API.DTOs.Common;
using DevHabit.API.Services;
using System.Dynamic;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace DevHabit.API.Controllers;

[ApiController]
[Route("habits")]
public sealed class HabitsController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetHabits(
        [FromQuery] HabitQueryParameters query,
        SortMappingProvider sortMappingProvider,
        DataShapingService dataShapingService)
    {
        if(!sortMappingProvider.ValidateMapping<HabitDto, Habit>(query.Sort))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided sort parameter isnt' valid: '{query.Sort}'");
        }

        if (!dataShapingService.Validate<HabitDto>(query.Fields))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided data shaping fields aren't valid: '{query.Fields}'");
        }

        query.Search = query.Search?.Trim().ToLower();

        SortMapping[] sortMappings = sortMappingProvider.GetMappings<HabitDto, Habit>();

        IQueryable<HabitDto> habitsQuery = dbContext
            .Habits
                .Where(h => EF.Functions.ILike(
                     h.Name, $"%{query.Search}%") ||
                     h.Description != null && EF.Functions.ILike(h.Description, $"%{query.Search}%"))
                .QueryHasValue(query.Status.HasValue, h => h.Status == query.Status) // ex method
                .QueryHasValue(query.Type.HasValue, h => h.Type == query.Type)
                .ApplySort(query.Sort, sortMappings)
                .Select(HabitQueries.ProjectToDto());

        int totalCount = await habitsQuery.CountAsync();

        List<HabitDto> habits = await habitsQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var paginationResult = new PaginationResult<ExpandoObject>
        {
            Items = dataShapingService.ShapeCollectionData(habits, query.Fields),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };

        return Ok(paginationResult);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetHabit(
        string id,
        string? fields,
        DataShapingService dataShapingService)
    {
        if (!dataShapingService.Validate<HabitWithTagsDto>(fields))
        {
            return Problem(
            statusCode: StatusCodes.Status400BadRequest,
                detail: $"The provided data shaping fields aren't valid: '{fields}'");
        }

        HabitWithTagsDto? habit = await dbContext
            .Habits
            .Where(h => h.Id == id)
            .Select(HabitWithTagsQueriesBase.ProjectToHabitWithTagsDto())
            .FirstOrDefaultAsync();

        if (habit is null)
        {
            return NotFound();
        }

        ExpandoObject shapedHabitDto = dataShapingService.ShapeData(habit, fields);

        return Ok(shapedHabitDto);
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
