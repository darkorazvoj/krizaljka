using Krizaljka.Domain.Terms;

namespace Krizaljka.Console;

public static class InMemoryDatabase
{
    // <term, Term>
    public static SortedDictionary<string, List<IValidTerm>> TermsDb = [];
    // <length, Term>
    public static SortedDictionary<int, List<IValidTerm>> LengthTermsDb = [];
    // <category name, id>
    public static SortedDictionary<string, int> CategoriesDb = [];

}
