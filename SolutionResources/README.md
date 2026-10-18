# Solution Resources

This folder contains code, configuration, and/or assets that are used across the Solution. Some files are considered "secret" and are not committed to source control. However, the solution is set up so that you **should not need any of these secret files** in order to do local development - only to publish the app.

In some cases, however, you may want to supply your own mock secret values for testing. Examples of these files are provided below for your reference.

### ClientSecrets.Values.cs

This is a... "clever" way of providing secret information to the client application at compile time. Use this partial static class to set the values of any properties in **ClientSecrets.cs**. Debug builds compile it only if it exists; Release builds require it. The release workflow generates it from **ClientSecrets.Values.cs.template**.

```csharp
namespace ValheimServerGUI.Properties
{
    public static partial class ClientSecrets
    {
        static ClientSecrets()
        {
            // Set the values of any properties in ClientSecrets.cs below
            RuneberryApiKeyHeader = "some-header-key";
            RuneberryClientApiKey = "some-api-key";
        }
    }
}
```
