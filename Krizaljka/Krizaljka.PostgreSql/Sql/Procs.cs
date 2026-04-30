using System;
using System.Collections.Generic;
using System.Text;

namespace Krizaljka.PostgreSql.Sql;

internal static class Procs
{
    public static string AppUserLoginGet => "cr.appUserLoginGet_v1";
    public static string AppUserIncreaseLoginAttempt => "cr.appUserIncreaseLoginAttempt_v1";
    public static string AppUserIncreaseLoginAttemptAndBlock => "cr.appUserIncreaseLoginAttemptAndBlock_v1";
    public static string AppUserUnblock => "cr.appUserUnblock_v1";
    public static string AppUserResetLoginAttempts => "cr.appUserResetLoginAttempts_v1";

    public static string TemplateInsert => "cr.templateinsert_v1";
    public static string TemplateView => "cr.templateView_V1";
    public static string TemplateUpdateIsActive => "cr.templateUpdateIsActive_v1";
}
