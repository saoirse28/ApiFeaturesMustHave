namespace HealthCheckMetric.Options;

public class ConnectionStringOptions
{
    // This property name MUST match the key in your configuration file.
    // For example, if your appsettings.json has a section like:
    // "ConnectionStrings": {
    //   "Mssql": "YourConnectionStringHere"
    // }
    // Then the property name should be "Mssql".
    public required string Mssql { get; set; }

    // This property name MUST match the key in your configuration file.
    // For example, if your appsettings.json has a section like:
    // "ConnectionStrings": {
    //   "Redis": "YourConnectionStringHere"
    // }
    // Then the property name should be "Redis".
    public required string Redis { get; set; }
}
