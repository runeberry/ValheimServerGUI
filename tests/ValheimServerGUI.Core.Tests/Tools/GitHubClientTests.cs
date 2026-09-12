using System;
using System.Collections.Generic;
using ValheimServerGUI.Tools;
using Xunit;

namespace ValheimServerGUI.Core.Tests.Tools
{
    /// <summary>
    /// Locks the release-selection contract that update notifications rely on (E41-E43). Notifications
    /// are only surfaced for the release <see cref="GitHubClient.SelectLatestRelease"/> returns, and it
    /// excludes releases flagged as GitHub pre-releases. That is the reason a release candidate must
    /// be published as an ordinary (non-pre-release) GitHub release for existing users to be notified.
    /// </summary>
    public class GitHubClientTests
    {
        private static GitHubRelease Release(
            string tag,
            DateTime publishedAt,
            bool prerelease = false,
            bool draft = false,
            bool hasAsset = true)
        {
            return new GitHubRelease
            {
                TagName = tag,
                PublishedAt = publishedAt,
                Prerelease = prerelease,
                Draft = draft,
                Assets = hasAsset ? new object[] { new object() } : Array.Empty<object>(),
            };
        }

        // E41: a release-candidate version published as an ORDINARY release (Prerelease = false) is
        // selected, while a newer build flagged as a GitHub pre-release is ignored.
        [Fact]
        public void SelectLatestRelease_SelectsPrereleaseVersionPublishedAsOrdinaryRelease_IgnoringGitHubPrereleaseFlag()
        {
            var releases = new[]
            {
                Release("v2.3.1", new DateTime(2025, 1, 1)),
                Release("v2.4.0-rc.1", new DateTime(2025, 2, 1), prerelease: false),
                Release("v2.4.0-rc.2", new DateTime(2025, 3, 1), prerelease: true),
            };

            var latest = GitHubClient.SelectLatestRelease(releases);

            Assert.Equal("v2.4.0-rc.1", latest?.TagName);
        }

        // E42: among eligible releases the most recently published wins; drafts and asset-less
        // releases are skipped even when they are newer.
        [Fact]
        public void SelectLatestRelease_PicksMostRecentEligible_SkippingDraftsAndAssetless()
        {
            var releases = new[]
            {
                Release("v2.4.0", new DateTime(2025, 5, 1)),
                Release("v2.4.1", new DateTime(2025, 6, 1), draft: true),
                Release("v2.4.2", new DateTime(2025, 7, 1), hasAsset: false),
                Release("v2.4.0-hotfix", new DateTime(2025, 6, 15)),
            };

            var latest = GitHubClient.SelectLatestRelease(releases);

            Assert.Equal("v2.4.0-hotfix", latest?.TagName);
        }

        // E43: no qualifying release -> null (treated as up-to-date, no error spam).
        [Fact]
        public void SelectLatestRelease_ReturnsNull_WhenNoReleaseQualifies()
        {
            var releases = new[]
            {
                Release("v2.4.0", new DateTime(2025, 1, 1), draft: true),
                Release("v2.4.1", new DateTime(2025, 2, 1), hasAsset: false),
                Release("v2.4.2", new DateTime(2025, 3, 1), prerelease: true),
            };

            Assert.Null(GitHubClient.SelectLatestRelease(releases));
        }

        [Fact]
        public void SelectLatestRelease_ReturnsNull_WhenNoReleasesExist()
        {
            Assert.Null(GitHubClient.SelectLatestRelease(new List<GitHubRelease>()));
        }
    }
}
