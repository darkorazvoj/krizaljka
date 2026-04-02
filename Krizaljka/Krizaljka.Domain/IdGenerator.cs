
namespace Krizaljka.Domain;

// TEMP
public static class IdGenerator
{
    private static long _lastId;

    public static long GetNextId()
    {
        return Interlocked.Increment(ref _lastId);
    }
}
