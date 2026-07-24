using Common.API.Responses;
using Common.API.Results;
using Common.Application.Pagination;
using Common.Domain.Results;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PeakService.API.Requests;
using PeakService.Application.Peaks.GetPeakById;
using PeakService.Application.Peaks.ListPeaks;
using PeakService.Application.Peaks.NearbyPeaks;

namespace PeakService.API.Controllers;

[ApiController]
[Route("api/peaks")]
public sealed class PeaksController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<PeakListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List([FromQuery] ListPeaksRequest request, CancellationToken cancellationToken)
    {
        Result<PagedResult<PeakListItemResponse>> result = await sender.Send(request.ToQuery(), cancellationToken);

        return result.ToActionResult(paged => Ok(paged.ToPagedResponse()));
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(PagedResponse<PeakListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search([FromQuery] SearchPeaksRequest request, CancellationToken cancellationToken)
    {
        Result<PagedResult<PeakListItemResponse>> result = await sender.Send(request.ToQuery(), cancellationToken);

        return result.ToActionResult(paged => Ok(paged.ToPagedResponse()));
    }

    [HttpGet("nearby")]
    [ProducesResponseType(typeof(PagedResponse<NearbyPeakResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Nearby([FromQuery] NearbyPeaksRequest request, CancellationToken cancellationToken)
    {
        Result<PagedResult<NearbyPeakResponse>> result = await sender.Send(request.ToQuery(), cancellationToken);

        return result.ToActionResult(paged => Ok(paged.ToPagedResponse()));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PeakDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        Result<PeakDetailResponse> result = await sender.Send(new GetPeakByIdQuery(id), cancellationToken);

        return result.ToActionResult();
    }
}
