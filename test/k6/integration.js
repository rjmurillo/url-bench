// Integration test: the full HTTP contract of the deployed service. Read-only except for a fixed set of links.
import http from 'k6/http';
import { check, group } from 'k6';
import { get, json, requireApiKey, resolve, shorten, warmUp } from './lib/api.js';
import { backendEnabled, requireBackend, SENTINEL_SLUG, SENTINEL_URL, storedUrl } from './lib/backend.js';
import { BASE_URL, HOME_URL, SHORT_BASE_URL, targetUrl } from './lib/config.js';

export const options = {
  vus: 1,
  iterations: 1,
  thresholds: {
    checks: ['rate==1'],
    http_req_failed: ['rate==0'],
  },
};

const SLUG_PATTERN = /^[A-Za-z0-9]{1,16}$/;

// Slugs the service never generates (they are 8 characters), in a valid format, so lookups must miss.
const UNKNOWN_SLUGS = ['k6Missing0', 'Zz9Zz9Zz9Zz9'];

const MALFORMED_SLUGS = {
  'contains a dash': 'has-dash',
  'longer than 16 characters': 'abcdefghijklmnopq',
  'non-ASCII character': '%C3%A9t%C3%A9',
  'contains a dot': 'abc.def',
};

function isValidationProblem(response) {
  const body = json(response);
  return response.status === 400
    && (response.headers['Content-Type'] || '').startsWith('application/problem+json')
    && body !== null
    && Array.isArray(body.errors && body.errors.url);
}

export function setup() {
  requireApiKey();
  requireBackend();
  warmUp();
}

export default function () {
  group('health endpoints', () => {
    check(get('/health'), { '/health is 200 Healthy': (r) => r.status === 200 && r.body === 'Healthy' });
    check(get('/alive'), { '/alive is 200 Healthy': (r) => r.status === 200 && r.body === 'Healthy' });
  });

  group('root', () => {
    check(get('/', { statuses: 302, name: 'GET /' }), {
      'root redirects to HOME_URL': (r) => r.status === 302 && r.headers.Location === HOME_URL,
    });
  });

  group('authentication', () => {
    const target = targetUrl('integration', 'auth');
    check(shorten(target, { key: null, statuses: 401 }), { 'missing key is 401': (r) => r.status === 401 });
    check(shorten(target, { key: 'wrong-key-0123456789abcdef', statuses: 401 }), {
      'wrong key is 401': (r) => r.status === 401,
    });
    check(shorten(target, { key: ' ', statuses: 401 }), { 'blank key is 401': (r) => r.status === 401 });
  });

  group('create, idempotency, and redirect', () => {
    const target = targetUrl('integration', 'roundtrip');
    const first = shorten(target);
    const body = json(first);
    check(first, {
      'create is 201': (r) => r.status === 201,
      'response is JSON': (r) => (r.headers['Content-Type'] || '').startsWith('application/json'),
      'slug is base62 and at most 16 characters': () => body !== null && SLUG_PATTERN.test(body.slug),
      'shortUrl is SHORT_BASE_URL/slug': () => body !== null && body.shortUrl === `${SHORT_BASE_URL}/${body.slug}`,
      'url echoes the target': () => body !== null && body.url === target,
      'Location header is the shortUrl': (r) => body !== null && r.headers.Location === body.shortUrl,
    });
    if (body === null) {
      return;
    }

    const second = json(shorten(target));
    check(second, { 'same URL returns the same slug': (b) => b !== null && b.slug === body.slug });

    check(resolve(body.slug), {
      'slug redirects (302) to the exact target': (r) => r.status === 302 && r.headers.Location === target,
    });

    if (backendEnabled) {
      check(storedUrl(body.slug), { 'backend entity holds the exact target': (stored) => stored === target });
    }
  });

  if (backendEnabled) {
    group('backend state', () => {
      check(storedUrl(SENTINEL_SLUG), {
        'health sentinel is seeded with the expected URL': (stored) => stored === SENTINEL_URL,
      });
      check(get(`/${SENTINEL_SLUG}`, { statuses: 404, name: 'GET /_health' }), {
        'sentinel slug is not publicly resolvable': (r) => r.status === 404,
      });
      for (const slug of UNKNOWN_SLUGS) {
        check(storedUrl(slug), { [`no backend entity for unknown slug ${slug}`]: (stored) => stored === null });
      }
    });
  }

  group('URL normalization', () => {
    const canonical = targetUrl('integration', 'normalize');
    const mixedCase = canonical.replace('https://example.com', 'HTTPS://EXAMPLE.COM');
    const a = json(shorten(canonical));
    const b = json(shorten(mixedCase));
    check(b, {
      'scheme and host are normalized': (body) => body !== null && body.url === canonical,
      'equivalent URLs share a slug': (body) => a !== null && body !== null && a.slug === body.slug,
    });
    if (backendEnabled && a !== null) {
      check(storedUrl(a.slug), { 'backend stores the normalized URL': (stored) => stored === canonical });
    }
  });

  group('http scheme is accepted', () => {
    const target = 'http://example.com/?source=k6-integration&case=http-scheme';
    const body = json(shorten(target));
    check(body, { 'http target is stored as-is': (b) => b !== null && b.url === target });
  });

  group('validation', () => {
    const invalidUrls = {
      'empty string': '',
      'not a URL': 'not a url',
      'relative path': '/relative/path',
      'ftp scheme': 'ftp://example.com/file',
      'javascript scheme': 'javascript:alert(1)',
      'mailto scheme': 'mailto:someone@example.com',
      'longer than 2048 characters': `https://example.com/${'a'.repeat(2100)}`,
      'points at the shortener': `${SHORT_BASE_URL}/abc`,
    };
    for (const [name, url] of Object.entries(invalidUrls)) {
      check(shorten(url, { statuses: 400 }), { [`${name} is a 400 validation problem`]: isValidationProblem });
    }

    check(shorten(null, { body: '{}', statuses: 400 }), { 'missing url field is a 400 validation problem': isValidationProblem });
    check(shorten(null, { body: '{"url":null}', statuses: 400 }), { 'null url is a 400 validation problem': isValidationProblem });
    check(shorten(null, { body: '{nope', statuses: 400 }), { 'malformed JSON is 400': (r) => r.status === 400 });
    check(shorten(null, { body: '[]', statuses: 400 }), { 'JSON array body is 400': (r) => r.status === 400 });
    check(shorten(targetUrl('integration', 'content-type'), { contentType: 'text/plain', statuses: 415 }), {
      'non-JSON content type is 415': (r) => r.status === 415,
    });
  });

  group('lookups that must miss', () => {
    for (const slug of UNKNOWN_SLUGS) {
      check(resolve(slug, { statuses: 404 }), { [`unknown slug ${slug} is 404`]: (r) => r.status === 404 });
    }
    for (const [name, slug] of Object.entries(MALFORMED_SLUGS)) {
      check(resolve(slug, { statuses: 404 }), { [`slug that ${name} is 404`]: (r) => r.status === 404 });
    }
  });

  group('method handling', () => {
    const response = http.get(`${BASE_URL}/api/links`, {
      redirects: 0,
      responseCallback: http.expectedStatuses(404, 405),
      tags: { name: 'GET /api/links', phase: 'main' },
    });
    check(response, { 'GET /api/links does not list links': (r) => r.status === 404 || r.status === 405 });
  });

  if (BASE_URL.startsWith('https://')) {
    group('HTTPS upgrade', () => {
      const plain = BASE_URL.replace(/^https:/, 'http:');
      const response = get('/k6Missing0', { statuses: 301, name: 'GET http:// upgrade', baseUrl: plain });
      check(response, {
        'plain HTTP redirects (301) to HTTPS': (r) => r.status === 301 && r.headers.Location === `${BASE_URL}/k6Missing0`,
      });
    });
  }
}
