# ReqLens — AI Requirements Analyst
### V1 Build Spec (all tooling free to use)

**Product identity:** An AI Business Analyst that finds what humans forgot to specify — not "upload a doc, get a summary," but "here's what's missing, why it matters, and the minimum questions needed to fix it."

---

## 1. V1 Scope

### In scope
- Input: PDF upload **or** pasted text (nothing else)
- Requirement extraction from the input
- Ambiguity detection (the centerpiece feature)
- Contradiction detection (candidate-filtered, not brute-force)
- Missing edge-case detection
- Clarification questions, generated per issue found
- Requirement Quality Score (rule-based + LLM-assisted, not LLM-only)
- Structured JSON output, schema-validated, with a repair/retry loop
- Simple single-user UI — no login required

### Explicitly out of scope for V1
- Authentication, teams, multi-tenancy
- Email / screenshot ingestion (OCR pipeline — separate hard problem)
- Vector database / RAG (belongs in V2 — see Roadmap)
- Requirement → User Stories → Acceptance Criteria → Dev Tasks pipeline (V3)
- Billing, admin panel

Keeping V1 this narrow is what makes it finishable in weeks rather than months, while still containing the feature that actually differentiates the project.

---

## 2. Core Loop

```
User
  │  PDF or pasted text
  ▼
Document Parser  (extract raw text)
  ▼
Requirement Extractor  (LLM: split into discrete requirement units)
  ▼
Requirement Analyzer
  ├── Ambiguity Detection
  ├── Contradiction Detection  (embedding similarity → candidate pairs → LLM judgment)
  ├── Missing Edge Case Detection
  └── Quality Scoring  (rule-based signals + LLM-assisted sub-scores)
  ▼
Clarification Engine  (turns each issue into a concrete question w/ checkbox options)
  ▼
Structured JSON  →  Schema Validation  →  valid? save : retry/repair
  ▼
UI: Quality Score + Requirements + Ambiguities + Contradictions + Questions
  ▼
User answers questions
  ▼
Updated Requirements  →  re-scored
```

---

## 3. Tech Stack (100% free tier / open source)

| Layer | Tool | Why / Cost |
|---|---|---|
| Frontend | React (Vite) | Free, open source |
| Backend | .NET Core Web API | Free SDK, matches your existing stack |
| Database | PostgreSQL | Free — run locally, or a free-tier hosted instance (e.g. Supabase or Neon; check current free-tier limits before committing, these shift) |
| PDF text extraction | PdfPig (.NET, open source, MIT) | No API cost, no external service |
| JSON schema validation | NJsonSchema (.NET, open source, MIT) | Free — validates LLM output against your schema |
| LLM provider (option A — speed) | **Groq** free tier | ~30 RPM / 1,000+ requests per day on Llama/Qwen models, no credit card. Best for a fast, responsive live demo. |
| LLM provider (option B — quality) | **Gemini API** free tier | Flash / Flash-Lite models, no credit card, generous daily request cap. Best if you want stronger reasoning on the ambiguity/contradiction steps. |
| LLM provider (option C — privacy story) | **Ollama** (local) | Fully local, $0 forever, no rate limits, no data leaves your machine — good if you want a "private by design" narrative, but needs reasonable local hardware and models are weaker than the hosted options. |
| Embeddings (for contradiction candidate-filtering) | Local embedding model via Ollama (e.g. `nomic-embed-text`), or Gemini's free embedding endpoint | Free — avoids paying for a vector DB service |
| Hosting — frontend | Vercel or Netlify free tier | Free static hosting |
| Hosting — backend | Render or Fly.io free tier | Free tier sufficient for a portfolio demo (expect cold starts) |
| Version control / CI | GitHub (free) + GitHub Actions free minutes | Free |

**Practical note:** free-tier limits on hosted LLM APIs change often — verify current RPM/RPD caps on the provider's docs before you build your rate-limiting/retry logic, don't hardcode last year's numbers.

**Recommended default for V1:** Groq for the fast, cheap extraction/classification steps (splitting requirements, flagging obvious ambiguity), Gemini Flash for the more reasoning-heavy contradiction judgment step. This mirrors how real production agent pipelines mix a fast/cheap model for high-volume steps with a stronger model for the hard step — a good thing to mention in an interview.

---

## 4. LLM Output Schema (structured, validated)

```json
{
  "requirements": [
    { "id": "R-001", "text": "string", "source_line": 12 }
  ],
  "ambiguities": [
    {
      "requirement_id": "R-001",
      "severity": "high | medium | low",
      "issue": "string — what's undefined",
      "questions": [
        { "text": "Can the admin create new users?", "option_type": "checkbox" }
      ]
    }
  ],
  "contradictions": [
    {
      "requirement_ids": ["R-012", "R-027"],
      "description": "string — the conflict",
      "resolution_question": "string"
    }
  ],
  "missing_edge_cases": [
    { "requirement_id": "R-005", "gaps": ["max file size", "allowed file types", "duplicate handling"] }
  ],
  "qualityScore": {
    "overall": 76,
    "completeness": 82,
    "clarity": 71,
    "testability": 68,
    "consistency": 91,
    "specificity": 74
  }
}
```

Backend flow: LLM response → validate against this schema with NJsonSchema → if invalid, retry with a repair prompt (feed back the validation error) → save only once valid. This is the piece that separates "I called an LLM and printed the result" from an engineered pipeline — worth calling out explicitly in your portfolio writeup.

---

## 5. Quality Score — don't make it LLM-only

A score that's purely "ask the LLM for a number" won't survive a technical interview question. Split it:

**Rule-based signals (cheap, deterministic, explainable):**
- Vague-verb count: requirements containing "manage / handle / support / process" etc. *without* a defined operation list nearby
- % of requirements with at least one attached acceptance criterion
- Actor coverage: actors mentioned vs. actors with defined permissions
- Unresolved contradiction count
- Unanswered clarification-question count

**LLM-assisted signals (subjective, harder to rule-check):**
- Clarity, specificity — genuinely needs judgment, so this part stays LLM-scored

Combine both into the sub-scores shown in the UI. This gives you a defensible answer when someone asks "how is 76/100 actually calculated?"

**Eval set:** build 10–15 requirement documents yourself, with known ambiguities/contradictions deliberately planted and tagged. Run the pipeline against them so you can quote a real number in your portfolio ("caught 18 of 20 planted ambiguities") instead of an unverifiable claim.

---

## 6. Contradiction Detection — scaling note

Don't compare every requirement against every other one (O(n²) LLM calls, slow and wasteful). Instead:

1. Embed each requirement (local model via Ollama, or a free embedding endpoint)
2. Use cosine similarity to find *candidate* pairs that are topically related
3. Only send those candidate pairs to the LLM for an actual contradiction judgment

This is similarity-based candidate filtering on the document's own content — not RAG (no external knowledge retrieval), so it doesn't violate "skip RAG in V1," but it's a legitimate technical answer to "how does this scale to a 50-page spec."

---

## 7. V1 UI (wireframe)

```
┌───────────────────────────────────────────┐
│           ReqLens — AI Requirements Analyst│
│                                             │
│   [ Upload PDF ]      or   [ Paste Text ]  │
│                                             │
│                [ Analyze ]                 │
└───────────────────────────────────────────┘

┌───────────────────────────────────────────┐
│ Requirement Quality              76 / 100  │
│ Completeness ████████░░ 82%                │
│ Clarity      ███████░░░ 71%                │
│ Testability  ██████░░░░ 68%                │
│ Consistency  █████████░ 91%                │
│ Specificity  ███████░░░ 74%                │
└───────────────────────────────────────────┘

Requirements 14   Ambiguities 7   Contradictions 2   Questions 9

⚠️ Ambiguous Requirement — High Priority
"Admin should be able to manage users."
Why: permitted operations are undefined.
☐ Create   ☐ Edit   ☐ Deactivate   ☐ Delete
☐ Reset password   ☐ Assign roles
[ Resolve ]

🔴 Potential Contradiction
R-012: "Customers can cancel an order at any time."
R-027: "Shipped orders cannot be cancelled."
Which rule should take precedence?
[ Resolve ]
```

---

## 8. Roadmap (after V1 works)

**V2**
- Requirements Pattern Bank + RAG (this is where RAG genuinely earns its place — cross-reference new requirements against a library of past ones to suggest commonly-missing pieces, e.g. "payment requirements typically also specify refunds, currencies, webhook handling")
- Requirement versioning / comparison over time

**V3**
- Requirement → User Stories → Acceptance Criteria → Edge Cases → API spec → DB entities → dev tasks pipeline
- This is where the project stops being "requirements analysis" and starts being "requirements → software," which is a strong story on its own

**V4 (only once V1–V3 are solid)**
- Auth, multi-tenant workspaces, teams
- Email / screenshot ingestion (OCR)
- Billing, admin panel

---

## 9. Portfolio writeup (once built)

> Built ReqLens, an AI-powered requirements analysis tool that detects ambiguity, contradictions, and missing edge cases in software requirements documents. Implemented structured LLM outputs with JSON schema validation and automatic repair, a hybrid rule-based + LLM quality scoring system, and embedding-based candidate filtering to make contradiction detection scale to long documents. Evaluated against a hand-built benchmark of planted ambiguities, achieving [X/Y] detection rate.

Fill in the real number once your eval set is running — that's the line that will actually get noticed.
