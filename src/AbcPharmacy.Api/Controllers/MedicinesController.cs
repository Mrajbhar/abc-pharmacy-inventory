using AbcPharmacy.Api.Contracts;
using AbcPharmacy.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AbcPharmacy.Api.Controllers;

[ApiController]
[Route("api/medicines")]
[Produces("application/json")]
public class MedicinesController(IMedicineService medicineService) : ControllerBase
{
   
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MedicineResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MedicineResponse>>> GetAll(
        [FromQuery] string? search, CancellationToken cancellationToken) =>
        Ok(await medicineService.GetAllAsync(search, cancellationToken));

   
    [HttpGet("{id:guid}")]
    [ProducesResponseType<MedicineResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MedicineResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await medicineService.GetByIdAsync(id, cancellationToken));

   
    [HttpPost]
    [ProducesResponseType<MedicineResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<MedicineResponse>> Create(
        CreateMedicineRequest request, CancellationToken cancellationToken)
    {
        var created = await medicineService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
