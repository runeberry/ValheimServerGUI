# Contributing

Here's the recommended workflow for contributing code back to ValheimServerGUI:

1. Pick an open [issue](https://github.com/runeberry/ValheimServerGUI/issues) that you'd like to make changes for. If there isn't an open issue for your change, [create a new one](https://github.com/runeberry/ValheimServerGUI/issues/new).
1. Read the **Developer's Guide** below to learn how to run the application locally.
2. Fork this repository & make your code changes.
3. Create a [pull request](https://github.com/runeberry/ValheimServerGUI/pulls) from your fork to `main`. Link your issue to the pull request - you can do this by simply putting "Resolves #{your_issue_number}" in the PR description.

# Developer's Guide

This project was developed using Visual Studio 2019 on Windows 10. The instructions below will assume you are working in a similar setup, unless noted otherwise. You can download the Community Edition of Visual Studio for free [here](https://visualstudio.microsoft.com/downloads/).

## Solution Projects

* **ValheimServerGUI** - The main desktop client application
* **ValheimServerGUI.Controls** - Common user controls used in the desktop client. These contain no Valheim-specific code.
* **ValheimServerGUI.Tools** - Common utilities used in the desktop client. These contain no Valheim-specific code and no references to Windows Forms.
* **ValheimServerGUI.Serverless** - A small REST API built specifically for the desktop client using the [AWS Serverless Application Model](https://aws.amazon.com/serverless/sam/).

## Solution Resources (Secrets)

The **SolutionResources** folder contains code, configuration, and/or assets that are used in multiple projects in the Solution. Some files are considered "secret" and are not committed to source control. However, the solution is set up so that you **should not need any of these secret files** in order to do local development - only to publish the app or Serverless code.

In some cases, however, you may want to supply your own mock secret values for testing. Read more about specific SolutionResources files [here](/SolutionResources/README.md).

## ValheimServerGUI - Desktop Application

Running the desktop client locally is fairly straightforward:

1. Select the **ValheimServerGUI** project (or any file in that project) in the Solution Explorer in Visual Studio.
2. Press **F5** or click the play button to start debugging the application.

### Publishing the app

This project uses a **Publish Profile** (.pubxml) file to store configuration for publishing the desktop client. All you need to do is right-click the **ValheimServerGUI** project in Visual studio and click "Publish".

1. Right-click the **ValheimServerGUI** project in the Solution Explorer in Visual Studio.
2. Click **Publish...**
3. Choose the **small-x64.pubxml** profile.
4. Click the **Publish** button in the top-right.
5. The .exe file will appear in the **/publish** folder in the root of this repo.

If you are publishing a Release configuration of this project, you will need a copy of the **ValheimServerGUI.snk** file in your SolutionResources folder. However, you can get around this by simply changing the Configuration to "Debug" for testing.

### Creating a new release

_For project maintainers only._

In order to bump the desktop client's application version, simply change the number in the `<Version>` section of **ValheimServerGUI.csproj**, then publish the app using the steps outlined above.

The desktop client queries GitHub to find out if a new release is available. Follow these instructions to ensure that users will be notified about a client update.

1. Start creating a new release [here](https://github.com/runeberry/ValheimServerGUI/releases/new). Set the release title and description to whatever you see fit.
2. Set the release tag version to a semantic version prefixed with `v`, such as `v1.2.3`.
   * This semver should correspond to the `<Version>` set in the .csproj file.
   * The semver on GitHub must be greater than the client's current version trigger an update notification.
3. Attach a .zip file containing just the `ValheimServerGUI.exe` file published from the previous section. The release must contain an asset in order to trigger an update notification.
4. Ensure the Pre-release button is **NOT** checked. Pre-releases will not trigger an update notification.
5. Click **Publish release**.

Users will be notified that an update is available the next time they open the desktop client, or within 24 hours. This timeframe is configured in the project's [Resource file](ValheimServerGUI/Properties/Resources.resx).

## ValheimServerGUI.Serverless - REST API

You only need to run the Serverless application locally if you're making changes to the REST API that you want to test. If you're just making changes to the desktop client, you can skip this section.

To run the serverless application locally:

1. Select the **ValheimServerGUI.Serverless** project (or any file in that project) in the Solution Explorer in Visual Studio.
2. Press **F5** or click the play button to start debugging the application. This will launch a console window.
3. Using a REST client of your choice (such as [Postman](https://www.postman.com/downloads/) or just cURL), you can then query any API route using the base address shown in the console. For example:

```bash
curl --header "Content-Type: application/json" \
  --request POST \
  --data '{}' \
  http://localhost:5000/crash-report
```

### Publishing the Serverless API

_For project maintainers only._

The API is published to AWS using the [Serverless Application Model](https://aws.amazon.com/serverless/sam/), which essentially means that the deployment instructions are contained within a CloudFormation template in this repo - namely, [serverless.template](/ValheimServerGUI.Serverless/serverless.template).

Before publishing, ensure that the following Solution Resources are set up locally:
* appsettings.secret.json
* Secrets.Values.cs

The easiest way to publish the API is to install the [AWS Toolkit Extension](https://marketplace.visualstudio.com/items?itemName=AmazonWebServices.AWSToolkitforVisualStudio2017) for Visual Studio. After installing the extension, follow these steps:

1. Open the **AWS Explorer** within Visual Studio.
2. Click the button to "Add AWS Credentials File" and log in. You must have credentials with an IAM role that will allow you to publish to the Runeberry account.
3. Right-click the **ValheimServerGUI.Serverless** project and click "Publish to AWS Lambda".
4. Click **Publish**.