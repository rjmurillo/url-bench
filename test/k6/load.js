// Load test: sustained redirect traffic against one replica. Reads only after a single setup write.
// Tune with -e LOAD_VUS=5 -e LOAD_DURATION=1m. Keep it modest: production runs at most one 0.25 vCPU replica.
import { check, sleep } from 'k6';
import { fail } from 'k6';
import { json, requireApiKey, resolve, shorten, warmUp } from './lib/api.js';
import { backendEnabled, requireBackend, storedUrl } from './lib/backend.js';
import { targetUrl } from './lib/config.js';

const VUS = Number(__ENV.LOAD_VUS || 5);
const DURATION = __ENV.LOAD_DURATION || '1m';

export const options = {
  scenarios: {
    redirects: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '15s', target: VUS },
        { duration: DURATION, target: VUS },
        { duration: '10s', target: 0 },
      ],
      gracefulRampDown: '5s',
    },
  },
  thresholds: {
    checks: ['rate>0.99'],
    'http_req_failed{phase:main}': ['rate<0.01'],
    'http_req_duration{phase:main}': ['p(95)<500', 'p(99)<1000'],
  },
};

export function setup() {
  requireApiKey();
  requireBackend();
  warmUp();
  const target = targetUrl('load', 'redirect');
  const body = json(shorten(target, { tags: { phase: 'setup' } }));
  if (body === null || !body.slug) {
    fail('Setup could not create the load-test link.');
  }
  if (backendEnabled && storedUrl(body.slug) !== target) {
    fail(`Backend does not hold ${body.slug} -> ${target}.`);
  }

  return { slug: body.slug, target };
}

export default function (data) {
  const response = resolve(data.slug);
  check(response, { 'redirects to target': (r) => r.status === 302 && r.headers.Location === data.target });
  sleep(0.2);
}
