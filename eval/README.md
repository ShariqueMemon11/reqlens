# Eval harness

Run the real pipeline (extract → ambiguities → contradictions) against tagged requirement documents. **Read the gold JSON and the matching `.txt` before you treat a pass/fail number as a portfolio quote.** The harness cannot tell you whether a plant was fair; that is a human judgment.

Quality scoring (the Groq clarity/specificity call) is skipped on purpose. Detection rate is the number the spec asks for. Actor checks use `QualityScoreCalculator` locally on extracted text.

## Files

Each case is a pair with the same stem:

- `eval/cases/<id>.txt` — the document the API will parse
- `eval/cases/<id>.json` — what is planted, in English, with a `whyThisCase` paragraph

Gold fields:

| Field | Meaning |
|---|---|
| `mustExtract` | Substrings that must appear in at least one extracted requirement (dropped-content regressions). |
| `ambiguities` | A hit if any detected ambiguity sits on a requirement whose text contains `needle`. |
| `contradictions` | A hit if any detected pair covers `needleA` and `needleB` (either order). |
| `mustNotDetect` | Fail if a matching requirement is flagged with issue text containing any `issueNeedles` (e.g. actor-undefined on “the system”). |
| `actorChecks` | `HasActor` on the extracted unit. `documentedTradeoff: true` prints as TRADEOFF and is not a harness failure. |

Needles match extracted **text**, not `R-001`, because extraction assigns ids.

## Regression cases (read these first)

- `03-compound-lock-notify` — lock **and** notify in one sentence; notify used to disappear
- `04-system-actor-lock` — “the system” must not be actor-undefined
- `05-prohibition-no-actor` — “Shipped orders cannot be cancelled.” HasActor=false, documented Phase 6 tradeoff
- `10-reports-managed` — sentence used to be omitted from requirement units
- `08-cancel-vs-shipped` and `12-eleven-sentence-mixed` — the contradiction pair from the spec

## Commands

API must already be running for a live run (`dotnet run --project backend/ReqLens.Api --launch-profile http`).

```text
dotnet run --project backend/ReqLens.Eval -- --verify-gold
dotnet run --project backend/ReqLens.Eval -- --list
dotnet run --project backend/ReqLens.Eval -- --api http://localhost:5080
```

`--verify-gold` only checks that every needle appears in its `.txt` (and that `expectHasActor: true` sentences match the regex on the source line). No LLM calls.

A live run writes `eval/last-report.json`. That file is a result artifact; do not treat it as gold.

Optional: `--cases <folder>`, `--out <path>`, `--delay-ms 3000` (default). The delay is applied between extract / ambiguities / contradictions **and** after each of the 12 cases.

Groq's free tier here is **8000 tokens per minute**, not just 30 RPM. A couple of seconds between cases is not enough by itself if extract fires 2–3 large prompts. For a quoteable run, start the API with a Groq gap so calls cannot pile into that TPM window:

```text
$env:Llm__Groq__MinRequestIntervalSeconds = "12"
dotnet run --project backend/ReqLens.Api --launch-profile http
```

If Groq still returns 429 after retries, that case is an **ERROR**, not a detection miss. The portfolio line is omitted whenever any case aborted (`Quoteable: false`). A wait Groq asks for that is longer than 25s (typical tokens-per-day reset) is treated as a failed call, not a 10-minute hang that times out.

Do not quote a rate from a run that disagrees with an immediate second run, or from a `Quoteable: false` report.
