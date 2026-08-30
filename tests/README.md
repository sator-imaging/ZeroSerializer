# Test Guidelines

## Test Method Naming & Structure Rules

1. **Explain Target Behavior/Feature**: Test method names must clearly explain the target behavior or feature being tested. They must not explain data shapes or meaningless details (e.g., avoid names like `publicFields`, `SanityCheck`, or generic `...Test`/`...Tests` suffixes).
2. **Preserve Declaration Order**: Do not change the declaration order of existing test methods or field declarations when making code updates.
