#!/usr/bin/env bash
# Gate a release before anything is built or signed: the tag must exist, must match the app
# version, and must not already have a release (draft or published) on any release host.
#
# Env: TAG, GITHUB_REPO + GH_TOKEN, FORGE_URL + FORGE_REPO + FORGE_TOKEN.
# Writes tag=, version=, and sha= (the tagged commit) to $GITHUB_OUTPUT.
set -euo pipefail

die() { echo "ERROR: $*" >&2; exit 1; }

[[ "$TAG" == v* ]] || die "tag '${TAG}' must start with 'v' (e.g. v2.4.0)."
version="${TAG#v}"

git rev-parse -q --verify "refs/tags/${TAG}" >/dev/null || die "tag ${TAG} does not exist."
csproj_version="$(git show "${TAG}:ValheimServerGUI/ValheimServerGUI.csproj" | grep -oP '(?<=<Version>)[^<]+')"
[ "$csproj_version" = "$version" ] \
  || die "tag ${TAG} does not match <Version>${csproj_version}</Version> in ValheimServerGUI.csproj at that tag."

# Lists every release page by page and prints the tag names. Drafts are included because
# the tokens can write to the repos (the by-tag lookup endpoints do not return drafts).
list_github_tags() {
  local page=1 body
  while :; do
    body="$(curl -sSf -H "Authorization: Bearer ${GH_TOKEN}" -H "Accept: application/vnd.github+json" \
      "https://api.github.com/repos/${GITHUB_REPO}/releases?per_page=100&page=${page}")"
    [ "$(jq length <<<"$body")" -gt 0 ] || break
    jq -r '.[].tag_name' <<<"$body"
    page=$((page + 1))
  done
}

list_forge_tags() {
  local page=1 body
  while :; do
    body="$(curl -sSf -H "Authorization: token ${FORGE_TOKEN}" \
      "${FORGE_URL}/api/v1/repos/${FORGE_REPO}/releases?limit=50&page=${page}")"
    [ "$(jq length <<<"$body")" -gt 0 ] || break
    jq -r '.[].tag_name' <<<"$body"
    page=$((page + 1))
  done
}

conflicts=()
github_tags="$(list_github_tags)" || die "could not list releases on GitHub ${GITHUB_REPO}."
grep -qxF "$TAG" <<<"$github_tags" && conflicts+=("GitHub ${GITHUB_REPO}")
forge_tags="$(list_forge_tags)" || die "could not list releases on ${FORGE_URL}/${FORGE_REPO}."
grep -qxF "$TAG" <<<"$forge_tags" && conflicts+=("${FORGE_URL}/${FORGE_REPO}")
[ "${#conflicts[@]}" -eq 0 ] || die "a release for ${TAG} already exists on: ${conflicts[*]}. Delete it first to re-release."

echo "Releasing ${TAG} (version ${version}); no existing release on GitHub or ${FORGE_URL}."
{
  echo "tag=${TAG}"
  echo "version=${version}"
  echo "sha=$(git rev-list -n 1 "refs/tags/${TAG}")"
} >> "$GITHUB_OUTPUT"
