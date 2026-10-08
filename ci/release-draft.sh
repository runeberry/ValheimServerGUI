#!/usr/bin/env bash
# Create a DRAFT release for TAG on GitHub and on the public Forgejo, each with ZIP attached.
# Drafts notify no one; a maintainer writes the notes and publishes them by hand.
#
# Env: TAG, SHA (the tagged commit), ZIP, GITHUB_REPO + GH_TOKEN, FORGE_URL + FORGE_REPO + FORGE_TOKEN.
set -euo pipefail

die() { echo "ERROR: $*" >&2; exit 1; }

[ -f "$ZIP" ] || die "release asset ${ZIP} not found."
name="$(basename "$ZIP")"
payload="$(jq -n --arg tag "$TAG" --arg sha "$SHA" \
  '{tag_name: $tag, target_commitish: $sha, name: $tag, body: "Release notes for \($tag).", draft: true, prerelease: false}')"

# GitHub: create the draft, then upload to the uploads host.
release="$(curl -sSf -X POST -H "Authorization: Bearer ${GH_TOKEN}" -H "Accept: application/vnd.github+json" \
  -d "$payload" "https://api.github.com/repos/${GITHUB_REPO}/releases")" \
  || die "could not create the GitHub draft release."
id="$(jq -r .id <<<"$release")"
curl -sSf -o /dev/null -X POST -H "Authorization: Bearer ${GH_TOKEN}" -H "Content-Type: application/zip" \
  --data-binary @"$ZIP" "https://uploads.github.com/repos/${GITHUB_REPO}/releases/${id}/assets?name=${name}" \
  || die "could not upload ${name} to the GitHub draft release ${id}."
echo "GitHub draft: $(jq -r .html_url <<<"$release")"

# Forgejo: create the draft, then upload as a multipart attachment.
release="$(curl -sSf -X POST -H "Authorization: token ${FORGE_TOKEN}" -H "Content-Type: application/json" \
  -d "$payload" "${FORGE_URL}/api/v1/repos/${FORGE_REPO}/releases")" \
  || die "could not create the ${FORGE_URL} draft release."
id="$(jq -r .id <<<"$release")"
curl -sSf -o /dev/null -X POST -H "Authorization: token ${FORGE_TOKEN}" \
  -F "attachment=@${ZIP}" "${FORGE_URL}/api/v1/repos/${FORGE_REPO}/releases/${id}/assets?name=${name}" \
  || die "could not upload ${name} to the ${FORGE_URL} draft release ${id}."
echo "Forgejo draft: $(jq -r .html_url <<<"$release")"
