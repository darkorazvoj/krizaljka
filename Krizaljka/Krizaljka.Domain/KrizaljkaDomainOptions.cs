

namespace Krizaljka.Domain;

public sealed class KrizaljkaDomainOptions
{
    public int MaxFailedLoginAttempts { get; set; } = 6;
    public int CoolOffTimeInMinutes { get; set; } = 7;


}
