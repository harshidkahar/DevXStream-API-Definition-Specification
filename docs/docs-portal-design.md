# DADS Documentation Portal Design Baseline

## Brand assets

Use the supplied DevXStream PNG logo without cropping, recolouring, or transformation. The application references the exact asset at `docs-portal/assets/devxstream-logo.png`.

## Design tokens

| Token | Value | Use |
| --- | --- | --- |
| `--brand` | `#5B4CFF` | Primary actions and emphasis |
| `--brand-dark` | `#3D2EDC` | Hover/active state |
| `--get` | `#147D52` | GET method label |
| `--post` | `#1868C5` | POST method label |
| `--put` | `#9B5B00` | PUT/PATCH method label |
| `--delete` | `#B42318` | DELETE method label |
| `--surface` | `#FFFFFF` | Cards and navigation surface |
| `--bg` | `#F7F8FC` | Page background |

## Wireframes

```text
+----------------------+------------------------------------------------+
| Exact DevXStream     | API name · version        Search · downloads |
| logo                 +------------------------------------------------+
|                      | Introduction / module / endpoint content      |
| Search               |                                                |
|                      | METHOD  /route                                |
| Introduction         | Summary and consumer-facing description        |
| Module               | Parameters · request · responses · snippets    |
|   GET /resource      |                                                |
|   POST /resource     | [Expandable DevXStream engineering details]    |
| Models               |                                                |
+----------------------+------------------------------------------------+
```

## Accessibility requirements

- WCAG 2.2 AA contrast minimum.
- Full keyboard access, logical focus sequence, and visible focus styling.
- Semantic landmarks, headings, labels, and descriptive copy buttons.
- Method labels include textual HTTP verbs; colour is never the only signal.
- Respect `prefers-reduced-motion`; do not require hover to access content.
