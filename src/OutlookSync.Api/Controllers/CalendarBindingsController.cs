using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OutlookSync.Api.Models;
using OutlookSync.Domain.Aggregates;
using OutlookSync.Domain.Repositories;

namespace OutlookSync.Api.Controllers;

/// <summary>
/// REST API controller for managing calendar bindings
/// </summary>
[ApiController]
[Route("api/calendar-bindings")]
[Produces("application/json")]
public class CalendarBindingsController(
    ICalendarBindingRepository calendarBindingRepository,
    IUnitOfWork unitOfWork,
    ILogger<CalendarBindingsController> logger) : ControllerBase
{
    /// <summary>
    /// Gets all calendar bindings
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all calendar bindings</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CalendarBindingDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CalendarBindingDto>>> GetAll(CancellationToken cancellationToken)
    {
        logger.LogInformation("Retrieving all calendar bindings");
        
        var bindings = await calendarBindingRepository.Query.ToListAsync(cancellationToken);
        var dtos = bindings.Select(CalendarBindingDto.FromDomain).ToList();
        
        return Ok(dtos);
    }
    
    /// <summary>
    /// Gets a calendar binding by ID
    /// </summary>
    /// <param name="id">The calendar binding ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The calendar binding if found</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<CalendarBindingDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CalendarBindingDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Retrieving calendar binding {BindingId}", id);
        
        var binding = await calendarBindingRepository.GetByIdAsync(id, cancellationToken);
        
        if (binding is null)
        {
            logger.LogWarning("Calendar binding {BindingId} not found", id);
            return NotFound(new { message = $"Calendar binding with ID '{id}' not found" });
        }
        
        return Ok(CalendarBindingDto.FromDomain(binding));
    }
    
    /// <summary>
    /// Creates a new calendar binding
    /// </summary>
    /// <param name="request">The calendar binding creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created calendar binding</returns>
    [HttpPost]
    [ProducesResponseType<CalendarBindingDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CalendarBindingDto>> Create(
        [FromBody] CreateCalendarBindingRequest request, 
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Creating calendar binding: {Name} from {SourceCredentialId}/{SourceExternalId} to {TargetCredentialId}/{TargetExternalId}",
            request.Name, 
            request.SourceCredentialId, 
            request.SourceCalendarExternalId,
            request.TargetCredentialId, 
            request.TargetCalendarExternalId);
        
        // Validate that source and target are not the same
        if (request.SourceCredentialId == request.TargetCredentialId && 
            request.SourceCalendarExternalId == request.TargetCalendarExternalId)
        {
            logger.LogWarning("Attempt to create binding with same source and target");
            return BadRequest(new { message = "Source and target calendar must be different" });
        }
        
        // Check for duplicate binding
        var exists = await calendarBindingRepository.ExistsAsync(
            request.SourceCredentialId,
            request.SourceCalendarExternalId,
            request.TargetCredentialId,
            request.TargetCalendarExternalId,
            excludeBindingId: null,
            cancellationToken);
        
        if (exists)
        {
            logger.LogWarning(
                "Calendar binding already exists: {SourceCredentialId}/{SourceExternalId} -> {TargetCredentialId}/{TargetExternalId}",
                request.SourceCredentialId,
                request.SourceCalendarExternalId,
                request.TargetCredentialId,
                request.TargetCalendarExternalId);
            
            return Conflict(new 
            { 
                message = "A calendar binding with the same source and target already exists" 
            });
        }
        
        // Create the binding
        var binding = new CalendarBinding(Guid.NewGuid())
        {
            Name = request.Name,
            SourceCredentialId = request.SourceCredentialId,
            SourceCalendarExternalId = request.SourceCalendarExternalId,
            TargetCredentialId = request.TargetCredentialId,
            TargetCalendarExternalId = request.TargetCalendarExternalId,
            Configuration = request.Configuration.ToDomain()
        };
        
        // Set enabled state
        if (!request.IsEnabled)
        {
            binding.Disable();
        }
        
        await calendarBindingRepository.AddAsync(binding, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("Calendar binding created with ID {BindingId}", binding.Id);
        
        var dto = CalendarBindingDto.FromDomain(binding);
        return CreatedAtAction(nameof(GetById), new { id = binding.Id }, dto);
    }
    
    /// <summary>
    /// Updates an existing calendar binding
    /// </summary>
    /// <param name="id">The calendar binding ID</param>
    /// <param name="request">The calendar binding update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated calendar binding</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<CalendarBindingDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CalendarBindingDto>> Update(
        Guid id, 
        [FromBody] UpdateCalendarBindingRequest request, 
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating calendar binding {BindingId}", id);
        
        var binding = await calendarBindingRepository.GetByIdAsync(id, cancellationToken);
        
        if (binding is null)
        {
            logger.LogWarning("Calendar binding {BindingId} not found", id);
            return NotFound(new { message = $"Calendar binding with ID '{id}' not found" });
        }
        
        // Update name if changed
        if (binding.Name != request.Name)
        {
            binding.Rename(request.Name);
        }
        
        // Update enabled state
        if (request.IsEnabled && !binding.IsEnabled)
        {
            binding.Enable();
        }
        else if (!request.IsEnabled && binding.IsEnabled)
        {
            binding.Disable();
        }
        
        // Update configuration
        binding.UpdateConfiguration(request.Configuration.ToDomain());
        
        await calendarBindingRepository.UpdateAsync(binding, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("Calendar binding {BindingId} updated successfully", id);
        
        var dto = CalendarBindingDto.FromDomain(binding);
        return Ok(dto);
    }
    
    /// <summary>
    /// Deletes a calendar binding
    /// </summary>
    /// <param name="id">The calendar binding ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting calendar binding {BindingId}", id);
        
        var binding = await calendarBindingRepository.GetByIdAsync(id, cancellationToken);
        
        if (binding is null)
        {
            logger.LogWarning("Calendar binding {BindingId} not found", id);
            return NotFound(new { message = $"Calendar binding with ID '{id}' not found" });
        }
        
        await calendarBindingRepository.DeleteAsync(binding, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("Calendar binding {BindingId} deleted successfully", id);
        
        return NoContent();
    }
}
