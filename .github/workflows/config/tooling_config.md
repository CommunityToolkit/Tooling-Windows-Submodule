
# Tooling Config Folder

This folder contains the minimum configuration required by the tooling module to be built and needs to be in the root of a repository using it.

These files are contained here in the `config` folder to be copied by the tooling `build.yml` file for self-validation within the tooling module CI pipeline.

They can also be used as a starting point for a custom repository that uses the `tooling` sub-module.

## Components

### Directory.Build.props

Contains properties like `RepositoryDirectory` and `ToolingDirectory` which are used within the property files of the tooling module to understand the proper path setup for dependencies and references.

### Directory.Build.targets

Contains `SlnGenSolutionItem` items that we wish to appear within the generated solution files.

### nuget.config

Contains any NuGet feeds required for dependent packages not found on NuGet.org at the moment (for instance the current unified port of the Community Toolkit Extensions used by testing).
