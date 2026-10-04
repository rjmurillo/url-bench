#!/usr/bin/env bash
# Runs a k6 suite against the deployed shortener with secrets and backend access wired up.
#
#   test/k6/run.sh smoke|integration|load [extra k6 args...]
#
# - API key: resolved by `op run` from .env.op (1Password desktop integration must be unlocked).
# - Backend reads: a Microsoft Entra token for Azure Storage from the signed-in Azure CLI user, who needs
#   "Storage Table Data Reader" on the storage account. Set BACKEND=off to skip.
# - BASE_URL defaults to the custom domain. Set it to the Container Apps FQDN if local DNS blocks the domain.
set -euo pipefail

suite="${1:?usage: test/k6/run.sh smoke|integration|load [k6 args...]}"
shift

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
script="${repo_root}/test/k6/${suite}.js"
[[ -f "${script}" ]] || { echo "No such suite: ${script}" >&2; exit 2; }

resource_group="${AZURE_RESOURCE_GROUP:-rg-go-richardmurillo}"

if [[ "${BACKEND:-on}" != "off" ]]; then
  if [[ -z "${STORAGE_TABLE_ENDPOINT:-}" ]]; then
    STORAGE_TABLE_ENDPOINT="$(az storage account list --resource-group "${resource_group}" \
      --query "[0].primaryEndpoints.table" --output tsv)"
  fi
  STORAGE_TOKEN="$(az account get-access-token --resource https://storage.azure.com \
    --query accessToken --output tsv)"
  export STORAGE_TABLE_ENDPOINT STORAGE_TOKEN
fi

cd "${repo_root}"
exec op run --env-file=.env.op -- k6 run "$@" "${script}"
