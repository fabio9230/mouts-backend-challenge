using Ambev.DeveloperEvaluation.Application.Sales.CancelItem;
using Ambev.DeveloperEvaluation.Application.Sales.CancelSale;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.WebApi.Common;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales;

[ApiController]
[Route("api/[controller]")]
public sealed class SalesController(
    IMediator mediator,
    IMapper mapper)
    : BaseController
{
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponseWithData<SaleResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateSaleRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var command = mapper.Map<CreateSaleCommand>(request);
        command.IdempotencyKey = idempotencyKey ?? string.Empty;

        var result = await mediator
            .Send(command, cancellationToken);

        if (result.IsIdempotentReplay)
            Response.Headers["X-Idempotent-Replay"] = "true";

        return StatusCode(
            result.IsIdempotentReplay ? StatusCodes.Status200OK : StatusCodes.Status201Created,
            new ApiResponseWithData<SaleResponse>
            {
                Success = true,
                Message = result.IsIdempotentReplay
                    ? "Idempotent replay detected. Existing sale returned."
                    : "Sale created successfully",
                Data = mapper.Map<SaleResponse>(result.Sale)
            });
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponseWithData<IReadOnlyCollection<SaleResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await mediator
            .Send(new ListSalesCommand(), cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponseWithData<IReadOnlyCollection<SaleResponse>>
        {
            Success = true,
            Message = "Sales retrieved successfully",
            Data = mapper.Map<IReadOnlyCollection<SaleResponse>>(result)
        });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponseWithData<SaleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await mediator
                .Send(new GetSaleCommand { Id = id }, cancellationToken);

            return StatusCode(StatusCodes.Status200OK, new ApiResponseWithData<SaleResponse>
            {
                Success = true,
                Message = "Sale retrieved successfully",
                Data = mapper.Map<SaleResponse>(result)
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponseWithData<SaleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateSaleRequest request,
        CancellationToken cancellationToken)
    {
        var command = mapper.Map<UpdateSaleCommand>(request);
        command.Id = id;

        try
        {
            var result = await mediator
                .Send(command, cancellationToken);

            return StatusCode(StatusCodes.Status200OK, new ApiResponseWithData<SaleResponse>
            {
                Success = true,
                Message = "Sale updated successfully",
                Data = mapper.Map<SaleResponse>(result)
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            await mediator
                .Send(new DeleteSaleCommand { Id = id }, cancellationToken);

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponseWithData<SaleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await mediator
                .Send(new CancelSaleCommand { Id = id }, cancellationToken);

            return StatusCode(StatusCodes.Status200OK, new ApiResponseWithData<SaleResponse>
            {
                Success = true,
                Message = "Sale cancelled successfully",
                Data = mapper.Map<SaleResponse>(result)
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("{saleId:guid}/items/{itemId:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponseWithData<SaleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelItem(
        Guid saleId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await mediator
                .Send(new CancelItemCommand { SaleId = saleId, ItemId = itemId }, cancellationToken);

            return StatusCode(StatusCodes.Status200OK, new ApiResponseWithData<SaleResponse>
            {
                Success = true,
                Message = "Sale item cancelled successfully",
                Data = mapper.Map<SaleResponse>(result)
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
