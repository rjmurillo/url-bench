// Direct reads from the Azure Table Storage backend, so tests prove data landed where the service claims.
// Authenticates with a Microsoft Entra token for https://storage.azure.com (shared keys are disabled on the account).
import http from 'k6/http';
import { fail } from 'k6';
import { BACKEND, STORAGE_TABLE_ENDPOINT, STORAGE_TOKEN } from './config.js';

const TABLE = 'links';

// Must stay in sync with LinkStoreHealthCheck in src/UrlShortener.Web.
export const SENTINEL_SLUG = '_health';
export const SENTINEL_URL = 'https://richardmurillo.com/?source=health-sentinel';

export const backendEnabled = STORAGE_TABLE_ENDPOINT !== '' && STORAGE_TOKEN !== '';

export function requireBackend() {
  if (BACKEND !== 'off' && !backendEnabled) {
    fail('STORAGE_TABLE_ENDPOINT and STORAGE_TOKEN are not set. Run through test/k6/run.sh, or set BACKEND=off to skip backend checks.');
  }
}

function quote(value) {
  return encodeURIComponent(value.replace(/'/g, "''"));
}

// Returns the stored URL for a slug, or null when no entity exists. Fails the test on any other response.
export function storedUrl(slug) {
  const url = `${STORAGE_TABLE_ENDPOINT}/${TABLE}(PartitionKey='${quote(slug)}',RowKey='')?$select=Url`;
  const response = http.get(url, {
    headers: {
      Authorization: `Bearer ${STORAGE_TOKEN}`,
      Accept: 'application/json;odata=nometadata',
      'x-ms-version': '2020-12-06',
      'x-ms-date': new Date().toUTCString(),
    },
    responseCallback: http.expectedStatuses(200, 404),
    tags: { name: 'backend GET links entity', phase: 'backend' },
  });

  if (response.status === 404) {
    return null;
  }
  if (response.status !== 200) {
    fail(`Backend read for '${slug}' returned ${response.status}: ${response.body}`);
  }
  return response.json('Url');
}
