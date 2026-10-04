// Smoke test: one pass over the critical path. Run after every deploy.
import { check, group } from 'k6';
import { get, json, requireApiKey, resolve, shorten, warmUp } from './lib/api.js';
import { backendEnabled, requireBackend, SENTINEL_SLUG, SENTINEL_URL, storedUrl } from './lib/backend.js';
import { HOME_URL, SHORT_BASE_URL, targetUrl } from './lib/config.js';

export const options = {
  vus: 1,
  iterations: 1,
  thresholds: {
    checks: ['rate==1'],
    http_req_failed: ['rate==0'],
    'http_req_duration{phase:main}': ['p(95)<1500'],
  },
};

export function setup() {
  requireApiKey();
  requireBackend();
  warmUp();
}

export default function () {
  group('health', () => {
    const health = get('/health');
    check(health, { 'health is 200 Healthy': (r) => r.status === 200 && r.body === 'Healthy' });
  });

  group('root redirects home', () => {
    const root = get('/', { statuses: 302, name: 'GET /' });
    check(root, { 'root redirects to HOME_URL': (r) => r.status === 302 && r.headers.Location === HOME_URL });
  });

  group('shorten and resolve', () => {
    const target = targetUrl('smoke', 'roundtrip');
    const created = shorten(target);
    const body = json(created);
    check(created, {
      'shorten returns 201': (r) => r.status === 201,
      'shortUrl uses the short base': () => body !== null && body.shortUrl === `${SHORT_BASE_URL}/${body.slug}`,
    });
    if (body === null) {
      return;
    }

    const redirect = resolve(body.slug);
    check(redirect, { 'slug redirects to target': (r) => r.status === 302 && r.headers.Location === target });

    if (backendEnabled) {
      check(storedUrl(body.slug), { 'backend stores slug -> target': (stored) => stored === target });
    }
  });

  if (backendEnabled) {
    group('backend health sentinel', () => {
      check(storedUrl(SENTINEL_SLUG), { 'health sentinel exists in storage': (stored) => stored === SENTINEL_URL });
    });
  }

  group('unknown slug', () => {
    check(resolve('k6Missing0', { statuses: 404 }), { 'unknown slug is 404': (r) => r.status === 404 });
  });
}
