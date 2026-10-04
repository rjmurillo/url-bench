# Troubleshooting: go.richardmurillo.com URL shortener

Find your symptom in the index. Each entry has five parts: symptom, confirm (read-only commands), cause, fix, verify. Always run the confirm step before you change anything. Read the [safety rules](README.md#safety-rules-for-agents) first. Never print secrets.

Run commands from the repo root:

```sh
cd /home/richard/src/GitHub/rjmurillo/url-bench
```

## Start here: 60-second triage

Run these four read-only commands in order. Stop at the first one that looks wrong and go to the linked section.

```sh
# 1. Public DNS (bypasses local DNS filters)
curl -s -H 'accept: application/dns-json' 'https://cloudflare-dns.com/dns-query?name=go.richardmurillo.com&type=A'
```

Expected: `Answer` contains a CNAME to `web.calmsand-3db4f9d7.westus2.azurecontainerapps.io.` and an A record `4.149.148.23`. If not, go to [A1](#a1-public-dns-has-no-record-or-the-wrong-record).

```sh
# 2. Service reachable at the public IP, ignoring local DNS
curl -s -o /dev/null -w "%{http_code} %{time_total}s\n" --max-time 60 --resolve go.richardmurillo.com:443:4.149.148.23 https://go.richardmurillo.com/health
```

Expected: `200`. If `000`, go to [C6](#c6-timeout-or-very-slow-response). If a TLS error, go to [A3](#a3-https-certificate-error). If `503`, go to [C5](#c5-500-502-or-503).

```sh
# 3. Same request using your normal DNS
curl -s -o /dev/null -w "%{http_code}\n" --max-time 60 https://go.richardmurillo.com/health
```

Expected: `200`. If step 2 worked but this fails, the problem is local DNS. Go to [A2](#a2-name-resolves-to-0000-or-).

```sh
# 4. Azure state
az containerapp revision list -g rg-go-richardmurillo -n web -o table
```

Expected: one active revision, `Healthy`, traffic 100. If not, go to [C5](#c5-500-502-or-503).

If all four pass, the service is up. The problem is in the specific request. Go to [C. HTTP status codes](#c-http-status-codes).

## Symptom index

| Symptom | Section |
| --- | --- |
| Public DNS has no record for `go` or `asuid.go` | [A1](#a1-public-dns-has-no-record-or-the-wrong-record) |
| `go.richardmurillo.com` resolves to `0.0.0.0` or `::` | [A2](#a2-name-resolves-to-0000-or-) |
| Browser or curl shows a certificate error | [A3](#a3-https-certificate-error) |
| Custom domain binding shows `Disabled` | [A3](#a3-https-certificate-error) |
| Deploy fails: `Text file busy: '/home/richard/.azure/bin/bicep'` | [B1](#b1-bicep-text-file-busy) |
| Deploy fails: `InvalidCustomHostNameValidation` | [B2](#b2-invalidcustomhostnamevalidation) |
| Deploy fails: `Microsoft.App` not registered | [B3](#b3-microsoftapp-resource-provider-not-registered) |
| Deploy fails: Azure login or subscription errors | [B4](#b4-azure-login-errors) |
| Deploy fails: missing `apiKey` parameter, or `op run` errors | [B5](#b5-op-run-or-missing-apikey-parameter) |
| `op whoami` says "account is not signed in" | [B6](#b6-op-whoami-says-account-is-not-signed-in) |
| Deploy fails with another error | [B7](#b7-other-deploy-errors) |
| `POST /api/links` returns 401 | [C1](#c1-401-unauthorized) |
| `POST /api/links` returns 400 | [C2](#c2-400-bad-request) |
| Short link returns 404 | [C3](#c3-404-not-found-on-a-short-link) |
| Every path returns a 404 HTML page from Azure | [C4](#c4-every-request-returns-an-azure-404-page) |
| 500, 502, or 503 | [C5](#c5-500-502-or-503) |
| First request takes about 8 seconds | [C6](#c6-timeout-or-very-slow-response) |
| Requests time out | [C6](#c6-timeout-or-very-slow-response) |
| Local run fails | [D1](#d1-local-run-fails) |

## A. Cannot connect or DNS does not resolve

### A1. Public DNS has no record or the wrong record

Symptom: DNS over HTTPS returns no `Answer`, or a different target, for `go.richardmurillo.com` or `asuid.go.richardmurillo.com`.

Confirm:

```sh
curl -s -H 'accept: application/dns-json' 'https://cloudflare-dns.com/dns-query?name=go.richardmurillo.com&type=CNAME'
curl -s -H 'accept: application/dns-json' 'https://cloudflare-dns.com/dns-query?name=asuid.go.richardmurillo.com&type=TXT'
```

Expected values:

- `go.richardmurillo.com` CNAME `web.calmsand-3db4f9d7.westus2.azurecontainerapps.io.`
- `asuid.go.richardmurillo.com` TXT `"A8F57878C0EC92FC4107CA2435EC583C8E98D7A04DD58FE799163EDA82997E9D"`

If the A query returns Cloudflare IPs (`104.x`, `172.64.x` to `172.71.x`) instead of a CNAME to Azure, the record is proxied.

Then read the records in Cloudflare (step 1 of [runbook: Recreate or fix the DNS records](runbook.md#recreate-or-fix-the-dns-records)).

Cause: a record was deleted, changed, or set to proxied. Or the Container Apps environment was recreated and its default domain or verification ID changed.

Fix: follow [runbook: Recreate or fix the DNS records](runbook.md#recreate-or-fix-the-dns-records). Needs human approval. Keep `proxied: false` on the CNAME.

Verify: rerun the confirm commands, then `curl -s -o /dev/null -w "%{http_code}\n" https://go.richardmurillo.com/health` returns `200`.

### A2. Name resolves to 0.0.0.0 or ::

Symptom: `dig go.richardmurillo.com`, `dig @1.1.1.1 go.richardmurillo.com`, or `dig @8.8.8.8 go.richardmurillo.com` returns `0.0.0.0` (A) or `::` (AAAA), often with TTL 5. Browsers fail to connect. This has happened on Richard's home network.

Confirm:

```sh
dig +short @1.1.1.1 go.richardmurillo.com A
curl -s -H 'accept: application/dns-json' 'https://cloudflare-dns.com/dns-query?name=go.richardmurillo.com&type=A'
```

If `dig` shows `0.0.0.0` but DNS over HTTPS shows the CNAME and `4.149.148.23`, public DNS is correct. Something on the local network is rewriting DNS answers.

Then test the service directly, using the IP from DNS over HTTPS:

```sh
curl -s -o /dev/null -w "%{http_code}\n" --resolve go.richardmurillo.com:443:4.149.148.23 https://go.richardmurillo.com/health
```

Expected: `200`. The service is fine.

Cause: the home router or DNS filter at `192.168.1.1` intercepts all port 53 traffic, even traffic addressed to `1.1.1.1` or `8.8.8.8`, and blocks the name. Newly registered or low-reputation names are often blocked by such filters.

Fix: this is not a service problem. Do not change Azure or Cloudflare. Tell the human to allowlist `go.richardmurillo.com` in the home DNS filter (router admin at `192.168.1.1`). As a temporary workaround, use `curl --resolve` as shown above, or test from another network.

Verify: `dig +short @1.1.1.1 go.richardmurillo.com A` returns `web.calmsand-3db4f9d7.westus2.azurecontainerapps.io.` and `4.149.148.23`.

Note: the environment IP `4.149.148.23` can change if Azure recreates the environment. Always take the IP from the DNS over HTTPS answer.

### A3. HTTPS certificate error

Symptom: curl prints `SSL: no alternative certificate subject name matches`, `SSL certificate problem`, or a TLS handshake failure. A browser shows a certificate warning. Or the custom domain binding shows `Disabled`.

Confirm:

```sh
echo | openssl s_client -connect 4.149.148.23:443 -servername go.richardmurillo.com 2>/dev/null | openssl x509 -noout -subject -issuer -enddate
az containerapp show -g rg-go-richardmurillo -n web --query "properties.configuration.ingress.customDomains" -o json
az containerapp env certificate list -g rg-go-richardmurillo -n aca6lwgsugxgoxds --managed-certificates-only -o table
grep -n '"certificateName"' src/UrlShortener.AppHost/appsettings.json
```

Healthy: subject `CN=go.richardmurillo.com`, issuer GeoTrust (DigiCert), end date in the future, `bindingType` `SniEnabled`, `certificateId` ending in the certificate name, and the same name in `appsettings.json`.

Causes and fixes:

| What you see | Cause | Fix |
| --- | --- | --- |
| `bindingType` is `Disabled`, certificate list has a certificate | Deployed with an empty or wrong `certificateName`. | Set `Parameters:certificateName` in `appsettings.json` to the name in the list, then [deploy](runbook.md#deploy-to-production). |
| `bindingType` is `Disabled`, certificate list is empty | Certificate never issued, or was deleted. | [runbook: Re-bind the custom domain certificate](runbook.md#re-bind-the-custom-domain-certificate). |
| No custom domain at all | Hostname was removed. | Check DNS ([A1](#a1-public-dns-has-no-record-or-the-wrong-record)), then [deploy](runbook.md#deploy-to-production). |
| Certificate expired or near expiry | Auto-renewal failed. Usually the CNAME is proxied or wrong. | Fix DNS ([A1](#a1-public-dns-has-no-record-or-the-wrong-record)). Then [re-bind](runbook.md#re-bind-the-custom-domain-certificate). |
| Subject is `*.calmsand-3db4f9d7...` or similar | The request reached Azure without the custom domain bound. | Same as the first row. |

Rule: never deploy with `certificateName` empty after the certificate exists.

Verify: rerun the confirm commands. `curl -s -o /dev/null -w "%{http_code}\n" https://go.richardmurillo.com/health` returns `200`.

## B. Deploy failures

Deploy is idempotent. After you fix the cause, rerun the same command:

```sh
op run --env-file=.env.op -- aspire deploy --non-interactive
```

The deploy runs pipeline steps in order: `process-parameters`, `build-prereq`, `validate-build-only-container-references`, `deploy-prereq`, `build-web`, `validate-azure-login`, `create-provisioning-context`, `provision-aca-acr`, `provision-aca`, `provision-storage`, `provision-azure-bicep-resources` (which includes sub-steps such as `provision-web-containerapp`), `deploy`. The failing step name tells you where to look. To list them: `aspire deploy --list-steps --non-interactive`.

Add `--include-exception-details` to see stack traces.

### B1. Bicep "Text file busy"

Symptom: deploy fails in a provision step with:

```text
OSError: [Errno 26] Text file busy: '/home/richard/.azure/bin/bicep'
```

Confirm:

```sh
az bicep version
```

Cause: the az CLI installs Bicep on demand. Aspire runs several provision steps in parallel. More than one tried to install or run Bicep at the same moment.

Fix:

```sh
az bicep install
op run --env-file=.env.op -- aspire deploy --non-interactive
```

Verify: `az bicep version` prints a version (for example `Bicep CLI version 0.47.16`). The deploy finishes without errors.

### B2. InvalidCustomHostNameValidation

Symptom: step `provision-web-containerapp` fails with `InvalidCustomHostNameValidation: A TXT record pointing from asuid.go.richardmurillo.com ...`.

Confirm: run the commands in [A1](#a1-public-dns-has-no-record-or-the-wrong-record). Also check the verification ID Azure expects:

```sh
az containerapp env show -g rg-go-richardmurillo -n aca6lwgsugxgoxds --query properties.customDomainConfiguration.customDomainVerificationId -o tsv
```

Cause: the TXT record `asuid.go` or the CNAME `go` is missing, wrong, or not yet visible in public DNS.

Fix: create or correct both records ([runbook: Recreate or fix the DNS records](runbook.md#recreate-or-fix-the-dns-records)). The TXT value must equal the verification ID above. The CNAME must be DNS only. Wait until DNS over HTTPS shows both records (TTL is 300 seconds). Then rerun the deploy.

Verify: the deploy succeeds and the custom domain shows `SniEnabled` (see [A3](#a3-https-certificate-error) confirm commands).

### B3. Microsoft.App resource provider not registered

Symptom: deploy fails with a message that the subscription is not registered to use namespace `Microsoft.App`. Seen on fresh subscriptions.

Confirm:

```sh
az provider show -n Microsoft.App --query registrationState -o tsv
```

Cause: the subscription has never used Container Apps.

Fix (one-time, modifies the subscription, needs human approval):

```sh
az provider register -n Microsoft.App --wait
```

If the error names other namespaces (for example `Microsoft.OperationalInsights` or `Microsoft.ContainerRegistry`), register those the same way.

Verify: the confirm command prints `Registered`. Rerun the deploy.

### B4. Azure login errors

Symptom: step `validate-azure-login` fails, or errors mention `az login`, expired tokens, `AADSTS`, or the wrong subscription.

Confirm:

```sh
az account show --query "{name:name,id:id,tenant:tenantId,user:user.name}" -o table
```

Expected: `Paid`, `b650d7dc-5e3d-476c-9bbb-299b54b160a0`, tenant `9e7cf608-d64e-46a3-846c-576744b06f4c`, user `rjmurillo@msn.com`.

Cause: not logged in, token expired, or a different default subscription. `.env.op` sets `Azure__CredentialSource=AzureCli`, so the deploy uses the az CLI login.

Fix: ask the human to run `az login` (it opens a browser). If only the subscription is wrong:

```sh
az account set --subscription b650d7dc-5e3d-476c-9bbb-299b54b160a0
```

Verify: the confirm command shows the expected values. Rerun the deploy.

### B5. op run or missing apiKey parameter

Symptom: `op run` fails with an error about the reference `op://Hermes/GO_RICHARDMURILLO_SHORTENER_API_KEY/credential`, or the deploy stops at `process-parameters` asking for `apiKey`. With `--non-interactive` it fails instead of prompting.

Confirm (prints only a length):

```sh
K=$(op read op://Hermes/GO_RICHARDMURILLO_SHORTENER_API_KEY/credential) && echo "length ${#K}"
grep -n 'Parameters__apiKey' .env.op
```

Expected: a length of at least 16, and `Parameters__apiKey=op://Hermes/GO_RICHARDMURILLO_SHORTENER_API_KEY/credential`.

Cause: the 1Password desktop app is locked, the item was renamed or moved, or the deploy ran without `op run`.

Fix: ask the human to unlock 1Password. Always deploy through `op run --env-file=.env.op -- ...`. Do not paste the key on the command line. If the item moved, ask the human for the new location. Do not edit `.env.op` without approval.

Verify: the confirm command prints a length. Rerun the deploy.

### B6. op whoami says account is not signed in

Symptom: `op whoami` prints `[ERROR] ... account is not signed in`.

Confirm:

```sh
op item get GO_RICHARDMURILLO_SHORTENER_API_KEY --vault Hermes --format json > /dev/null && echo "op works"
```

Cause: `op` uses the 1Password desktop app integration. In that mode `op whoami` reports "not signed in" even though other commands work.

Fix: none needed if the confirm command prints `op works`. If it fails, ask the human to unlock the 1Password desktop app.

Verify: `op works` is printed.

### B7. Other deploy errors

1. Note the failing step name and the first error line.
2. Rerun once. Transient Azure errors (timeouts, `Conflict` on a resource that is still updating, `TooManyRequests`) often pass on retry.
3. Rerun with details: `op run --env-file=.env.op -- aspire deploy --non-interactive --include-exception-details`.
4. If the error mentions a parameter value that should have changed but did not, Aspire may be using its deploy cache. Rerun with `--clear-cache`. Do not open the cache file; it contains secrets.
5. If `build-web` fails, run `dotnet build src/UrlShortener.AppHost/UrlShortener.AppHost.csproj` and the [tests](runbook.md#run-the-tests) to see the compile error.
6. If still stuck, stop and report the step name and error text to the human. Do not delete resources to "start fresh".

## C. HTTP status codes

### C1. 401 Unauthorized

Symptom: `POST /api/links` returns `401` with an empty body.

Confirm. The auth check runs before body validation, so a correct key with an invalid URL returns `400`, not `401`. This test creates no link:

```sh
KEY=$(op read op://Hermes/GO_RICHARDMURILLO_SHORTENER_API_KEY/credential) && curl -s -o /dev/null -w "%{http_code}\n" -X POST https://go.richardmurillo.com/api/links \
  -H "Content-Type: application/json" -H "X-Api-Key: $KEY" -d '{"url":"not-a-url"}'; unset KEY
```

Then compare the deployed key with 1Password (prints hashes only):

```sh
printf '%s' "$(op read op://Hermes/GO_RICHARDMURILLO_SHORTENER_API_KEY/credential)" | sha256sum | cut -c1-12
printf '%s' "$(az containerapp secret show -g rg-go-richardmurillo -n web --secret-name shortener--apikey --query value -o tsv 2>/dev/null)" | sha256sum | cut -c1-12
```

Causes and fixes:

| What you see | Cause | Fix |
| --- | --- | --- |
| Confirm returns `400` | The caller used a wrong or old key, or misspelled the header. | Use header name `X-Api-Key` and the current value from 1Password. |
| Confirm returns `401`, hashes differ | 1Password was changed but production was not redeployed. | [Deploy](runbook.md#deploy-to-production). |
| Confirm returns `401`, hashes match | The running replica has an old secret. | Restart the revision (see [runbook: Rotate the production API key](runbook.md#rotate-the-production-api-key), the restart command). |

Common caller mistakes: a trailing newline in the key (read it with `$(...)`, which strips it), extra quotes, or sending the key as a query string.

Verify: the confirm command returns `400`.

### C2. 400 Bad Request

Symptom: `POST /api/links` returns `400` with a JSON `ValidationProblem` body, for example `{"errors":{"url":["url must be an absolute http or https URL."]}}`.

Confirm: read the `errors.url` message in the response.

| Message | Cause | Fix |
| --- | --- | --- |
| `url is required and must be at most 2048 characters.` | Missing, blank, or too long `url`. | Send `{"url":"https://..."}` with at most 2048 characters. |
| `url must be an absolute http or https URL.` | Relative URL, or a scheme such as `ftp:`, `mailto:`, `javascript:`. | Use a full `http://` or `https://` URL. |
| `url must not point at the shortener itself.` | The target host is `go.richardmurillo.com`. | Shorten the final destination instead. |
| Body is not a ValidationProblem, mentions JSON | Malformed JSON or wrong `Content-Type`. | Send valid JSON with `Content-Type: application/json`. |

Verify: the corrected request returns `201`.

### C3. 404 Not Found on a short link

Symptom: `GET https://go.richardmurillo.com/<slug>` returns `404`, but `/health` returns `200`.

Confirm:

```sh
curl -s -o /dev/null -w "%{http_code}\n" https://go.richardmurillo.com/health
curl -s -o /dev/null -w "%{http_code} -> %{redirect_url}\n" https://go.richardmurillo.com/<slug>
```

Causes:

- The slug was mistyped. Slugs are case-sensitive and use only `A-Z`, `a-z`, `0-9`, at most 16 characters. Anything else returns 404 without a lookup.
- The link was never created, or was created in a local environment (local links live in the Azurite emulator, not production).
- No link has ever been created in this storage account. The `links` table is created on the first write. Until then, every slug returns 404.
- The storage account was recreated (for example after a teardown), so old links are gone.

Fix: create the link again ([runbook: Create a short link](runbook.md#create-a-short-link-in-production)). Shortening the same URL produces the same slug, so old short links work again once recreated. This is true unless a collision made the original slug use a later seed; in that case the new slug can differ.

Verify: the slug returns `302` to the expected URL.

If `/health` is not `200`, go to [C5](#c5-500-502-or-503) instead.

### C4. Every request returns an Azure 404 page

Symptom: every path, including `/health`, returns `404` with an HTML page titled `Azure Container App - Unavailable` that says `Error 404 - This Container App is stopped or does not exist.` The app's own 404 has an empty body, so this page means the request never reached the app.

Confirm:

```sh
curl -s -o /dev/null -w "%{http_code}\n" https://web.calmsand-3db4f9d7.westus2.azurecontainerapps.io/health
curl -s -o /dev/null -w "%{http_code}\n" http://go.richardmurillo.com/health
az containerapp show -g rg-go-richardmurillo -n web --query "properties.configuration.ingress.customDomains" -o json
az containerapp show -g rg-go-richardmurillo -n web --query "properties.runningStatus" -o tsv
```

Cause: if the default FQDN returns `200` but `go.richardmurillo.com` returns this page over HTTP (and fails TLS over HTTPS), the custom domain is not attached to the `web` app. If the default FQDN also returns this page, the app is stopped or missing.

Fix: for a missing custom domain, see [A3](#a3-https-certificate-error). If `runningStatus` is not `Running`, report it to the human and, with approval, [deploy](runbook.md#deploy-to-production). For a missing app, check `az resource list -g rg-go-richardmurillo -o table` and [deploy](runbook.md#deploy-to-production).

Verify: `curl -s -o /dev/null -w "%{http_code}\n" https://go.richardmurillo.com/health` returns `200`.

### C5. 500, 502, or 503

Symptom: requests return `500`, `502`, or `503`, or `/health` returns `503` (`Unhealthy`).

Confirm:

```sh
curl -s -w "\n%{http_code}\n" https://go.richardmurillo.com/alive
curl -s -w "\n%{http_code}\n" https://go.richardmurillo.com/health
az containerapp revision list -g rg-go-richardmurillo -n web -o table
az containerapp logs show -g rg-go-richardmurillo -n web --tail 100 --format text
az containerapp logs show -g rg-go-richardmurillo -n web --type system --tail 50
```

`/alive` checks only the process. `/health` also reads the `_health` sentinel entity from Azure Table Storage (5 second timeout). The Container Apps readiness probe calls `/health`, so a revision whose storage is broken stays out of rotation instead of serving errors.

Causes and fixes:

| What you see | Cause | Fix |
| --- | --- | --- |
| `/alive` `200`, `/health` `503`; logs show `AuthorizationPermissionMismatch`, `403`, or Table errors | The app identity `web_identity-6lwgsugxgoxds` cannot reach storage `storage6lwgsugxgoxds` (role assignment missing, or storage was changed). Shared key auth is disabled by design, so do not turn it on. | [Deploy](runbook.md#deploy-to-production). The deploy recreates the role assignment. |
| `/health` `503`; logs mention `Sentinel entry has an unexpected value` | Someone edited the `_health` entity. | Delete the `_health` entity (needs human approval and `Storage Table Data Contributor`); the next probe re-seeds it. |
| Revision `Unhealthy` or failed; logs show `OptionsValidationException` about `Shortener` | Startup config is invalid: `ApiKey` missing or shorter than 16 characters, or `BaseUrl` missing. | Check the key length (see [B5](#b5-op-run-or-missing-apikey-parameter) confirm). Deploy through `op run`. |
| System logs show image pull errors | The image is missing in `acaacr6lwgsugxgoxds` or the identity `aca_mi-6lwgsugxgoxds` cannot pull. | [Deploy](runbook.md#deploy-to-production). |
| `POST /api/links` returns `500`; logs show `Unable to allocate a unique slug.` | All 8 Murmur3 seeds collided with other URLs. Extremely rare. | Report to the human. A workaround is to change the target URL slightly (for example add a harmless query parameter). |
| `500` with a stack trace in logs for any other reason | Application bug. | Report the exception text and time to the human. Do not change code without approval. |
| Single `502` or `503` right after a deploy or during a cold start | The replica was starting. | Retry after 30 seconds. |

Verify: `/health` returns `200` and the revision is `Healthy`.

### C6. Timeout or very slow response

Symptom: the first request after a quiet period takes about 8 seconds. Or requests hang and time out.

Confirm:

```sh
for i in 1 2 3; do curl -s -o /dev/null -w "%{http_code} %{time_total}s\n" --max-time 60 https://go.richardmurillo.com/health; done
```

Causes:

- First request about 8 seconds, then about 0.1 seconds: scale-to-zero cold start. The app scales to 0 replicas after 300 seconds without traffic. This is expected and saves money. Do not change `MinReplicas` without the human's approval, because that adds cost.
- All requests time out, `000` status: DNS points somewhere wrong ([A1](#a1-public-dns-has-no-record-or-the-wrong-record), [A2](#a2-name-resolves-to-0000-or-)), or the replica cannot start ([C5](#c5-500-502-or-503), check system logs).
- Only from one network: local network or firewall. Test with `--resolve go.richardmurillo.com:443:4.149.148.23` and from another network.

Fix: depends on the cause above. Cold starts need no fix.

Verify: the second and third requests in the confirm loop finish in under a second with `200`.

## D. Local development

### D1. Local run fails

Symptom: `aspire start`, `aspire wait web`, or local requests to `http://localhost:5466` fail.

Confirm:

```sh
docker info --format '{{.ServerVersion}}'
dotnet user-secrets list --project src/UrlShortener.AppHost | grep -c '^Parameters:apiKey = '
aspire describe
aspire logs web --tail 100
```

Causes and fixes:

| What you see | Cause | Fix |
| --- | --- | --- |
| `docker info` fails | Docker is not running. Azurite needs it. | Ask the human to start Docker. |
| `grep -c` prints `0` | No local API key. | `dotnet user-secrets set Parameters:apiKey "$(openssl rand -hex 24)" --project src/UrlShortener.AppHost` |
| `aspire wait web` times out | First run is pulling the Azurite image. | `aspire wait web --timeout 300` |
| Port 5466 in use, or another AppHost running | Leftover process. | `aspire stop`, then start again. For worktrees use `aspire start --isolated`. See [`.agents/skills/aspire-orchestration`](../../.agents/skills/aspire-orchestration). |
| Logs show `OptionsValidationException` | Local key shorter than 16 characters. | Set a new key with the command above. |

Verify: `curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5466/health` returns `200`.

## D. k6 suite failures

Run the suites with `test/k6/run.sh` ([runbook](runbook.md#run-the-k6-suites-against-production)).

| What you see | Cause | Fix |
| --- | --- | --- |
| `API_KEY is not set` | Not run through `run.sh`/`op run`, or 1Password prompt dismissed. | Unlock 1Password, rerun, approve the prompt. See [B5](#b5-op-run-or-missing-apikey-parameter). |
| `authorization prompt dismissed` | The 1Password desktop approval was dismissed or timed out. | Rerun and approve the prompt. |
| `STORAGE_TABLE_ENDPOINT and STORAGE_TOKEN are not set` | `az` is not signed in, or k6 was run directly. | `az login`, then use `run.sh`. Or set `BACKEND=off`. |
| `Backend read for '...' returned 403` | Your user lacks `Storage Table Data Reader`, or the role was granted less than about 5 minutes ago. | Grant the role ([runbook](runbook.md#inspect-stored-links)) and wait. |
| `Warm-up failed: GET .../health returned 0` with a DNS or connection error | Local DNS blocks the custom domain. | Set `BASE_URL=https://web.calmsand-3db4f9d7.westus2.azurecontainerapps.io`. See [A2](#a2-name-resolves-to-0000-or-). |
| `Warm-up failed ... returned 503` | Storage is unreachable from the app. | Go to [C5](#c5-500-502-or-503). |
| `shortUrl uses the short base` fails | `SHORT_BASE_URL` does not match the service's `Shortener__BaseUrl`. | Leave `SHORT_BASE_URL` unset for production. |
