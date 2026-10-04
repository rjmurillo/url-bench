import http from 'k6/http';
import { fail } from 'k6';
import { API_KEY, BASE_URL } from './config.js';

const API_KEY_HEADER = 'X-Api-Key';

function expected(statuses) {
  return http.expectedStatuses(...[].concat(statuses));
}

function params(name, statuses, extra = {}) {
  return {
    redirects: 0,
    responseCallback: expected(statuses),
    ...extra,
    tags: { name, phase: 'main', ...(extra.tags || {}) },
  };
}

// key: undefined uses the configured key, null omits the header, any string is sent as-is.
// body: undefined sends {"url": url}; a string is sent verbatim (for malformed-payload cases).
export function shorten(url, { key, body, statuses = 201, contentType = 'application/json', tags } = {}) {
  const headers = { 'Content-Type': contentType };
  const apiKey = key === undefined ? API_KEY : key;
  if (apiKey !== null) {
    headers[API_KEY_HEADER] = apiKey;
  }

  const payload = body === undefined ? JSON.stringify({ url }) : body;
  return http.post(`${BASE_URL}/api/links`, payload, params('POST /api/links', statuses, { headers, tags }));
}

export function resolve(slug, { statuses = 302, tags } = {}) {
  return http.get(`${BASE_URL}/${slug}`, params('GET /{slug}', statuses, { tags }));
}

export function get(path, { statuses = 200, name = `GET ${path}`, tags, baseUrl = BASE_URL } = {}) {
  return http.get(`${baseUrl}${path}`, params(name, statuses, { tags }));
}

export function json(response) {
  try {
    return response.json();
  } catch {
    return null;
  }
}

export function requireApiKey() {
  if (!API_KEY) {
    fail('API_KEY is not set. Run through `op run --env-file=.env.op -- k6 run ...` or pass -e API_KEY=...');
  }
}

// Scale-to-zero means the first request can take several seconds. Warm the app up outside the measured phase.
export function warmUp() {
  const response = http.get(`${BASE_URL}/health`, {
    timeout: '90s',
    responseCallback: expected(200),
    tags: { name: 'warmup /health', phase: 'warmup' },
  });
  if (response.status !== 200) {
    fail(`Warm-up failed: GET ${BASE_URL}/health returned ${response.status} ${response.error || ''}`);
  }
}
