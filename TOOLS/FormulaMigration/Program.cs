using FormulaMigration;

try
{
    return await FormulaMigrationCommand.RunAsync(args);
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Formula migration command failed: {exception.Message}");
    return 1;
}
