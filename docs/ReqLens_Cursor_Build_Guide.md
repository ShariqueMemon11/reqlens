# ReqLens — Cursor Build Guide

## How to use this

1. Create the project folder, `git init`, and copy in both this file and `ReqLens_V1_Spec.md` under a `/docs` folder.
2. Open the folder in Cursor.
3. Paste the **Persistent Rules** block below into a `.cursorrules` file at the project root (Cursor reads this automatically for every chat in this project). This is a one-time setup step.
4. Give Cursor the **Phase 0** prompt. Let it propose a plan, review it, say "go," let it build, then test what it built yourself before moving on.
5. Repeat for each phase, in order. Don't skip ahead, even if a phase looks small — the point is to keep Cursor scoped and keep you following along.
6. If Cursor's plan looks off, or it asks you a question, answer it in the Cursor chat directly — that back-and-forth is intentional, it's the "ask if something's fishy" behavior we're setting up.

---

## 1. Persistent Rules — paste into `.cursorrules` at project root

```
You are acting as a careful senior full-stack engineer building "ReqLens" — an AI
Requirements Analyst app — together with me, a developer who wants to follow along
and learn as we go.

Ground truth: /docs/ReqLens_V1_Spec.md is the source of truth for scope, architecture,
schema, and scoring logic. Read it before starting any phase. If something I ask for
conflicts with it, point out the conflict instead of silently picking one.

How we work:
1. We build in phases, one at a time. I will tell you which phase to start. Do NOT
   implement future phases, and do NOT add features "while you're at it," even if
   they seem related or easy. If you think a future-phase piece is genuinely required
   right now to make this phase work, stop and ask me first.
2. Before writing any code for a phase, give me a short plan: which files you'll
   create or change, and any non-obvious decisions you're making (library choice,
   schema shape, naming). Wait for me to say "go" before writing code.
3. After implementing a phase, give me a short summary: what was built, how to run
   and test it, and anything that needs my input or a decision.
4. If you find something ambiguous, inconsistent, or "fishy" in the spec or in what
   I ask for — stop and ask me a specific question instead of guessing or silently
   picking the "most likely" interpretation.
5. Don't over-engineer. No speculative abstractions, no generic plugin systems, no
   config for things we don't need yet. Prefer explicit, readable code over clever
   code. We can refactor later if a real second use case shows up.
6. Move fast within a phase: use existing, well-known libraries instead of writing
   things from scratch (PDF parsing, schema validation, HTTP client, UI primitives).
   Don't re-litigate stack choices already made in the spec.
7. Every LLM call must go through a single, shared client wrapper — no ad-hoc HTTP
   calls to the LLM scattered across the codebase.
8. Every LLM response must be validated against its JSON schema before being trusted.
   If validation fails, retry once with a repair prompt that includes the validation
   error. If it fails again, surface a clear error instead of silently saving bad data.
9. No mock or fake data pretending to be real output. If a piece isn't built yet, show
   an honest "not implemented yet" state rather than hardcoding fake results.
10. Comment non-obvious logic (why, not what). Keep files and components small and
    single-purpose.
11. Never commit secrets. API keys go in .env, and .env is gitignored. Document what's
    needed in .env.example.

UI requirements — apply from Phase 1 onward, on every screen you touch:
- Full light/dark mode: system-preference detection on first load, a manual toggle,
  and the choice persisted across sessions.
- Consistent design tokens: one spacing scale, one radius scale, one color scale — no
  ad-hoc hex codes or magic pixel values scattered across components.
- Real typography: a proper font pairing, not the browser default sans-serif, with a
  clear type scale for headings, body, and labels.
- Every async state is a designed state: loading uses skeletons (not just a spinner),
  empty states are designed (not a blank screen), error states show a real message
  with a retry action where it makes sense.
- Accessible: visible focus states, sufficient color contrast in both themes,
  keyboard-navigable.
- Responsive down to mobile width, not just desktop.
- Subtle motion on state changes (a panel expanding, a score updating, a question
  being resolved) — restrained, not flashy.
- Consistent iconography from a single icon set, not mixed styles.

Stack (full detail in the spec): React + Vite + TypeScript + Tailwind + shadcn/ui +
lucide-react on the frontend; .NET Core Web API (layered: Api / Application / Domain
/ Infrastructure) + PostgreSQL on the backend; Groq and/or Gemini free-tier APIs for
LLM calls; NJsonSchema for schema validation; PdfPig for PDF text extraction.
```

---

## 2. Target Folder Structure

```
reqlens/
├── docs/
│   ├── ReqLens_V1_Spec.md
│   └── ReqLens_Cursor_Build_Guide.md
│
├── frontend/
│   ├── src/
│   │   ├── components/
│   │   │   ├── ui/              # Button, Card, Badge, ProgressBar, Tabs, Skeleton, EmptyState, Toast
│   │   │   ├── upload/
│   │   │   ├── dashboard/
│   │   │   ├── requirements/
│   │   │   ├── ambiguities/
│   │   │   ├── contradictions/
│   │   │   └── layout/          # header, theme toggle, app shell
│   │   ├── hooks/
│   │   ├── lib/                 # api client, utils
│   │   ├── store/                # app state
│   │   ├── styles/               # theme tokens, globals.css
│   │   ├── types/
│   │   ├── App.tsx
│   │   └── main.tsx
│   ├── public/
│   ├── index.html
│   ├── tailwind.config.ts
│   ├── vite.config.ts
│   └── package.json
│
├── backend/
│   ├── ReqLens.Api/              # controllers, Program.cs, appsettings
│   ├── ReqLens.Application/      # services: parsing, extraction, ambiguity,
│   │                              #   contradiction, scoring, LLM client, DTOs
│   ├── ReqLens.Domain/           # entities, schema models
│   ├── ReqLens.Infrastructure/   # DB persistence, migrations, external services
│   └── ReqLens.Tests/
│
├── .env.example
├── .gitignore
└── README.md
```

---

## 3. Phase-by-Phase Prompts

Paste these into Cursor **one at a time, in order**. Each one assumes the rules file above is already in place.

### Phase 0 — Scaffold
```
Set up the project scaffold only. Create the folder structure exactly as defined in
docs/ReqLens_Cursor_Build_Guide.md (frontend + backend + docs).

Frontend: Vite + React + TypeScript + Tailwind, with shadcn/ui and lucide-react
installed and configured but no custom UI yet beyond a placeholder homepage that
renders "ReqLens" and a working dark/light toggle using Tailwind's dark mode class
strategy.

Backend: .NET Core Web API solution with the four-project layered structure, a single
GET /health endpoint returning { status: "ok" }, CORS configured to allow the
frontend's dev origin, and appsettings for a Postgres connection string (not yet
used).

Add .env.example, .gitignore, and a README with setup/run instructions for both
frontend and backend.

Do not build any business logic, upload flow, or LLM integration yet — that's later
phases. When done, tell me exactly how to run both apps locally and confirm the
health check and dark mode toggle work.
```

### Phase 1 — Design System & App Shell
```
Build the design system and app shell only — no real features yet, everything can use
placeholder content.

Define design tokens (spacing, radius, color scale for light and dark) in Tailwind
config. Build these reusable primitives in components/ui: Button, Card, Badge,
ProgressBar (for score sub-bars), Tabs, Skeleton loader, EmptyState, and a
Toast/notification component.

Build the app shell: header with app name + dark/light toggle, a main content area,
responsive down to mobile.

Build a static, non-functional version of the dashboard layout from the spec (score
card, counts row, list area) using placeholder numbers, just to prove the visual
design works — no data wiring yet.

Give me a plan of the components and token choices before you start.
```

### Phase 2 — Upload → Parse Pipeline
```
Wire the real upload flow end to end, with no LLM involved yet — just prove the
plumbing works.

Frontend: connect the Upload PDF / Paste Text controls from the shell to a real API
call.

Backend: an endpoint that accepts a PDF or raw text, extracts text using PdfPig if
it's a PDF, stores the raw document + extracted text in Postgres (create the
migration), and returns a document ID and the extracted text length.

Frontend should show a loading skeleton while this runs and display the extracted
text length + first few hundred characters on success, or a real error state on
failure (e.g. corrupt PDF, empty text).

No requirement extraction or LLM calls yet — that's Phase 3.
```

### Phase 3 — Requirement Extraction (first real LLM call)
```
Add the first real LLM integration: requirement extraction.

Build the shared LLM client wrapper described in the rules (single place for API key,
provider selection, retries). Ask me for my Groq/Gemini API key setup before writing
the client — don't hardcode a provider silently.

Backend: a service that takes the extracted document text, calls the LLM to split it
into discrete requirement units matching the requirements[] shape in the JSON schema
in the spec, validates the response with NJsonSchema, retries once with a repair
prompt on validation failure, and persists the requirements.

Frontend: after upload succeeds, automatically trigger extraction, show a loading
state, then render the requirements list using the Card primitive from Phase 1.
```

### Phase 4 — Ambiguity Detection + Clarification Questions
```
Add ambiguity detection.

Backend: analyze each stored requirement, produce ambiguities[] matching the schema
(severity, issue, questions with checkbox options), validate and persist.

Frontend: render ambiguities with severity color-coding (use the Badge primitive),
each with its checkbox question list and a "Resolve" action (button only for now —
wiring the actual resolve/re-score flow is Phase 7).

Update the dashboard counts row with the real ambiguity count.
```

### Phase 5 — Contradiction Detection
```
Add contradiction detection using the embedding-based candidate-filtering approach
from the spec — do not brute-force compare every requirement pair.

Backend: generate embeddings for each requirement, find candidate pairs above a
similarity threshold, send only those pairs to the LLM for a contradiction judgment,
validate and persist matches to contradictions[].

Frontend: render contradiction cards showing both conflicting requirements and the
resolution question. Update the dashboard counts row with the real contradiction
count.
```

### Phase 6 — Quality Scoring
```
Implement the hybrid quality score from the spec: rule-based signals computed
directly in code (vague-verb count, acceptance-criteria coverage, actor coverage,
unresolved contradiction/question counts) combined with LLM-assisted subjective
sub-scores (clarity, specificity).

Show me your rule-based scoring logic before wiring it in — this is the part I most
want to understand and be able to defend later.

Frontend: wire the real score into the ProgressBar sub-bars on the dashboard,
replacing the placeholder numbers from Phase 1.
```

### Phase 7 — Resolve & Re-score Flow
```
Wire the "Resolve" actions from Phase 4/5 to actually work: the user checks the
relevant checkboxes or answers the resolution question, submits, the backend updates
the requirement/contradiction state and recomputes the quality score, and the
frontend animates the score updating and moves the resolved item out of the active
list (or marks it resolved).

This is the loop that makes the before/after score story real, so make sure the
change is visibly clear in the UI.
```

### Phase 8 — Polish, Empty/Error States, Eval Harness
```
Final pass. Audit every screen against the UI checklist in the rules (loading, empty,
and error states; dark mode; responsiveness; focus states). Fix anything that was
missed.

Then build the eval harness: a small script or endpoint that runs the pipeline
against a folder of test requirement documents with known planted issues (I'll
provide these) and reports a detection rate, so I can quote a real number in my
portfolio writeup.
```

---

## 4. Working Agreement

- We go one phase at a time, in order. Don't let Cursor jump ahead even if it offers to.
- Test what got built before approving the next phase — run it yourself, click through it, don't just read the summary.
- If Cursor's plan for a phase looks bigger or different than what the prompt asked for, push back before saying "go."
- If you want a second opinion on something Cursor built or proposed, paste it back into this chat and I'll take a look.
