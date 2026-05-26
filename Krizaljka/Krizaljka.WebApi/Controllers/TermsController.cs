using Krizaljka.WebApi.Workers.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Channels;
using Krizaljka.Domain.Terms;

namespace Krizaljka.WebApi.Controllers;

[Authorize]
[ApiController]
public class TermsController(ChannelWriter<IFileBatch> channelWriter) : BaseController
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

        if (!languageId.HasValue || !Enum.IsDefined<TermLanguage>((TermLanguage)languageId.Value))
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

}
