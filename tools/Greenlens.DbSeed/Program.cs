using Greenlens.Infrastructure.Seeders;

if (args.Length > 0 && args[0] == "import-boundary")
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine("Usage: import-boundary <province-geojson-path> <ward-geojson-path>");
        return 1;
    }

    await BoundaryGeometryImporterRunner.RunAsync(args[1], args[2]);
}
else
{
    await GamificationCatalogSeederRunner.RunAsync();
}

return 0;
