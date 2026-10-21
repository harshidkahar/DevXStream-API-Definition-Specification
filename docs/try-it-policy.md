# DADS try-it policy

The DADS portal never enables live API requests by default. A deployment must explicitly configure `data/try-it.json` with a trusted environment URL and an operation ID allow-list. Do not use a production environment as the first preview target.

Only configured operations can execute. The browser constructs a request from the selected environment, documented route, parameter examples, and optional JSON body; it blocks any URL outside that environment. Responses are capped at 100 KB. The portal has no field for API keys and must not be used to distribute credentials.

`GET`, `HEAD`, and `OPTIONS` are considered read-only. `POST`, `PUT`, `PATCH`, and `DELETE` remain disabled unless `allowStateChanging` is explicitly true, and then require a second deliberate confirmation click for each operation. Configure these only for a disposable sandbox and an approved operation allow-list.

Example configuration:

```json
{
  "enabled": true,
  "allowStateChanging": false,
  "environments": [{ "id": "sandbox", "label": "Sandbox", "url": "https://sandbox.example.com/v1" }],
  "allowedOperations": ["catalog-listproducts"]
}
```
