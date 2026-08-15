#!/usr/bin/env bash
set -euo pipefail

VERSION="v0.11.0.3"
COMMIT="cb6e0966ac305202c47f1d1a81c105966e29da96"
ARCHIVE_URL="https://github.com/ramokz/phantom-camera/archive/refs/tags/${VERSION}.tar.gz"
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DESTINATION="${REPO_ROOT}/addons/phantom_camera"
TEMP_ROOT="$(mktemp -d)"
trap 'rm -rf "${TEMP_ROOT}"' EXIT

printf 'Downloading Phantom Camera %s (%s)...\n' "${VERSION}" "${COMMIT}"
curl --fail --location --silent --show-error "${ARCHIVE_URL}" -o "${TEMP_ROOT}/phantom-camera.tar.gz"
mkdir -p "${TEMP_ROOT}/extract"
tar -xzf "${TEMP_ROOT}/phantom-camera.tar.gz" -C "${TEMP_ROOT}/extract"

PLUGIN_CONFIG="$(find "${TEMP_ROOT}/extract" -type f -path '*/addons/phantom_camera/plugin.cfg' -print -quit)"
if [[ -z "${PLUGIN_CONFIG}" ]]; then
  echo 'Could not locate addons/phantom_camera/plugin.cfg in the pinned archive.' >&2
  exit 1
fi

SOURCE="$(dirname "${PLUGIN_CONFIG}")"
mkdir -p "$(dirname "${DESTINATION}")"
rm -rf "${DESTINATION}"
cp -R "${SOURCE}" "${DESTINATION}"
printf '%s\n%s\n' "${VERSION}" "${COMMIT}" > "${DESTINATION}/.lime-version"

printf 'Phantom Camera materialized at addons/phantom_camera.\n'
