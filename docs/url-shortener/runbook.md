# Runbook: go.richardmurillo.com URL shortener

Task recipes. Each recipe lists prerequisites, exact commands, and a verification step. Run commands exactly as written. Read the [safety rules](README.md#safety-rules-for-agents) first.

All commands assume this working directory unless a recipe says otherwise:

```sh
cd /home/richard/src/GitHub/rjmurillo/url-bench
```

Each shell command in these docs is independent. If your tool starts a new shell for every command, repeat the `cd` and any variable assignment (for example `KEY=...`) in the same command that uses it.

Contents:

1. [Common checks](#common-checks)
2. [Create a short link in production](#create-a-short-link-in-production)
3. [Run locally](#run-locally)
4. [Run the tests](#run-the-tests)
5. [Deploy to production](#deploy-to-production)
6. [Rotate the production API key](#rotate-the-production-api-key)
7. [Recreate or fix the DNS records](#recreate-or-fix-the-dns-records)
8. [Re-bind the custom domain certificate](#re-bind-the-custom-domain-certificate)
9. [View logs and revisions](#view-logs-and-revisions)
10. [Tear down](#tear-down)

## Common checks

Use these read-only checks before and after any task.

Azure login and subscription:

```sh
az account show --query "{name:name,id:id,user:user.name}" -o table
```

Expected: name `Paid`, ID `b650d7dc-5e3d-476c-9bbb-299b54b160a0`, user `rjmurillo@msn.com`. If it fails, a human must run `az login`. If the subscription differs, run `az account set --subscription b650d7dc-5e3d-476c-9bbb-299b54b160a0`.

1Password access (prints only a length):

```sh
K=$(op read op://Hermes/GO_RICHARDMURILLO_SHORTENER_API_KEY/credential) && echo "api key length ${#K}"
```

Expected: `api key length 48` (or another number of at least 16). `op whoami` may say "account is not signed in". Ignore that. If `op read` fails, ask the human to unlock the 1Password desktop app.

Public health:

```sh
curl -s -o /dev/null -w "%{http_code} %{time_total}s\n" https://go.richardmurillo.com/health
```

Expected: `200`. The first request after idle can take about 8 seconds (cold start). If the command fails to connect, see [troubleshooting.md](troubleshooting.md#a-cannot-connect-or-dns-does-not-resolve).

Resource inventory:

```sh
az resource list -g rg-go-richardmurillo -o table
```

Expected: 8 resources, all `Succeeded`: `storage6lwgsugxgoxds`, `web_identity-6lwgsugxgoxds`, `acaacr6lwgsugxgoxds`, `aca_mi-6lwgsugxgoxds`, `acalaw-6lwgsugxgoxds`, `aca6lwgsugxgoxds`, `web`, `aca6lwgsugxgoxds/mc-aca6lwgsugxgox-go-richardmurill-5559`.

Production API key in sync (prints two 12-character hashes, never the key):

```sh
printf '%s' "$(op read op://Hermes/GO_RICHARDMURILLO_SHORTENER_API_KEY/credential)" | sha256sum | cut -c1-12
printf '%s' "$(az containerapp secret show -g rg-go-richardmurillo -n web --secret-name shortener--apikey --query value -o tsv 2>/dev/null)" | sha256sum | cut -c1-12
```

Expected: both lines are identical. If they differ, the deployed key does not match 1Password. Deploy again (see [Deploy to production](#deploy-to-production)).

## Create a short link in production

This writes a permanent link. There is no delete API. Only create links the human asked for.

Prerequisites: 1Password access.

Command. Replace `https://example.com/some/page` with the target URL:

```sh
KEY=$(op read op://Hermes/GO_RICHARDMURILLO_SHORTENER_API_KEY/credential) && \
curl -s -w "\nHTTP %{http_code}\n" -X POST https://go.richardmurillo.com/api/links \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: $KEY" \
  -d '{"url":"https://example.com/some/page"}'; unset KEY
```

Expected output: `HTTP 201` and a JSON body like:

```json
{"slug":"3kTMd7","shortUrl":"https://go.richardmurillo.com/3kTMd7","url":"https://example.com/some/page"}
```

Give the human the `shortUrl` value. Calling the API again with the same URL returns the same slug. That is safe.

Verify. Replace `<slug>` with the returned slug:

```sh
curl -s -o /dev/null -w "%{http_code} -> %{redirect_url}\n" https://go.richardmurillo.com/<slug>
```

Expected: `302 -> https://example.com/some/page`.

Other results: `401` means the key is wrong or missing. `400` means the URL is invalid. See [troubleshooting.md](troubleshooting.md#c-http-status-codes).

## Run locally

Prerequisites:

- Docker is running: `docker info --format '{{.ServerVersion}}'` prints a version.
- The Aspire CLI exists: `~/.dotnet/tools/aspire --version` prints `13.6.0+...`.
- A local API key is set. Check without printing it:

  ```sh
  dotnet user-secrets list --project src/UrlShortener.AppHost | grep -c '^Parameters:apiKey = '
  ```

  If it prints `0`, set one:

  ```sh
  dotnet user-secrets set Parameters:apiKey "$(openssl rand -hex 24)" --project src/UrlShortener.AppHost
  ```

  This local key is not the production key. Do not copy the production key into user secrets.

Start, wait, and inspect:

```sh
aspire start --non-interactive
aspire wait web
aspire describe
```

`aspire start` runs the AppHost in the background. It starts Azurite (the Azure Storage emulator) in Docker and the `web` project. `aspire wait web` blocks until `web` is healthy (default timeout 120 seconds; add `--timeout 300` if Docker must pull images). `aspire describe` lists resources and their URLs.

Verify:

```sh
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5466/health
KEY=$(dotnet user-secrets list --project src/UrlShortener.AppHost | sed -n 's/^Parameters:apiKey = //p') && \
curl -s -w "\nHTTP %{http_code}\n" -X POST http://localhost:5466/api/links \
  -H "Content-Type: application/json" -H "X-Api-Key: $KEY" \
  -d '{"url":"https://example.com/"}'; unset KEY
```

Expected: `200`, then `HTTP 201` with a `shortUrl` that starts with `http://localhost:5466/`.

Logs: `aspire logs web --tail 100`.

Stop when done:

```sh
aspire stop
```

If `aspire` is not on `PATH`, use `~/.dotnet/tools/aspire` instead. For lifecycle problems (port conflicts, file locks) read [`.agents/skills/aspire-orchestration`](../../.agents/skills/aspire-orchestration).

## Run the tests

Prerequisites: .NET 10 SDK (`dotnet --version` prints `10.0.x`). Docker is not needed.

```sh
dotnet test test/UrlShortener.Web.Tests/UrlShortener.Web.Tests.csproj
```

Expected last line: `Passed!  - Failed:     0, Passed:    19, Skipped:     0, Total:    19`.

The older benchmark test project `test/UrlConverterPermutations.csproj` excludes the `UrlShortener.Web.Tests` folder. Run the command above to test the shortener.

## Run the k6 suites against production

The suites in [`test/k6`](../../test/k6) test the deployed service over HTTP and also read the `links` table directly, to prove each link landed in storage and the health sentinel exists.

| Suite | What it checks | Approval |
| --- | --- | --- |
| `smoke` | Health, root redirect, create + redirect + backend entity, sentinel in storage, unknown slug. 8 checks. | No |
| `integration` | Full HTTP contract: auth, idempotency, normalization, validation (400/415), misses, method handling, HTTPS upgrade, backend entities, sentinel not public. 44 checks. | No |
| `load` | 5 virtual users for about 90 seconds of redirects. Thresholds p95 < 500 ms, p99 < 1000 ms, errors < 1%. | Yes |

Prerequisites: `k6 version` works (install with `brew install k6`), `az account show` works, the 1Password desktop app is unlocked, and your Azure user has `Storage Table Data Reader` on `storage6lwgsugxgoxds`.

```sh
test/k6/run.sh smoke
test/k6/run.sh integration
```

If local DNS blocks `go.richardmurillo.com` (see [troubleshooting A2](troubleshooting.md#a2-name-resolves-to-0000-or-)), target the Container Apps host instead. Short links still use the custom domain, so `SHORT_BASE_URL` stays at its default:

```sh
BASE_URL=https://web.calmsand-3db4f9d7.westus2.azurecontainerapps.io test/k6/run.sh smoke
```

Expected: `checks_succeeded...: 100.00%` and no `thresholds ... have been crossed` line. The exit code is non-zero when any threshold fails.

`run.sh` gets the API key from `.env.op` through `op run`, the table endpoint from `az storage account list`, and a storage token from `az account get-access-token --resource https://storage.azure.com`. Set `BACKEND=off` to skip storage reads (for example against a local Azurite run). Extra arguments go to `k6 run`, for example `-e LOAD_VUS=2`.

The suites only shorten fixed `https://example.com/?source=k6-...` URLs. Shortening is idempotent, so reruns add no rows.

## Inspect stored links

Read-only. Needs `Storage Table Data Reader` (shared key access is disabled, so `--auth-mode login` is required):

```sh
az storage entity query --account-name storage6lwgsugxgoxds --auth-mode login --table-name links --select PartitionKey Url -o table
```

`PartitionKey` is the slug. The `_health` row is the readiness sentinel. In the Azure portal, open the storage account, then Storage browser, then Tables, then `links`, and set the authentication method to "Microsoft Entra user account". With "Access key" the portal shows an error or nothing, because shared keys are disabled.

To grant another user read access:

```sh
az role assignment create --assignee <user@domain> --role "Storage Table Data Reader" \
  --scope "$(az storage account show -n storage6lwgsugxgoxds -g rg-go-richardmurillo --query id -o tsv)"
```

## Deploy to production

Needs human approval unless the human asked you to deploy. Deploy is idempotent. Running it twice is safe.

Prerequisites:

1. [Common checks](#common-checks) pass for Azure login and 1Password access.
2. `certificateName` is not empty:

   ```sh
   grep -n '"certificateName"' src/UrlShortener.AppHost/appsettings.json
   ```

   Expected: `"certificateName": "mc-aca6lwgsugxgox-go-richardmurill-5559"`. Confirm the certificate exists:

   ```sh
   az containerapp env certificate list -g rg-go-richardmurillo -n aca6lwgsugxgoxds --managed-certificates-only -o table
   ```

   The `Name` column must match the value in `appsettings.json`. If it does not, stop and follow [Re-bind the custom domain certificate](#re-bind-the-custom-domain-certificate).
3. Tests pass ([Run the tests](#run-the-tests)).
4. Bicep is installed, to avoid a known race: `az bicep version`. If it errors, run `az bicep install` first.

Deploy:

```sh
op run --env-file=.env.op -- aspire deploy --non-interactive
```

This builds the container image, pushes it to `acaacr6lwgsugxgoxds`, and applies Bicep for every resource. `op run` injects the API key from 1Password as `Parameters__apiKey`. It takes a few minutes.

Verify:

```sh
az containerapp revision list -g rg-go-richardmurillo -n web -o table
curl -s -o /dev/null -w "%{http_code}\n" https://go.richardmurillo.com/health
curl -s -o /dev/null -w "%{http_code} -> %{redirect_url}\n" https://go.richardmurillo.com/
az containerapp show -g rg-go-richardmurillo -n web --query "properties.configuration.ingress.customDomains[].{name:name,binding:bindingType}" -o table
```

Expected: the newest revision is `Active`, `Healthy`, traffic 100. Health is `200`. Root is `302 -> https://richardmurillo.com/`. The custom domain binding is `SniEnabled` (not `Disabled`). Then run the API key sync check from [Common checks](#common-checks). Then run `test/k6/run.sh smoke` ([Run the k6 suites](#run-the-k6-suites-against-production)).

If the deploy fails, find the error message in [troubleshooting.md](troubleshooting.md#b-deploy-failures). For deploy internals see [`.agents/skills/aspire-deployment`](../../.agents/skills/aspire-deployment).

## Rotate the production API key

Needs human approval. After the deploy, the old key stops working. Anyone who uses the old key must get the new one from 1Password.

Prerequisites: [Common checks](#common-checks) pass. Deploy prerequisites pass.

Step 1. Save the old key in a shell file readable only by you, so you can prove it stops working. Do not print it.

```sh
umask 077 && op read op://Hermes/GO_RICHARDMURILLO_SHORTENER_API_KEY/credential > /tmp/shortener-old-key
```

Step 2. Generate a new key and write it to 1Password. The `> /dev/null` hides the item output.

```sh
NEW=$(openssl rand -hex 24) && op item edit GO_RICHARDMURILLO_SHORTENER_API_KEY --vault Hermes "credential=$NEW" > /dev/null && echo "updated, length ${#NEW}"; unset NEW
```

Expected: `updated, length 48`. Note: `op item edit` takes the value as a command argument, which other local processes can briefly see. On a single-user workstation this is acceptable.

Step 3. Confirm 1Password now holds a different key:

```sh
printf '%s' "$(cat /tmp/shortener-old-key)" | sha256sum | cut -c1-12
printf '%s' "$(op read op://Hermes/GO_RICHARDMURILLO_SHORTENER_API_KEY/credential)" | sha256sum | cut -c1-12
```

Expected: the two hashes differ.

Step 4. Deploy:

```sh
op run --env-file=.env.op -- aspire deploy --non-interactive
```

Step 5. Confirm the container app secret matches 1Password. Run the API key sync check from [Common checks](#common-checks). If the hashes differ, Aspire used a cached parameter value. Deploy again with the cache cleared:

```sh
op run --env-file=.env.op -- aspire deploy --non-interactive --clear-cache
```

Step 6. Confirm the old key is rejected and the new key is accepted. This sends an invalid URL on purpose, so no link is created. A 400 proves the key passed the auth check.

```sh
OLD=$(cat /tmp/shortener-old-key) && curl -s -o /dev/null -w "old key: %{http_code}\n" -X POST https://go.richardmurillo.com/api/links \
  -H "Content-Type: application/json" -H "X-Api-Key: $OLD" -d '{"url":"not-a-url"}'; unset OLD
NEW=$(op read op://Hermes/GO_RICHARDMURILLO_SHORTENER_API_KEY/credential) && curl -s -o /dev/null -w "new key: %{http_code}\n" -X POST https://go.richardmurillo.com/api/links \
  -H "Content-Type: application/json" -H "X-Api-Key: $NEW" -d '{"url":"not-a-url"}'; unset NEW
```

Expected: `old key: 401` and `new key: 400`. If the human wants a full check, also run [Create a short link](#create-a-short-link-in-production) and expect `201`.

If the old key still returns 400, the running replica still has the old secret. Restart the active revision:

```sh
REV=$(az containerapp show -g rg-go-richardmurillo -n web --query properties.latestRevisionName -o tsv) && \
az containerapp revision restart -g rg-go-richardmurillo -n web --revision "$REV"
```

Then repeat step 6.

Step 7. Delete the saved old key:

```sh
rm -f /tmp/shortener-old-key
```

## Recreate or fix the DNS records

Needs human approval for any write. Use this when `go` or `asuid.go` is missing, wrong, or proxied.

Required records in zone `richardmurillo.com` (zone ID `eb3e06806ce27389477ee4730b056822`):

| Type | Name | Content | Proxied | TTL |
| --- | --- | --- | --- | --- |
| CNAME | `go` | `web.calmsand-3db4f9d7.westus2.azurecontainerapps.io` | `false` | 300 |
| TXT | `asuid.go` | `A8F57878C0EC92FC4107CA2435EC583C8E98D7A04DD58FE799163EDA82997E9D` | n/a | 300 |

If the Container Apps environment was recreated, the default domain and verification ID change. Read the current values first:

```sh
az containerapp env show -g rg-go-richardmurillo -n aca6lwgsugxgoxds --query "{domain:properties.defaultDomain,verificationId:properties.customDomainConfiguration.customDomainVerificationId}" -o json
```

The CNAME target is `web.<domain>`.

Step 1. Read the current records (read-only). This also tests the token. Do not use `/user/tokens/verify`; it fails for this account-owned token.

```sh
CF=$(op read op://Hermes/CLOUDFLARE_API_TOKEN/credential) && \
curl -s -H "Authorization: Bearer $CF" \
  "https://api.cloudflare.com/client/v4/zones/eb3e06806ce27389477ee4730b056822/dns_records?name.contains=go.richardmurillo.com" \
  | python3 -c 'import json,sys;d=json.load(sys.stdin);print("success",d["success"]);[print(r["id"],r["type"],r["name"],r["content"],"proxied="+str(r["proxied"])) for r in d["result"]]'; unset CF
```

Expected: `success True`, then one CNAME and one TXT line matching the table.

Step 2a. If a record is missing, create it. CNAME:

```sh
CF=$(op read op://Hermes/CLOUDFLARE_API_TOKEN/credential) && \
curl -s -X POST -H "Authorization: Bearer $CF" -H "Content-Type: application/json" \
  "https://api.cloudflare.com/client/v4/zones/eb3e06806ce27389477ee4730b056822/dns_records" \
  -d '{"type":"CNAME","name":"go","content":"web.calmsand-3db4f9d7.westus2.azurecontainerapps.io","proxied":false,"ttl":300}' \
  | python3 -c 'import json,sys;d=json.load(sys.stdin);print(d["success"],d.get("errors"))'; unset CF
```

TXT:

```sh
CF=$(op read op://Hermes/CLOUDFLARE_API_TOKEN/credential) && \
curl -s -X POST -H "Authorization: Bearer $CF" -H "Content-Type: application/json" \
  "https://api.cloudflare.com/client/v4/zones/eb3e06806ce27389477ee4730b056822/dns_records" \
  -d '{"type":"TXT","name":"asuid.go","content":"\"A8F57878C0EC92FC4107CA2435EC583C8E98D7A04DD58FE799163EDA82997E9D\"","ttl":300}' \
  | python3 -c 'import json,sys;d=json.load(sys.stdin);print(d["success"],d.get("errors"))'; unset CF
```

Step 2b. If the CNAME exists but `proxied=True` or the content is wrong, patch it. Replace `<record-id>` with the ID from step 1 (currently `0904ffed7396c2df0ff76845e3a25393`):

```sh
CF=$(op read op://Hermes/CLOUDFLARE_API_TOKEN/credential) && \
curl -s -X PATCH -H "Authorization: Bearer $CF" -H "Content-Type: application/json" \
  "https://api.cloudflare.com/client/v4/zones/eb3e06806ce27389477ee4730b056822/dns_records/<record-id>" \
  -d '{"content":"web.calmsand-3db4f9d7.westus2.azurecontainerapps.io","proxied":false}' \
  | python3 -c 'import json,sys;d=json.load(sys.stdin);print(d["success"],d.get("errors"))'; unset CF
```

Expected for each write: `True []`.

Step 3. Verify with public DNS over HTTPS. This avoids local DNS filters:

```sh
curl -s -H 'accept: application/dns-json' 'https://cloudflare-dns.com/dns-query?name=go.richardmurillo.com&type=CNAME'
curl -s -H 'accept: application/dns-json' 'https://cloudflare-dns.com/dns-query?name=asuid.go.richardmurillo.com&type=TXT'
```

Expected: the `Answer` arrays contain the CNAME target and the TXT value. You can also run `dig +short @1.1.1.1 go.richardmurillo.com CNAME` and `dig +short @1.1.1.1 asuid.go.richardmurillo.com TXT`, but on Richard's home network `dig` can return wrong answers (see [troubleshooting.md](troubleshooting.md#a2-name-resolves-to-0000-or-)).

Then run the health check from [Common checks](#common-checks).

## Re-bind the custom domain certificate

Use this when the custom domain binding is `Disabled`, the certificate is missing, or HTTPS shows a certificate error for `go.richardmurillo.com`.

Prerequisites: DNS records are correct ([Recreate or fix the DNS records](#recreate-or-fix-the-dns-records), step 1 and step 3). The `containerapp` az extension is installed:

```sh
az extension show -n containerapp --query version -o tsv || az extension add -n containerapp
```

Step 1. Inspect (read-only):

```sh
az containerapp show -g rg-go-richardmurillo -n web --query "properties.configuration.ingress.customDomains" -o json
az containerapp env certificate list -g rg-go-richardmurillo -n aca6lwgsugxgoxds --managed-certificates-only -o table
```

Healthy state: one custom domain `go.richardmurillo.com` with `bindingType` `SniEnabled` and a `certificateId` that ends in `/managedCertificates/mc-aca6lwgsugxgox-go-richardmurill-5559`. The certificate list shows that name.

Step 2. If the binding is `Disabled` or the certificate is missing, bind it. Azure creates (or reuses) a managed certificate and validates it with the CNAME. This can take several minutes.

```sh
az containerapp hostname bind -g rg-go-richardmurillo -n web --hostname go.richardmurillo.com --environment aca6lwgsugxgoxds --validation-method CNAME
```

Step 3. Read the certificate name:

```sh
az containerapp env certificate list -g rg-go-richardmurillo -n aca6lwgsugxgoxds --managed-certificates-only -o table
```

Step 4. If the name differs from `Parameters:certificateName` in [`src/UrlShortener.AppHost/appsettings.json`](../../src/UrlShortener.AppHost/appsettings.json), edit that file so it holds the new name. This is a source change; tell the human so they can commit it. Then deploy ([Deploy to production](#deploy-to-production)). If you skip this, the next deploy unbinds the certificate again.

Verify:

```sh
echo | openssl s_client -connect go.richardmurillo.com:443 -servername go.richardmurillo.com 2>/dev/null | openssl x509 -noout -subject -issuer -enddate
curl -s -o /dev/null -w "%{http_code}\n" https://go.richardmurillo.com/health
```

Expected: subject `CN=go.richardmurillo.com`, issuer GeoTrust (DigiCert), an end date in the future, and `200`.

## View logs and revisions

All read-only.

Application (console) logs, last 100 lines:

```sh
az containerapp logs show -g rg-go-richardmurillo -n web --tail 100 --format text
```

`--tail` accepts 0 to 300. Add `--follow` to stream. Stop streaming with Ctrl+C. If the app is scaled to zero, there may be no replica to read from. Send a request to `/health` first to wake it.

System logs (scaling, image pulls, probe failures, secret errors). System logs support only JSON format:

```sh
az containerapp logs show -g rg-go-richardmurillo -n web --type system --tail 100
```

Revisions:

```sh
az containerapp revision list -g rg-go-richardmurillo -n web -o table
az containerapp revision list -g rg-go-richardmurillo -n web --all -o table
```

The second command includes inactive revisions.

Current app configuration (no secret values):

```sh
az containerapp show -g rg-go-richardmurillo -n web --query "{revision:properties.latestRevisionName,image:properties.template.containers[0].image,scale:properties.template.scale,domains:properties.configuration.ingress.customDomains,env:properties.template.containers[0].env[].name}" -o json
```

Local logs while running locally: `aspire logs web --tail 100`. For traces and structured logs see [`.agents/skills/aspire-monitoring`](../../.agents/skills/aspire-monitoring).

## Tear down

Needs human approval. Always. This deletes every Azure resource in the deployment, including the storage account and every short link. It cannot be undone. Do not run it to "fix" a problem.

Prerequisites: the human has explicitly said to delete the production deployment. [Common checks](#common-checks) pass.

Optional backup of links before teardown. Ask the human whether they want one. Shared key access is disabled, so use Azure AD auth. Your account needs the "Storage Table Data Reader" role on the storage account.

```sh
az storage entity query --account-name storage6lwgsugxgoxds --table-name links --auth-mode login -o json > /tmp/links-backup.json
```

Destroy:

```sh
op run --env-file=.env.op -- aspire destroy --non-interactive --yes
```

`--yes` skips the confirmation prompt. `aspire destroy` refuses to run with `--non-interactive` unless `--yes` is also present. If a human is present and wants to confirm at the prompt, run `op run --env-file=.env.op -- aspire destroy` without either flag.

After Azure is gone, remove the DNS records so `go.richardmurillo.com` does not point at a deleted Azure host. Use the record IDs from [Recreate or fix the DNS records](#recreate-or-fix-the-dns-records), step 1:

```sh
CF=$(op read op://Hermes/CLOUDFLARE_API_TOKEN/credential) && \
for id in <cname-record-id> <txt-record-id>; do
  curl -s -X DELETE -H "Authorization: Bearer $CF" \
    "https://api.cloudflare.com/client/v4/zones/eb3e06806ce27389477ee4730b056822/dns_records/$id" \
    | python3 -c 'import json,sys;d=json.load(sys.stdin);print(d["success"],d.get("errors"))'
done; unset CF
```

Verify:

```sh
az group exists -n rg-go-richardmurillo
az resource list -g rg-go-richardmurillo -o table
```

Expected: `false`, or an empty resource list if the group remains.
