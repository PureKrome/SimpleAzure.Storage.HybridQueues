# Change Log
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/) and this project adheres to [Semantic Versioning](http://semver.org/).

## [Unreleased]
- Nothing yet.

## [2.0.0] - 2025-11-17
### Added
- Upgraded to .NET 10
- Nuget packages updated to latest versions

## [1.5.0] - 2025-07-24
### Added
- Nuget packages updated to latest versions

## [1.4.0] - 2024-01-13
## Changed
- Reduced the max size of a Queue Message from 64KB (the queue default value) down to 48KB because that's the max size for Base64 and we don't know what type of Queue's Encoding has been pre-defined.

## [1.3.0] - 2023-12-29
### Updated
- Fixed handling when a POCO is just too large and wasn't correctly getting stored to the blob
  by dropping the max message payload size down to 48KB for Base64 encoded messages.

## [1.3.0] - 2023-12-29
### Changed
- Case insensitive deserialization for complex json queue messages

## [1.2.0] - 2023-12-19
### Added
- Easy way to setup required Azure Storage

## [1.1.0] - 2023-12-19
### Added
- Expose ParseMessageAsync as a public method
### Updated
- 🚮 Remove hardcoded version from NuGet package

## [1.0.0] - 2023-12-07
### Added
- Inital release.

