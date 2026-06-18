
namespace Krizaljka.PostgreSql.Sql;

internal static class Procs
{
    public static string AppUserLoginGet => "cr.appUserLoginGet_v1";
    public static string AppUserIncreaseLoginAttempt => "cr.appUserIncreaseLoginAttempt_v1";
    public static string AppUserIncreaseLoginAttemptAndBlock => "cr.appUserIncreaseLoginAttemptAndBlock_v1";
    public static string AppUserUnblock => "cr.appUserUnblock_v1";
    public static string AppUserResetLoginAttempts => "cr.appUserResetLoginAttempts_v1";
    public static string MeView => "cr.meView_v1";


    public static string TemplateInsert => "cr.templateinsert_v1";
    public static string TemplateView => "cr.templateView_V1";
    public static string TemplateUpdateIsActive => "cr.templateUpdateIsActive_v1";

    public static string TermInsert => "cr.terminsert_v1";
    public static string TermView  => "cr.termView_V1";
    public static string TermUpdateIsActive => "cr.termUpdateIsActive_v1";
    public static string TermUpdateTerm => "cr.termUpdateTerm_v1";

    public static string TermDescriptionInsert => "cr.termDescriptionInsert_v1";
    public static string TermDescriptionView => "cr.TermDescriptionView_V1";
    public static string TermDescriptionUpdate => "cr.termupdatedescription_v1";
    public static string TermDescriptionDelete => "cr.TermDescriptionDelete_V1 ";
    public static string TermImportBatchInsert => "cr.termimportbatchinsert_v1";
}
