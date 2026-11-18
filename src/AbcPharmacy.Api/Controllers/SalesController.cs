using AbcPharmacy.Api.Contracts;
using AbcPharmacy.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AbcPharmacy.Api.Controllers;

[ApiController]
[Route("api/sales")]
[Produces("application/json")]
public class SalesController(ISaleService saleService) : ControllerBase
{
   
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<SaleResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SaleResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await saleService.GetAllAsync(cancellationToken));

    
    [HttpGet("{id:guid}")]
    [ProducesResponseType<SaleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaleResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await saleService.GetByIdAsync(id, cancellationToken));

   
    [HttpPost]
    [ProducesResponseType<SaleResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<SaleResponse>> Create(CreateSaleRequest request, CancellationToken cancellationToken)
    {
        var sale = await saleService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = sale.Id }, sale);
    }
}
