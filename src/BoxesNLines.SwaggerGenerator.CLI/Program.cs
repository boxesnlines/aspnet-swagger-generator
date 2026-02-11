using BoxesNLines.SwaggerGenerator;

ApiAnalyzer analyzer = new ApiAnalyzer();
WebApi result = analyzer.AnalyzeProject("../../../../../test/TestWebApi/TestWebApi.csproj");
Console.WriteLine(result.Swagger);
