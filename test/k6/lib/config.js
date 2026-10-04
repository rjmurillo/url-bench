// Runtime configuration, read from environment variables (`k6 run -e NAME=value` or the process environment).

function trimSlash(value) {
  return value.replace(/\/+$/, '');
}

// Where requests are sent. Override with the Container Apps FQDN to bypass the custom domain's DNS.
export const BASE_URL = trimSlash(__ENV.BASE_URL || 'https://go.richardmurillo.com');

// The base the service uses to compose short links (its Shortener:BaseUrl). It differs from BASE_URL when testing
// through the Container Apps FQDN or locally.
export const SHORT_BASE_URL = trimSlash(__ENV.SHORT_BASE_URL || 'https://go.richardmurillo.com');

export const HOME_URL = __ENV.HOME_URL || 'https://richardmurillo.com/';

// Parameters__apiKey is the name `op run --env-file=.env.op` exports, so the deploy env file works unchanged.
export const API_KEY = __ENV.API_KEY || __ENV.Parameters__apiKey || '';

// Backend verification. run.sh discovers the endpoint and fetches the token; BACKEND=off skips these checks
// (for example when testing against a local emulator).
export const BACKEND = (__ENV.BACKEND || 'on').toLowerCase();
export const STORAGE_TABLE_ENDPOINT = BACKEND === 'off' ? '' : trimSlash(__ENV.STORAGE_TABLE_ENDPOINT || '');
export const STORAGE_TOKEN = BACKEND === 'off' ? '' : __ENV.STORAGE_TOKEN || '';

// Every URL these suites shorten is fixed. Shortening is idempotent, so repeated runs reuse the same slugs instead
// of adding rows to the production links table.
export function targetUrl(suite, testCase) {
  return `https://example.com/?source=k6-${suite}&case=${testCase}`;
}
