using Krizaljka.Domain.Core.Stuff.DispatcherStuff;
using Krizaljka.Domain.Core.Stuff.Pagination;
using Krizaljka.Domain.Core.Stuff.Services;
using Krizaljka.Domain.Terms;
using Krizaljka.Domain.Terms.Handlers;
using Krizaljka.WebApi.Models;
using Krizaljka.WebApi.Models.Term;
using Krizaljka.WebApi.PaginationUtils;
using Krizaljka.WebApi.Workers.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Channels;

namespace Krizaljka.WebApi.Controllers;

[Authorize]
[ApiController]
public class TermsController(
    AppDispatcher dispatcher,
    ChannelWriter<IFileBatch> channelWriter) : BaseController
{
    private const string BaseRute = "terms";

    [HttpPost(BaseRute +"/files")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<IActionResult> UploadJsonFiles(
        [FromForm] List<IFormFile>? files,
        [FromForm] int? languageId,
        CancellationToken cancellationToken)
    {
        if (files is null || files.Count == 0)
        {
            return BadRequest("no_files");
        }

        if (!languageId.HasValue || !Enum.IsDefined((TermLanguage)languageId.Value))
        {
            return BadRequest("missing_or_invalid_language");
        }

        List<FileContent> fileRecords = [];

        foreach (var file in files)
        {
            if (file.Length == 0)
            {
                continue;
            }

            using StreamReader reader = new(file.OpenReadStream());

            var content = await reader.ReadToEndAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            fileRecords.Add(new FileContent(content));
        }

        if (fileRecords.Count > 0)
        {
            await channelWriter.WriteAsync(new TermFileBatch((TermLanguage)languageId.Value, fileRecords), cancellationToken);
        }

        return Accepted(new
        {
            queued = fileRecords.Count
        });
    }

    [Route(BaseRute)]
    [HttpGet]
    public async Task<IActionResult> GetPaginatedListAsync([FromQuery] string? pg, CancellationToken ct)
    {
        var paginationCore = PaginationParser.Parse(pg);
        var result =
            await dispatcher.DispatchAsync(new GetTermsPaginatedListServiceRequest(paginationCore), ct);

        if (result is Success<PaginatedResult<List<TermListItem>>> successResult)
        {
            var list = successResult.Data.List
                .Select(x => new TermListItemResponse(
                    x.Id,
                    x.LanguageId,
                    x.RawValue,
                    x.Length, 
                    x.IsActive))
                .ToList();

            return Ok(new PaginationOffsetResponse<List<TermListItemResponse>>(
                list,
                successResult.Data.TotalRows));
        }

        return MapResult(result);
    }

    [Route(BaseRute + "/{id:long}")]
    [HttpGet]
    public async Task<IActionResult> GetAsync([FromRoute] long id, CancellationToken ct)
    {
        var result = await dispatcher.DispatchAsync(new GetTermServiceRequest(id), ct);

        if (result is Success<Term> successResult)
        {
            var term = successResult.Data;
            return Ok(new TermResponse(
                term.Id,
                (int)term.Language,
                term.Description,
                term.RawValue,
                term.DenseValue,
                term.Letters,
                term.SpaceIndexes,
                term.DashIndexes,
                term.Length,
                term.IsActive,
                term.IsPrivate,
                term.CreatedById,
                term.CreatedOn,
                term.Changestamp));
        }

        return MapResult(result);
    }

    [Route(BaseRute + "/{id:long}/active")]
    [HttpPut]
    public async Task<IActionResult> UpdateActiveAsync(
        [FromRoute] long id,
        [FromBody] UpdateActiveTermRequest? request,
        CancellationToken ct)
    {
        if (request is null)
        {
            return BadRequestBodyMissing();
        }

        return MapResult<string>(await dispatcher.DispatchAsync(new UpdateIsActiveTermServiceRequest(
                id,
                request.IsActive,
                request.Changestamp),
            ct));
    }

    [Route(BaseRute + "/{id:long}")]
    [HttpPut]
    public async Task<IActionResult> UpdateAsync(
        [FromRoute] long id,
        [FromBody] UpdateTermRequest? request,
        CancellationToken ct)
    {
        if (request is null)
        {
            return BadRequestBodyMissing();
        }

        if (!request.LanguageId.HasValue || !Enum.IsDefined((TermLanguage)request.LanguageId.Value))
        {
            return BadRequest("missing_or_invalid_language");
        }

        return MapResult<string>(await dispatcher.DispatchAsync(new UpdateTermServiceRequest(
                id,
                (TermLanguage)request.LanguageId,
                request.Description,
                request.Term,
                request.Changestamp),
            ct));
    }

}
