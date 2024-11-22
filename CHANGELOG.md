# Change Log

## [0.3.5] 2024-11-22

### Added

- OutputEnumerableAttribute to allow formatting of enumerable of objects with OutputFormatToStringAttribute
- OutputDictionaryAttribute to allow formatting of dictionaries of values that are objects with OutputFormatToStringAttribute

## [0.3.4] 2024-11-02

### Fixed

- Vulnerable NuGet packages

## [0.3.3] 2024-05-28

### Fixed

- Nullable syntax update for more picky Rider analyzer

## [0.3.2] 2024-05-27

### Added

- Inheritance supported without a formatter.

[0.3.1] 2024-02-10

### Added

- `NoQuote` option for OutputFormatProvider, similar to [DebuggerDisplayAttribute](https://learn.microsoft.com/en-us/visualstudio/debugger/using-the-debuggerdisplay-attribute?view=vs-2022)

### Fixed

- Incorrect display name of types for OutputFormatProvider error messages since it wasn't using CSharpErrorMessageFormat. It would display with `global::` prefix
- Issues with OutputFormatProvider for nullable types

## [0.3.0] 2024-01-12

### Added

- OutputFormatToString attribute to allow custom formatting of an object in ToString()
- OutputFormatProvider attribute to use a method to format an object

### Fixed

- Fix for property type not working for multi-level namespace #14

## [0.2.3] 2023-12-29

### Added

- Sort option for sorting properties in output
- Source Links in csproj to allow debugging of NuGet package

### Changed

- Split out the formatting method into Seekatar.Mask.Mask* methods to make it easier to use outside of generated code.

## [0.2.2] 2023-11-30

### Added

- Support for getting the properties of the parent class, too.

### Fixed

- Encoding of values for JSON output

## [0.2.1] 2023-11-14

### Changed

- Downgraded the Microsoft.CodeAnalysis.CSharp NuGet package to `4.3.0` to run with .NET SDK 6.0.416

## [0.2.0] 2023-11-06

### Added

- OptionPropertyToStringGenerator that allows masking of a class that you don't own
- SuffixLen to the OutputMaskAttribute
- minimal-api sample, with Dockerfile for testing different SDKs

### Changed

- Downgraded the Microsoft.CodeAnalysis.CSharp NuGet package to run with .NET SDK 7.1.201
- Change Regex error text from `***!` to `"***Regex no match***!`

## [0.1.4] 2023-10-11

### Changed

- NuGet Ref of `Microsoft.CodeAnalysis.CSharp` from `4.7.0` to `4.6.0`
- Generate code for .NET 6 (no raw strings)

## [0.1.3] 2023-10-07

### Added

- Support for Json switch on class attribute to output as JSON
- Support for using properties in the Title

### Changed

- Quote regex type formats
- Change value of a null object from `<null>` to `null`

## [0.1.2-prerelease] 2023-09-05

### Added

- Validation of Regex during editing to create a warning if it is invalid.
- Separate README.md for the package
- Help links to warning and error messages

### Changed

- Regex doesn't need double escaping of backslash

### Fixed

- Integration tests set current culture to avoid failing on Linux

## [0.1.1-prerelease] 2023-09-04

Initial release
