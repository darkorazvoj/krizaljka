using Krizaljka.Domain.Template;

namespace Krizaljka.WebApi.Models.KrizaljkaTemplate;

public record KrizaljkaTemplatesExportResponse(List<KrizaljkaTemplateExport> Templates);
