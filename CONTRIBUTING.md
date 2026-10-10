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

## Backend

The server-side backend (crash/bug report handling, update check and external-IP check) lives in a **separate repository** and is **not needed** to build or run the client. The desktop client talks to it over HTTP through `CoreConstants.UrlRuneberryApi`; when the backend is unreachable the client stays fully functional (only report submission and those checks need connectivity). Player names come from the world save, not the backend.

## Solution Resources (Secrets)

The **SolutionResources** folder contains code, configuration, and/or assets that are used in multiple projects in the Solution. Some files are considered "secret" and are not committed to source control. However, the solution is set up so that you **should not need any of these secret files** in order to do local development - only to publish the app.

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

### Publishing a release

_For project maintainers only._

Releases are built and signed by CI, not locally. To cut one:

1. Set the new version in the `<Version>` element of **src/ValheimServerGUI.Core/ValheimServerGUI.Core.csproj** (a semantic version, e.g. `3.0.0` or `3.0.0-rc.1`).
2. Push a matching tag prefixed with `v`, such as `v3.0.0`. The workflow can also be run by hand for an existing tag.

The release workflow (`.forgejo/workflows/release.yml`) runs the test suite, builds the Windows `.exe`, Linux `.tar.gz`, and `.AppImage`, signs the `.exe` with the Runeberry Software code signing certificate, and creates **draft** releases on GitHub and the public forge with all three attached. A tag with a pre-release suffix (e.g. `v3.0.0-rc.1`) is marked as a pre-release.

A maintainer then edits the release notes and publishes the drafts.

The desktop client queries GitHub to find out if a new release is available. It only considers releases that are published, **not** marked as pre-releases, and have at least one asset, and it notifies the user when that release's version is greater than its own. Pre-releases never trigger an update notification. Users are notified the next time they open the desktop client, or within 24 hours (`UpdateCheckInterval` in [CoreConstants.cs](src/ValheimServerGUI.Core/CoreConstants.cs)).
