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
3. Choose the **small-x64-debug.pubxml** profile.
4. Click the **Publish** button in the top-right.
5. The .exe file will appear in the **/publish/small-x64/** folder in the root of this repo.

### Publishing a signed version of the app

_For projects maintainers only._

In order to publish a code-signed release of the app, you must take the following additional steps:

* You must add the **ValheimServerGUI.snk** file to the SolutionResources folder.
* You must have the Runeberry Software code signing certificate (.pfx) installed on your machine.
* You must have **SignTool.exe** in your system's PATH.
  * With the Visual Studio Installer, this is installed along with the Windows 10/11 SDKs. The install path looks like: `C:\Program Files (x86)\Windows Kits\10\bin\{version}\x86`

With all this in place, you should be able to run the **small-x64-release.pubxml** publish profile.

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

To run the serverless application locally (Visual Studio 2022):

1. Right-click the **ValheimServerGUI.Serverless** project in the Solution Explorer, and select "Set as Startup Project".
2. Press **F5** or click the play button to start debugging the application in IIS Express. This will launch a new browser window.
3. Using a REST client of your choice (such as [Postman](https://www.postman.com/downloads/) or just cURL), you can then query any API route using the base address shown in the console. For example:

```bash
curl --header "Content-Type: application/json" \
  --request POST \
  --data '{}' \
  http://localhost:44385/crash-report
```

### Publishing the Serverless API

_For project maintainers only._

The API is published to AWS using the [Serverless Application Model](https://aws.amazon.com/serverless/sam/), which essentially means that the deployment instructions are contained within a CloudFormation template in this repo - namely, [serverless.template](/ValheimServerGUI.Serverless/serverless.template).

Before publishing, ensure that the following Solution Resources are set up locally:
* ServerSecrets.Values.cs

The easiest way to publish the API is to install the [AWS Toolkit Extension](https://marketplace.visualstudio.com/items?itemName=AmazonWebServices.AWSToolkitforVisualStudio2017) for Visual Studio. After installing the extension, follow these steps:

1. Open the **AWS Explorer** within Visual Studio.
2. Click the button to "Add AWS Credentials File" and log in. You must have credentials with an IAM role that will allow you to publish to the Runeberry account.
3. Right-click the **ValheimServerGUI.Serverless** project and click "Publish to AWS Lambda".
4. Click **Publish**.