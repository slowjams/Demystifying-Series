## Demystifying Nuget
=====================

even though you don't include symbol, the underlying dll file, the assembly's Metadata contains a mapping for the source file

`SlowJamsAppLogger.dll` -> `Metadata` -> `Debug Directory` -> `CodeView` contains the mapping below:

`4fe85ca3-a1d1-4e43-97bf-5766ff5f4085` `C:\Users\Lxdth\source\repos\SlowJamsAppLogger\SlowJamsAppLogger\obj\Release\net8.0\SlowJamsAppLogger.pdb`

```bash
dotnet nuget push SlowJamsAppLogger.2.0.0.nupkg --api-key oxxx --source https://api.nuget.org/v3/index.json
```