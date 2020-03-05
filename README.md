# GitVersionCore

Helpful MSBuild task for versioning your .NET Core projects.

I hope to add this as a NuGet package, soon.

## Using it

In your .csproj file, add the following:

```xml
<UsingTask TaskName="GitVersionCore" AssemblyFile="<path>/GitVersionCore.dll" />
<Target Name="AfterGenerateAssemblyInfo" AfterTargets="CoreGenerateAssemblyInfo">
    <GitVersionCore OutputFile="$(GeneratedAssemblyInfoFile)" Copyright="Copyright (C) 2017-{Year}" />
</Target>
```

## Contributing

Sure, send me a PR.
