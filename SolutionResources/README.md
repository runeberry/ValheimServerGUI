# Solution Resources

This folder contains code, configuration, and/or assets that are used in multiple projects in the Solution. Some files are considered "secret" and are not committed to source control. However, the solution is set up so that you **should not need any of these secret files** in order to do local development - only to publish the app or Serverless code.

In some cases, however, you may want to supply your own mock secret values for testing. Examples of these files are provided below for your reference.

### ValheimServerGUI.snk

This is only needed when publishing the desktop client application in the Release configuration. If you need to publish the application locally for some reason, simply change the Publish Profile (.pubxml) to publish to Debug configuration temporarily.

### Secrets.Values.cs

This is a... "clever" way of providing secret information to both the client and Serverless applications at compile time. Use this partial static class to set the values of any properties in **Secrets.cs**.

```csharp
namespace ValheimServerGUI.Properties
{
  public static partial class Secrets
  {
    static Secrets()
    {
      // Set the values of any properties in Secrets.cs below
      RuneberryApiKeyHeader = "some-header-key";
    }
  }
}
```

### appsettings.secret.json

Configuration values for the Serverless application, both when running locally and deployed. This file **must** exist when deploying the Serverless application, else it will fail to start.

```jsonc
{
  "SteamApiKey": "", // Not yet used, but planned for an upcoming update
}
```

### appsettings.local.json

Configuration values for the Serverless application only when running locally. You can set AWS Lambda environment variables here to imitate running in a cloud environment.

```jsonc
{
  // These are required to access other AWS services from within the API
  "AWS_ACCESS_KEY_ID": "",
  "AWS_SECRET_ACCESS_KEY": ""
}
```