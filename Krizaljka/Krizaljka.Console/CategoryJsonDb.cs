
namespace Krizaljka.Console;

public record CategoryJsonDbItem(string Name, int Id);

public record CategoryJsonDb(List<CategoryJsonDbItem> Categories);
