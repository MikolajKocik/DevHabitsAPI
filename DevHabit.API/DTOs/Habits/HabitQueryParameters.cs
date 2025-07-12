using System.Linq.Expressions;
using DevHabit.API.Entities;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Resources;

namespace DevHabit.API.DTOs.Habits;

public sealed record HabitQueryParameters
{
    [FromQuery(Name = "q")]
    public string? Search { get; set; }
    public HabitType? Type { get; init; }
    public HabitStatus? Status { get; init; }
    public string? Sort { get; set; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

