# Change Log

## [0.2.1] 2023-11-14

## Changed

- Downgraded the Microsoft.CodeAnalysis.CSharp NuGet package to run with .NET SDK 6.0.416

## [0.2.0] 2023-11-06

## Added

- OptionPropertyToStringGenerator that allows masking of a class that you don't own
- SuffixLen to the OutputMaskAttribute
- minimal-api sample, with Dockerfile for testing different SDKs

## Changed

- Downgraded the Microsoft.CodeAnalysis.CSharp NuGet package to run with .NET SDK 7.1.201
- Change Regex error text from `***!` to `"***Regex no match***!`

## [0.1.4] 2023-10-11

## Changed

- NuGet Ref of `Microsoft.CodeAnalysis.CSharp` from `4.7.0` to `4.6.0`
- Generate code for .NET 6 (no raw strings)

## [0.1.3] 2023-10-07

## Added

- Support for Json switch on class attribute to output as JSON
- Support for using properties in the Title

## Changed

- Quote regex type formats
- Change value of a null object from `<null>` to `null`

## [0.1.2-prerelease] 2023-09-05

## Added

- Validation of Regex during editing to create a warning if it is invalid.
- Separate README.md for the package
- Help links to warning and error messages

## Changed

- Regex doesn't need double escaping of backslash

## Fixed

- Integration tests set current culture to avoid failing on Linux

## [0.1.1-prerelease] 2023-09-04

Initial release
