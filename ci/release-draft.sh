#!/usr/bin/env bash
# Create a DRAFT release for TAG on GitHub and on the public Forgejo, each with every file in
# ASSET_DIR attached. Drafts notify no one; a maintainer writes the notes and publishes them by
# hand. A tag with a semver pre-release suffix (e.g. v3.0.0-rc.1) is marked as a pre-release,
# which also keeps it out of the in-app update check.
#
# Env: TAG, SHA (the tagged commit), ASSET_DIR, GITHUB_REPO + GH_TOKEN, FORGE_URL + FORGE_REPO + FORGE_TOKEN.
set -euo pipefail

die() { echo "ERROR: $*" >&2; exit 1; }

shopt -s nullglob
assets=("$ASSET_DIR"/*)
[ "${#assets[@]}" -gt 0 ] || die "no release assets found in ${ASSET_DIR}."

prerelease=false
[[ "$TAG" == *-* ]] && prerelease=true
payload="$(jq -n --arg tag "$TAG" --arg sha "$SHA" --argjson prerelease "$prerelease" \
  '{tag_name: $tag, target_commitish: $sha, name: $tag, body: "Release notes for \($tag).", draft: true, prerelease: $prerelease}')"
echo "Creating ${TAG} drafts (prerelease=${prerelease}) with: ${assets[*]##*/}"

# GitHub: create the draft, then upload each asset to the uploads host.
release="$(curl -sSf -X POST -H "Authorization: Bearer ${GH_TOKEN}" -H "Accept: application/vnd.github+json" \
  -d "$payload" "https://api.github.com/repos/${GITHUB_REPO}/releases")" \
  || die "could not create the GitHub draft release."
id="$(jq -r .id <<<"$release")"
for asset in "${assets[@]}"; do
  name="$(basename "$asset")"
  curl -sSf -o /dev/null -X POST -H "Authorization: Bearer ${GH_TOKEN}" -H "Content-Type: application/octet-stream" \
    --data-binary @"$asset" "https://uploads.github.com/repos/${GITHUB_REPO}/releases/${id}/assets?name=${name}" \
    || die "could not upload ${name} to the GitHub draft release ${id}."
done
echo "GitHub draft: $(jq -r .html_url <<<"$release")"

# Forgejo: create the draft, then upload each asset as a multipart attachment.
release="$(curl -sSf -X POST -H "Authorization: token ${FORGE_TOKEN}" -H "Content-Type: application/json" \
  -d "$payload" "${FORGE_URL}/api/v1/repos/${FORGE_REPO}/releases")" \
  || die "could not create the ${FORGE_URL} draft release."
id="$(jq -r .id <<<"$release")"
for asset in "${assets[@]}"; do
  name="$(basename "$asset")"
  curl -sSf -o /dev/null -X POST -H "Authorization: token ${FORGE_TOKEN}" \
    -F "attachment=@${asset}" "${FORGE_URL}/api/v1/repos/${FORGE_REPO}/releases/${id}/assets?name=${name}" \
    || die "could not upload ${name} to the ${FORGE_URL} draft release ${id}."
done
echo "Forgejo draft: $(jq -r .html_url <<<"$release")"
