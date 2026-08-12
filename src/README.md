# LINQ Migration Guide

Production source under `src/` avoids `System.Linq`. The generator has few query sites, so direct loops and indexed access are clearer than maintaining query helpers or iterator structs.

## Migration rules

1. Do not add `using System.Linq;` to production source under `src/`.
2. Use indexed loops for concrete collections such as `ImmutableArray<T>` and `List<T>`.
3. Iterate Roslyn `ISymbol` collections directly and select concrete symbol types with pattern matching.
4. Use `IsDefaultOrEmpty` and index `0` when retrieving the first item from an `ImmutableArray<T>`.
5. Use `GetMembers(name).Length != 0` when only member existence matters.
6. Keep filtering and terminal checks in one loop instead of materializing an intermediate collection.
7. Introduce a reusable query helper only after the same non-trivial operation has multiple real call sites.

Tests and benchmarks projects are outside this migration rule.

## Inlining examples

```csharp
foreach (ISymbol declaredMember in members)
{
    if (declaredMember is not IFieldSymbol instanceField || instanceField.IsStatic)
    {
        continue;
    }

    // Use instanceField.
}

Location? firstLocation = locations.IsDefaultOrEmpty ? null : locations[0];

bool containsMethod = containingType.GetMembers(methodName).Length != 0;
```
