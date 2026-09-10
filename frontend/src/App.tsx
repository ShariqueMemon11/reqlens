import { useState } from "react"

import { AnalysisTabs } from "@/components/dashboard/analysis-tabs"
import { CountsRow, CountsRowSkeleton } from "@/components/dashboard/counts-row"
import { IssueList } from "@/components/dashboard/issue-list"
import { IssueListSkeleton } from "@/components/dashboard/issue-list-skeleton"
import { ScoreCard, ScoreCardSkeleton } from "@/components/dashboard/score-card"
import { AppShell } from "@/components/layout/app-shell"
import { useDocumentPipeline } from "@/hooks/use-document-pipeline"
import { ParseError } from "@/components/upload/parse-error"
import { ParseResult } from "@/components/upload/parse-result"
import { ParseSkeleton } from "@/components/upload/parse-skeleton"
import type { Ambiguity, Contradiction, ResolveIssueResponse } from "@/lib/api"

function countItems(
  requirementCount: number,
  ambiguities: Ambiguity[],
  contradictions: Contradiction[] | undefined
) {
  const openAmbiguities = ambiguities.filter((item) => !item.isResolved)
  const openContradictions = (contradictions ?? []).filter((item) => !item.isResolved)
  return [
    { label: "Requirements", value: String(requirementCount) },
    { label: "Ambiguities", value: String(openAmbiguities.length) },
    {
      label: "Contradictions",
      value: contradictions ? String(openContradictions.length) : "—",
    },
    {
      label: "Questions",
      value: String(
        openAmbiguities.reduce((total, item) => total + item.questions.length, 0)
      ),
    },
  ]
}

export default function App() {
  const { state, submitPdf, submitText, retry, applyResolution, isBusy } =
    useDocumentPipeline()
  const [previousOverall, setPreviousOverall] = useState<number | null>(null)

  function handleResolved(result: ResolveIssueResponse) {
    if (state.status === "success") {
      setPreviousOverall(state.quality.overall)
    }
    applyResolution(result)
  }

  function startPdf(file: File) {
    setPreviousOverall(null)
    submitPdf(file)
  }

  function startText(text: string) {
    setPreviousOverall(null)
    submitText(text)
  }

  return (
    <AppShell
      onSelectPdf={startPdf}
      onSubmitText={startText}
      isBusy={isBusy}
    >
      {state.status === "parsing" ? (
        <ParseSkeleton label="Extracting text…" />
      ) : state.status === "extracting" ? (
        <div className="flex flex-col gap-6">
          <ParseResult result={state.parse} />
          <CountsRowSkeleton />
          <IssueListSkeleton label="Extracting requirements…" />
        </div>
      ) : state.status === "detecting" ? (
        <div className="flex flex-col gap-6">
          <ParseResult result={state.parse} />
          <CountsRow
            items={[
              {
                label: "Requirements",
                value: String(state.extraction.requirements.length),
              },
              { label: "Ambiguities", value: "—" },
              { label: "Contradictions", value: "—" },
              { label: "Questions", value: "—" },
            ]}
          />
          <IssueListSkeleton label="Detecting ambiguities…" />
        </div>
      ) : state.status === "contradicting" ? (
        <div className="flex flex-col gap-6">
          <ParseResult result={state.parse} />
          <CountsRow
            items={countItems(
              state.extraction.requirements.length,
              state.ambiguities.ambiguities,
              undefined
            )}
          />
          <IssueListSkeleton label="Detecting contradictions…" />
        </div>
      ) : state.status === "scoring" ? (
        <div className="flex flex-col gap-6">
          <ParseResult result={state.parse} />
          <ScoreCardSkeleton />
          <CountsRow
            items={countItems(
              state.extraction.requirements.length,
              state.ambiguities.ambiguities,
              state.contradictions.contradictions
            )}
          />
          <AnalysisTabs
            documentId={state.parse.id}
            requirements={state.extraction.requirements}
            ambiguities={state.ambiguities.ambiguities}
            contradictions={state.contradictions.contradictions}
            onResolved={handleResolved}
            candidateCapReached={state.contradictions.candidateCapReached}
            candidatePairsAboveThreshold={
              state.contradictions.candidatePairsAboveThreshold
            }
            candidatePairsSentToLlm={state.contradictions.candidatePairsSentToLlm}
            similarityThreshold={state.contradictions.similarityThreshold}
            neighborsPerRequirement={state.contradictions.neighborsPerRequirement}
          />
        </div>
      ) : state.status === "parse-error" ? (
        <ParseError message={state.message} onRetry={retry} />
      ) : state.status === "extract-error" ? (
        <ParseError
          title="Could not extract requirements"
          message={state.message}
          onRetry={retry}
        />
      ) : state.status === "detect-error" ? (
        <div className="flex flex-col gap-6">
          <ParseResult result={state.parse} />
          <CountsRow
            items={[
              {
                label: "Requirements",
                value: String(state.extraction.requirements.length),
              },
              { label: "Ambiguities", value: "—" },
              { label: "Contradictions", value: "—" },
              { label: "Questions", value: "—" },
            ]}
          />
          <AnalysisTabs
            documentId={state.parse.id}
            requirements={state.extraction.requirements}
            ambiguities={[]}
            contradictions={[]}
            onResolved={handleResolved}
          />
          <ParseError
            title="Could not detect ambiguities"
            message={state.message}
            onRetry={retry}
          />
        </div>
      ) : state.status === "contradiction-error" ? (
        <div className="flex flex-col gap-6">
          <ParseResult result={state.parse} />
          <CountsRow
            items={countItems(
              state.extraction.requirements.length,
              state.ambiguities.ambiguities,
              undefined
            )}
          />
          <AnalysisTabs
            documentId={state.parse.id}
            requirements={state.extraction.requirements}
            ambiguities={state.ambiguities.ambiguities}
            contradictions={[]}
            onResolved={handleResolved}
          />
          <ParseError
            title="Could not detect contradictions"
            message={state.message}
            onRetry={retry}
          />
        </div>
      ) : state.status === "score-error" ? (
        <div className="flex flex-col gap-6">
          <ParseResult result={state.parse} />
          <ScoreCard />
          <CountsRow
            items={countItems(
              state.extraction.requirements.length,
              state.ambiguities.ambiguities,
              state.contradictions.contradictions
            )}
          />
          <AnalysisTabs
            documentId={state.parse.id}
            requirements={state.extraction.requirements}
            ambiguities={state.ambiguities.ambiguities}
            contradictions={state.contradictions.contradictions}
            onResolved={handleResolved}
            candidateCapReached={state.contradictions.candidateCapReached}
            candidatePairsAboveThreshold={
              state.contradictions.candidatePairsAboveThreshold
            }
            candidatePairsSentToLlm={state.contradictions.candidatePairsSentToLlm}
            similarityThreshold={state.contradictions.similarityThreshold}
            neighborsPerRequirement={state.contradictions.neighborsPerRequirement}
          />
          <ParseError
            title="Could not score quality"
            message={state.message}
            onRetry={retry}
          />
        </div>
      ) : state.status === "success" ? (
        <div className="flex flex-col gap-6">
          <ParseResult result={state.parse} />
          <ScoreCard score={state.quality} previousOverall={previousOverall} />
          <CountsRow
            items={countItems(
              state.extraction.requirements.length,
              state.ambiguities.ambiguities,
              state.contradictions.contradictions
            )}
          />
          <AnalysisTabs
            documentId={state.parse.id}
            requirements={state.extraction.requirements}
            ambiguities={state.ambiguities.ambiguities}
            contradictions={state.contradictions.contradictions}
            onResolved={handleResolved}
            candidateCapReached={state.contradictions.candidateCapReached}
            candidatePairsAboveThreshold={
              state.contradictions.candidatePairsAboveThreshold
            }
            candidatePairsSentToLlm={state.contradictions.candidatePairsSentToLlm}
            similarityThreshold={state.contradictions.similarityThreshold}
            neighborsPerRequirement={state.contradictions.neighborsPerRequirement}
          />
        </div>
      ) : (
        <div className="flex flex-col gap-6">
          <ScoreCard />
          <CountsRow />
          <IssueList />
        </div>
      )}
    </AppShell>
  )
}
