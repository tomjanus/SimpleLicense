### Using the Canonicalizer Registry

```csharp
Console.WriteLine("Loading canonicalizer registry...");
CanonicalizerRegistry.LoadFromJson("config/canonicalizers.json");

foreach (var (ext, can) in CanonicalizerRegistry.Registry)
{
    Console.WriteLine($"Extension: {ext}, Canonicalizer: {can.GetType().FullName}");
}
```
