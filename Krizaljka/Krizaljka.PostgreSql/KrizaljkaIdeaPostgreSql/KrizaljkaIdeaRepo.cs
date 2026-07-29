using Krizaljka.Domain.Core.Stuff.DatabaseStuff;
using Krizaljka.PostgreSql.Postgres.Stuff;
using System.Data;
using Krizaljka.Domain.Core.Stuff.Pagination;
using Krizaljka.Domain.Idea;
using Krizaljka.PostgreSql.Postgres.Stuff.Models;
using Krizaljka.PostgreSql.Sql;

namespace Krizaljka.PostgreSql.KrizaljkaIdeaPostgreSql;

internal class KrizaljkaIdeaRepo(IDbSession<ConnStrings> dbSession)
    : BaseRepo<ConnStrings>(dbSession), IKrizaljkaIdeaRepo
{
    public Task<string?> InsertAsync(
        int languageId,
        int status,
        string themeName,
        int templateRows,
        int templateCols,
        int templateZeroBlocksNum,
        List<long> themeTerms,
        List<long> otherTerms,
        int minutesPerTemplate,
        int maxNumOfCompletedTemplates,
        List<long> templateIdsOnly,
        List<long> templateIdsExcluded,
        long ranById,
        DateTimeOffset createdOn,
        CancellationToken ct) =>
        BaseExecuteWithOutAsync<string>(
            $"call {Procs.KrizaljkaIdeaInsert} (@languageId, @status, @themeName, @templateRows, @templateCols, @templatezeroblocksnum, @themeTerms, @otherTerms, @minutesPerTemplate, @maxNumOfCompletedTemplates, @templateIdsOnly, @templateIdsExcluded, @createdOn, @RanById, null);",
            new SqlParams()
                .Add("languageId", languageId)
                .Add("status", status)
                .Add("themeName", themeName)
                .Add("templateRows", templateRows)
                .Add("templateCols", templateCols)
                .Add("templatezeroblocksnum", templateZeroBlocksNum)
                .AddJsonList("themeTerms", themeTerms)
                .AddJsonList("otherTerms", otherTerms)
                .Add("minutesPerTemplate", minutesPerTemplate)
                .Add("maxNumOfCompletedTemplates", maxNumOfCompletedTemplates)
                .AddJsonList("templateIdsOnly", templateIdsOnly)
                .AddJsonList("templateIdsExcluded", templateIdsExcluded)
                .Add("createdOn", createdOn)
                .Add("ranById", ranById)
                .AddOutput("newId", DbType.String),
            "newId",
            ConnStrings.Core,
            ct);

    public Task<PaginatedResult<List<KrizaljkaIdeaListItem>>> GetListAsync(IPaginationCore paginationCore,
        CancellationToken ct) =>
        BaseGetPaginatedListAsync<KrizaljkaIdeaListItem, KrizaljkaIdeaListItemDao>(
            paginationCore,
            Procs.KrizaljkaIdeaView,
            KrizaljkaIdeaListItemDao.ToDaoPaginationParameters,
            "",
            ConnStrings.Core,
            ct);

    public Task<PaginatedResult<List<KrizaljkaIdeaTermListItem>>> GetTermsListAsync(
        IPaginationCore paginationCore,
        List<(string, object?)>? additionalDynamicParameters,
        CancellationToken ct) =>
        BaseGetPaginatedListAsync<KrizaljkaIdeaTermListItem, KrizaljkaIdeaTermListItemDao>(
            paginationCore,
            Procs.KrizaljkaIdeaTermsView,
            KrizaljkaIdeaTermListItemDao.ToDaoPaginationParameters,
            "id = @ideaId",
            ConnStrings.Core,
            additionalDynamicParameters,
            ct);

    public Task<PaginatedResult<List<KrizaljkaIdeaTemplateListItem>>> GetTemplatesListAsync(
        IPaginationCore paginationCore,
        List<(string, object?)>? additionalDynamicParameters,
        CancellationToken ct) =>
        BaseGetPaginatedListAsync<KrizaljkaIdeaTemplateListItem, KrizaljkaIdeaTemplateListItemDao>(
            paginationCore,
            Procs.KrizaljkaIdeaTemplatesView,
            KrizaljkaIdeaTemplateListItemDao.ToDaoPaginationParameters,
            "id = @ideaId",
            ConnStrings.Core,
            additionalDynamicParameters,
            ct);

    public Task<PaginatedResult<List<KrizaljkaIdeaAllTemplatesListItem>>> GetAllTemplatesListAsync(
        IPaginationCore paginationCore,
        List<(string, object?)>? additionalDynamicParameters,
        CancellationToken ct) =>
        BaseGetPaginatedListAsync<KrizaljkaIdeaAllTemplatesListItem, KrizaljkaIdeaAllTemplatesListItemDao>(
            paginationCore,
            Procs.KrizaljkaIdeaAllTemplatesView,
            KrizaljkaIdeaAllTemplatesListItemDao.ToDaoPaginationParameters,
            "id = @ideaId",
            ConnStrings.Core,
            additionalDynamicParameters,
            ct);

    public Task<KrizaljkaIdeaConfig?> GetConfigAsync(string id, CancellationToken ct) =>
        BaseGetAsync<KrizaljkaIdeaConfig, KrizaljkaIdeaConfigDao>(
            $"select {DaoUtils.GetSelectColumns(typeof(KrizaljkaIdeaConfigDao))} from {Procs.KrizaljkaIdeaView} where id = @id",
            new SqlParams()
                .Add("id", id),
            ConnStrings.Core,
            ct);

    public Task<string?> UpdateConfigAsync(
        string id, 
        int languageId,
        string themeName, 
        int templateRows, 
        int templateCols,
        int templateZeroBlocksNum,
        int minutesPerTemplate, 
        int maxNumOfCompletedTemplates, 
        string changestamp, 
        CancellationToken ct) =>
        BaseExecuteWithOutAsync<string?>(
            $"call {Procs.KrizaljkaIdeaUpdateConfig} (@id, @languageId, @themeName, @templateRows, @templateCols, @templateZeroBlocksNum, @minutesPerTemplate, @maxNumOfCompletedTemplates, @changestamp,  null);",
            new SqlParams()
                .Add("id", id)
                .Add("languageId", languageId)
                .Add("themeName", themeName)
                .Add("templateRows", templateRows)
                .Add("templateCols", templateCols)
                .Add("templateZeroBlocksNum", templateZeroBlocksNum)
                .Add("minutesPerTemplate", minutesPerTemplate)
                .Add("maxNumOfCompletedTemplates", maxNumOfCompletedTemplates)
                .Add("changestamp", changestamp)
                .AddOutput("newchangestamp", dbType: DbType.String),
            "newchangestamp",
            ConnStrings.Core,
            ct);

    public Task<string?> AddIdAsync(
        string id,
        string columnName,
        long newId,
        string changestamp,
        CancellationToken ct) =>
        BaseExecuteWithOutAsync<string?>(
            $"call {Procs.KrizaljkaIdeaAddId} (@id, @columnName, @newId, @changestamp,  null);",
            new SqlParams()
                .Add("id", id)
                .Add("columnName", columnName)
                .Add("newId", newId)
                .Add("changestamp", changestamp)
                .AddOutput("newchangestamp", dbType: DbType.String),
            "newchangestamp",
            ConnStrings.Core,
            ct);

    public Task<string?> RemoveIdAsync(
        string id,
        string columnName,
        long itemId,
        string changestamp,
        CancellationToken ct)
        =>
            BaseExecuteWithOutAsync<string?>(
                $"call {Procs.KrizaljkaIdeaRemoveId} (@id, @columnName, @itemId, @changestamp,  null);",
                new SqlParams()
                    .Add("id", id)
                    .Add("columnName", columnName)
                    .Add("itemId", itemId)
                    .Add("changestamp", changestamp)
                    .AddOutput("newchangestamp", dbType: DbType.String),
                "newchangestamp",
                ConnStrings.Core,
                ct);
}
