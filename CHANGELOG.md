# Changelog

## [1.1.0-rc.4](https://github.com/sator-imaging/ZeroSerializer/releases/tag/v1.1.0-rc.4) (2026-08-30)

### 📣 Breaking Changes ⚠
* fix!: remove unauthorized spec by [@sator-imaging](https://github.com/sator-imaging) in [#92](https://github.com/sator-imaging/ZeroSerializer/pull/92)
* fix!: ShapeTag generation for nullable non-blittable types by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#99](https://github.com/sator-imaging/ZeroSerializer/pull/99)
### 🚀 Features
* Add ZEROS009 diagnostic for StructLayout attribute on non-struct types by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#88](https://github.com/sator-imaging/ZeroSerializer/pull/88)
### 📖 Documentation
* docs: update README usage section with serialize sample and blittable structs by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#89](https://github.com/sator-imaging/ZeroSerializer/pull/89)
### 📚 Other Changes
* refactor(test): chore by [@sator-imaging](https://github.com/sator-imaging) in [#73](https://github.com/sator-imaging/ZeroSerializer/pull/73)
* test: chore by [@sator-imaging](https://github.com/sator-imaging) in [#74](https://github.com/sator-imaging/ZeroSerializer/pull/74)
* Optimize GetByteLength Generated Code by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#71](https://github.com/sator-imaging/ZeroSerializer/pull/71)
* Add record and record struct tests to both tests and tests-unity by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#75](https://github.com/sator-imaging/ZeroSerializer/pull/75)
* Update Blittable Detection and ZEROS002 Diagnostic by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#79](https://github.com/sator-imaging/ZeroSerializer/pull/79)
* test: add diagnostic test for ZEROS003 with unmarked nested class by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#81](https://github.com/sator-imaging/ZeroSerializer/pull/81)
* Report ZEROS003 and ZEROS004 on property type location by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#84](https://github.com/sator-imaging/ZeroSerializer/pull/84)
* Disallow partial struct declarations for blittable structs by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#82](https://github.com/sator-imaging/ZeroSerializer/pull/82)
* Add unit tests for shared reference instance serialization by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#86](https://github.com/sator-imaging/ZeroSerializer/pull/86)
* Add roundtrip test for Foo and Bar structs by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#87](https://github.com/sator-imaging/ZeroSerializer/pull/87)
* Update ZEROS007 diagnostic to recommend flags enum for bool properties by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#85](https://github.com/sator-imaging/ZeroSerializer/pull/85)
* Add fallback detection to generated source preview action by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#94](https://github.com/sator-imaging/ZeroSerializer/pull/94)
* Update Diagnostic Rule IDs and diagnostic test assertions by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#95](https://github.com/sator-imaging/ZeroSerializer/pull/95)
* Wording update: change field to property across codebase, tests, and docs by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#97](https://github.com/sator-imaging/ZeroSerializer/pull/97)
* Rename test methods to explain target behavior and add test guidelines by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#98](https://github.com/sator-imaging/ZeroSerializer/pull/98)
* Use MemoryMarshal.Write for blittable struct property serialization by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#101](https://github.com/sator-imaging/ZeroSerializer/pull/101)
* Add nullable view support tests and UnityCompatibilityTests by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#102](https://github.com/sator-imaging/ZeroSerializer/pull/102)
* Update test suite with explicit types and hardcoded byte length assertions by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#103](https://github.com/sator-imaging/ZeroSerializer/pull/103)
* Add explicit expected GetByteLength assertions to tests by [@sator-imaging](https://github.com/sator-imaging) in [#105](https://github.com/sator-imaging/ZeroSerializer/pull/105)


**Full Changelog**: https://github.com/sator-imaging/ZeroSerializer/compare/v1.1.0-rc.3...v1.1.0-rc.4


## [1.1.0-rc.3](https://github.com/sator-imaging/ZeroSerializer/releases/tag/v1.1.0-rc.3) (2026-08-13)

### 📣 Breaking Changes ⚠
* Remove distinction of enum and flags-enum for ShapeTag by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#65](https://github.com/sator-imaging/ZeroSerializer/pull/65)
### 🚀 Features
* Add `GetByteLength()` to generated View structs by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#56](https://github.com/sator-imaging/ZeroSerializer/pull/56)
### 📚 Other Changes
* refactor: chore by [@sator-imaging](https://github.com/sator-imaging) in [#59](https://github.com/sator-imaging/ZeroSerializer/pull/59)
* Fix and Add Diagnostic Tests with Rule Prefix Naming by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#57](https://github.com/sator-imaging/ZeroSerializer/pull/57)
* Add XML comments to ShapeTag and ShapeHash view constants by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#60](https://github.com/sator-imaging/ZeroSerializer/pull/60)
* Rename bad naming in test method by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#63](https://github.com/sator-imaging/ZeroSerializer/pull/63)
* test: fix project structure by [@sator-imaging](https://github.com/sator-imaging) in [#69](https://github.com/sator-imaging/ZeroSerializer/pull/69)


**Full Changelog**: https://github.com/sator-imaging/ZeroSerializer/compare/v1.1.0-rc.2...v1.1.0-rc.3


## [1.1.0-rc.2](https://github.com/sator-imaging/ZeroSerializer/releases/tag/v1.1.0-rc.2) (2026-08-13)

### 📣 Breaking Changes ⚠
* Drop unexpected nested blittable struct support; require `[ZeroSerializer]` and tighten diagnostics by [@sator-imaging](https://github.com/sator-imaging) in [#48](https://github.com/sator-imaging/ZeroSerializer/pull/48)
### 🚀 Features
* Add `ShapeTag` and `ShapeHash` to Generated View Structs by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#43](https://github.com/sator-imaging/ZeroSerializer/pull/43)
* Add opt-in EmitShapeTag support by [@sator-imaging](https://github.com/sator-imaging) in [#49](https://github.com/sator-imaging/ZeroSerializer/pull/49)
### 📖 Documentation
* docs(ja): README by [@sator-imaging](https://github.com/sator-imaging) in [#39](https://github.com/sator-imaging/ZeroSerializer/pull/39)
* Describe behavior of non-marked nested types in README docs by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#42](https://github.com/sator-imaging/ZeroSerializer/pull/42)
* docs: README by [@sator-imaging](https://github.com/sator-imaging) in [#50](https://github.com/sator-imaging/ZeroSerializer/pull/50)
### 📚 Other Changes
* Use Serialize method for nested blittable structs by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#35](https://github.com/sator-imaging/ZeroSerializer/pull/35)
* Add generic type disallowance diagnostic (ZEROS008) for ZeroSerializer by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#36](https://github.com/sator-imaging/ZeroSerializer/pull/36)
* test: reflect update to integration test by [@sator-imaging](https://github.com/sator-imaging) in [#40](https://github.com/sator-imaging/ZeroSerializer/pull/40)
* chore by [@sator-imaging](https://github.com/sator-imaging) in [#44](https://github.com/sator-imaging/ZeroSerializer/pull/44)
* style: add using directives and remove inline fully-qualified type references by [@sator-imaging](https://github.com/sator-imaging) in [#45](https://github.com/sator-imaging/ZeroSerializer/pull/45)
* Prefix generated ShapeTag with version (v1) and refactor creation by [@sator-imaging](https://github.com/sator-imaging) in [#46](https://github.com/sator-imaging/ZeroSerializer/pull/46)
* refactor: project structure by [@sator-imaging](https://github.com/sator-imaging) in [#47](https://github.com/sator-imaging/ZeroSerializer/pull/47)
* style: doc comment by [@sator-imaging](https://github.com/sator-imaging) in [#51](https://github.com/sator-imaging/ZeroSerializer/pull/51)
* style: shape tag by [@sator-imaging](https://github.com/sator-imaging) in [#53](https://github.com/sator-imaging/ZeroSerializer/pull/53)
* Add tests for nullable enums and sbyte max value/array roundtrips by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#52](https://github.com/sator-imaging/ZeroSerializer/pull/52)
* fix: limit blittable detection to structs by [@sator-imaging](https://github.com/sator-imaging) in [#55](https://github.com/sator-imaging/ZeroSerializer/pull/55)


**Full Changelog**: https://github.com/sator-imaging/ZeroSerializer/compare/v1.1.0-rc.1...v1.1.0-rc.2


## [1.1.0-rc.1](https://github.com/sator-imaging/ZeroSerializer/releases/tag/v1.1.0-rc.1) (2026-08-10)

### 📣 Breaking Changes ⚠
* Return view instead of nested blittable struct itself by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#26](https://github.com/sator-imaging/ZeroSerializer/pull/26)
### 🚀 Features
* Optimize byte[] serialization in source generator by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#10](https://github.com/sator-imaging/ZeroSerializer/pull/10)
* Strict blittable struct detection by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#14](https://github.com/sator-imaging/ZeroSerializer/pull/14)
* View Struct Updates: `IsBlittable`, `AsMemory`, and `Materialize` by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#30](https://github.com/sator-imaging/ZeroSerializer/pull/30)
### ✨ Bug Fixes
* Fix cross-boundary nested struct View return types by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#31](https://github.com/sator-imaging/ZeroSerializer/pull/31)
### 📖 Documentation
* Describe that RequiredByteLength and Serialize size include offset table by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#18](https://github.com/sator-imaging/ZeroSerializer/pull/18)
* Update README.ja.md with latest implementation details by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#16](https://github.com/sator-imaging/ZeroSerializer/pull/16)
* Add conditions for negative RequiredByteLength in README.ja.md by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#22](https://github.com/sator-imaging/ZeroSerializer/pull/22)
### 📚 Other Changes
* test: avoid using power-of-two by [@sator-imaging](https://github.com/sator-imaging) in [#19](https://github.com/sator-imaging/ZeroSerializer/pull/19)
* Add sandbox generated-source CI summary workflow by [@sator-imaging](https://github.com/sator-imaging) with [@Copilot](https://github.com/Copilot) in [#17](https://github.com/sator-imaging/ZeroSerializer/pull/17)
* Add Unity compatibility test via netstandard2.1 sandbox referencing by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#11](https://github.com/sator-imaging/ZeroSerializer/pull/11)
* Add Array Roundtrip Test by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#13](https://github.com/sator-imaging/ZeroSerializer/pull/13)
* Add preprocessor directive helper method with no indentation to GeneratedSourceBuilder by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#25](https://github.com/sator-imaging/ZeroSerializer/pull/25)
* Eliminate ToDisplayString in HasSequentialPackOneLayout by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#21](https://github.com/sator-imaging/ZeroSerializer/pull/21)
* chore by [@sator-imaging](https://github.com/sator-imaging) in [#27](https://github.com/sator-imaging/ZeroSerializer/pull/27)
* Simplify attribute name matching in ZeroSerializerGenerator by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#23](https://github.com/sator-imaging/ZeroSerializer/pull/23)
* Add integration test referencing NuGet package by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#24](https://github.com/sator-imaging/ZeroSerializer/pull/24)
* test: published package testing by [@sator-imaging](https://github.com/sator-imaging) in [#28](https://github.com/sator-imaging/ZeroSerializer/pull/28)
* chore by [@sator-imaging](https://github.com/sator-imaging) in [#33](https://github.com/sator-imaging/ZeroSerializer/pull/33)

### 🎉 New Contributors
* [@sator-imaging](https://github.com/sator-imaging) with [@Copilot](https://github.com/Copilot) made their first contribution in [#17](https://github.com/sator-imaging/ZeroSerializer/pull/17)

**Full Changelog**: https://github.com/sator-imaging/ZeroSerializer/compare/v1.0.1...v1.1.0-rc.1


## [1.0.1](https://github.com/sator-imaging/ZeroSerializer/releases/tag/v1.0.1) (2026-08-09)

### 📣 Breaking Changes ⚠
* Change export namespace to SerializerNamespace by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#12](https://github.com/sator-imaging/ZeroSerializer/pull/12)
### 📚 Other Changes
* Add UTF-8 in byte[] serialization tests by [@google-labs-jules](https://github.com/google-labs-jules)[bot] in [#9](https://github.com/sator-imaging/ZeroSerializer/pull/9)

### 🎉 New Contributors
* [@google-labs-jules](https://github.com/google-labs-jules)[bot] made their first contribution in [#9](https://github.com/sator-imaging/ZeroSerializer/pull/9)

**Full Changelog**: https://github.com/sator-imaging/ZeroSerializer/compare/v1.0.0...v1.0.1


## [1.0.0](https://github.com/sator-imaging/ZeroSerializer/releases/tag/v1.0.0) (2026-08-08)

### 🚀 Features
* perf: avoid repeated property access by [@sator-imaging](https://github.com/sator-imaging) in [#7](https://github.com/sator-imaging/ZeroSerializer/pull/7)

### 🎉 New Contributors
* [@sator-imaging](https://github.com/sator-imaging) made their first contribution in [#7](https://github.com/sator-imaging/ZeroSerializer/pull/7)

**Full Changelog**: https://github.com/sator-imaging/ZeroSerializer/compare/v1.0.0-rc.4...v1.0.0


## [1.0.0-rc.4](https://github.com/sator-imaging/ZeroSerializer/releases/tag/v1.0.0-rc.4) (2026-08-08)




## [1.0.0-rc.3](https://github.com/sator-imaging/ZeroSerializer/releases/tag/v1.0.0-rc.3) (2026-08-08)




## [1.0.0-rc.2](https://github.com/sator-imaging/ZeroSerializer/releases/tag/v1.0.0-rc.2) (2026-08-08)




## [1.0.0-rc.1](https://github.com/sator-imaging/ZeroSerializer/releases/tag/v1.0.0-rc.1) (2026-08-08)

* [@github-actions](https://github.com/github-actions)[bot] made their first contribution in [#1](https://github.com/sator-imaging/ZeroSerializer/pull/1)

**Full Changelog**: https://github.com/sator-imaging/ZeroSerializer/commits/v1.0.0-rc.1
