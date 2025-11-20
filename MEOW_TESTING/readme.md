

https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-code-coverage?tabs=linux

```shell
dotnet test --collect:"XPlat Code Coverage"
```

```shell
dotnet tool install -g dotnet-reportgenerator-globaltool
```
```shell
reportgenerator -reports:"TestResults/5d3ec170-1b08-4dc1-b238-5886c7c56af7/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
```

