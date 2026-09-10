import { useState } from "react"
import { GitCompare } from "lucide-react"
import { toast } from "sonner"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Card,
  CardContent,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { EmptyState } from "@/components/ui/empty-state"
import {
  resolveContradiction,
  type Contradiction,
  type Requirement,
  type ResolveIssueResponse,
} from "@/lib/api"

type ContradictionsListProps = {
  documentId: string
  contradictions: Contradiction[]
  requirements: Requirement[]
  onResolved: (result: ResolveIssueResponse) => void
  candidateCapReached?: boolean
  candidatePairsAboveThreshold?: number
  candidatePairsSentToLlm?: number
  similarityThreshold?: number
  neighborsPerRequirement?: number
}

function ContradictionCard({
  documentId,
  contradiction,
  leftText,
  rightText,
  onResolved,
}: {
  documentId: string
  contradiction: Contradiction
  leftText: string | undefined
  rightText: string | undefined
  onResolved: (result: ResolveIssueResponse) => void
}) {
  const [leftId, rightId] = contradiction.requirementIds
  const [chosen, setChosen] = useState(contradiction.chosenRequirementId ?? "")
  const [busy, setBusy] = useState(false)

  async function resolve() {
    if (!chosen || busy || contradiction.isResolved) {
      return
    }

    setBusy(true)
    try {
      const result = await resolveContradiction(documentId, contradiction.id, chosen)
      onResolved(result)
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not resolve this contradiction.")
      setBusy(false)
    }
  }

  return (
    <Card className={contradiction.isResolved ? "opacity-70" : "transition-opacity duration-300"}>
      <CardHeader>
        <div className="flex flex-wrap items-center gap-2">
          <GitCompare className="size-4 text-destructive" aria-hidden="true" />
          <CardTitle>Potential Contradiction</CardTitle>
          <Badge variant="destructive">Conflict</Badge>
          {contradiction.isResolved ? <Badge variant="secondary">Resolved</Badge> : null}
        </div>
      </CardHeader>
      <CardContent className="flex flex-col gap-3">
        <p className="text-body">
          <span className="font-medium">{leftId}:</span>{" "}
          {leftText ? `“${leftText}”` : "Requirement text unavailable."}
        </p>
        <p className="text-body">
          <span className="font-medium">{rightId}:</span>{" "}
          {rightText ? `“${rightText}”` : "Requirement text unavailable."}
        </p>
        <p className="text-body text-muted-foreground">{contradiction.description}</p>
        <p className="text-body">{contradiction.resolutionQuestion}</p>
        <fieldset disabled={contradiction.isResolved || busy} className="flex flex-col gap-2">
          <legend className="text-label font-medium text-muted-foreground">
            Which rule takes precedence?
          </legend>
          {[leftId, rightId].map((requirementId) =>
            requirementId ? (
              <label key={requirementId} className="inline-flex items-center gap-2 text-body">
                <input
                  type="radio"
                  name={`contradiction-${contradiction.id}`}
                  value={requirementId}
                  checked={chosen === requirementId}
                  onChange={() => setChosen(requirementId)}
                  className="size-4 border-input accent-primary focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none"
                />
                {requirementId}
              </label>
            ) : null
          )}
        </fieldset>
      </CardContent>
      {contradiction.isResolved ? null : (
        <CardFooter>
          <Button
            type="button"
            disabled={!chosen || busy}
            onClick={() => void resolve()}
          >
            {busy ? "Resolving…" : "Resolve"}
          </Button>
        </CardFooter>
      )}
    </Card>
  )
}

export function ContradictionsList({
  documentId,
  contradictions,
  requirements,
  onResolved,
  candidatePairsAboveThreshold,
  candidatePairsSentToLlm,
  similarityThreshold,
  neighborsPerRequirement,
}: ContradictionsListProps) {
  const textById = new Map(
    requirements.map((requirement) => [requirement.id, requirement.text])
  )
  const active = contradictions.filter((item) => !item.isResolved)
  const resolved = contradictions.filter((item) => item.isResolved)

  return (
    <div className="flex flex-col gap-3">
      {candidatePairsAboveThreshold != null &&
      candidatePairsSentToLlm != null &&
      candidatePairsAboveThreshold > candidatePairsSentToLlm ? (
        <p
          className="rounded-xl border bg-muted/40 px-4 py-3 text-body text-muted-foreground"
          role="status"
        >
          Compared {candidatePairsSentToLlm} nearest-neighbor pairs (top{" "}
          {neighborsPerRequirement ?? 5} per requirement) out of{" "}
          {candidatePairsAboveThreshold} pairs above cosine{" "}
          {similarityThreshold ?? 0.7}. The rest were not sent to the model.
        </p>
      ) : null}

      {contradictions.length === 0 ? (
        <EmptyState
          icon={<GitCompare className="size-8" />}
          title="No contradictions found"
          description="Related requirements were compared, and none look like they cannot both be true."
        />
      ) : (
        <div className="flex flex-col gap-6">
          {active.length === 0 ? (
            <EmptyState
              icon={<GitCompare className="size-8" />}
              title="All contradictions resolved"
              description="Active conflicts are cleared. Consistency on the score card uses only unresolved pairs."
            />
          ) : (
            <ul className="flex flex-col gap-3">
              {active.map((contradiction) => {
                const [leftId, rightId] = contradiction.requirementIds
                return (
                  <li key={contradiction.id}>
                    <ContradictionCard
                      documentId={documentId}
                      contradiction={contradiction}
                      leftText={leftId ? textById.get(leftId) : undefined}
                      rightText={rightId ? textById.get(rightId) : undefined}
                      onResolved={onResolved}
                    />
                  </li>
                )
              })}
            </ul>
          )}

          {resolved.length > 0 ? (
            <section className="flex flex-col gap-3">
              <h3 className="text-label font-medium text-muted-foreground">Resolved</h3>
              <ul className="flex flex-col gap-3">
                {resolved.map((contradiction) => {
                  const [leftId, rightId] = contradiction.requirementIds
                  return (
                    <li key={contradiction.id}>
                      <ContradictionCard
                        documentId={documentId}
                        contradiction={contradiction}
                        leftText={leftId ? textById.get(leftId) : undefined}
                        rightText={rightId ? textById.get(rightId) : undefined}
                        onResolved={onResolved}
                      />
                    </li>
                  )
                })}
              </ul>
            </section>
          ) : null}
        </div>
      )}
    </div>
  )
}
