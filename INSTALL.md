Project Setup
=============

Here are steps you need to do to setup environment to be able to develop. You need following software installed:

- Visual Studio 2019 (https://www.visualstudio.com/downloads/download-visual-studio-vs.aspx) or JetBrains Rider (https://www.jetbrains.com/rider/)
- .NET Core 3.1 (https://www.microsoft.com/net/core)

Code Style Setup
----------------

We are using StyleCop.Analyzers project for extended code style check. It should be installed for every project in solution:

```
Install-Package StyleCop.Analyzers
```

To install StyleCop analyzers for all projects within solution run following command in package manager console:

```
Get-Project -All | Install-Package StyleCop.Analyzers
```

Publish
-------

Use following command to publish the app, it will create framework independed executables.

```
dotnet publish -r linux-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true -o ../../publish
```

You can use following runtime identifiers:

- `linux-x64` for Linux.
- `win-x64` for Windows.
