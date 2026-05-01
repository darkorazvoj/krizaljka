namespace Krizaljka.WebApi.Csrf;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class SkipCsrfAttribute : Attribute;