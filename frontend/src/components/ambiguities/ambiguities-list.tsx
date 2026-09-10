import { useState } from "react"
import { TriangleAlert } from "lucide-react"
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
import { resolveAmbiguity, type Ambiguity, type Requirement, type ResolveIssueResponse } from "@/lib/api"

type AmbiguitiesListProps = {
  documentId: string
  ambiguities: Ambiguity[]
  requirements: Requirement[]
  onResolved: (result: ResolveIssueResponse) => void
}

function severityVariant(severity: Ambiguity["severity"]) {
  if (severity === "high") return "destructive" as const
  if (severity === "medium") return "warning" as const
  return "secondary" as const
}

function severityLabel(severity: Ambiguity["severity"]) {
  return severity.charAt(0).toUpperCase() + severity.slice(1)
}

function AmbiguityCard({
  documentId,
  ambiguity,
  requirementText,
  onResolved,
}: {
  documentId: string
  ambiguity: Ambiguity
  requirementText: string | undefined
  onResolved: (result: ResolveIssueResponse) => void
}) {
  const [selected, setSelected] = useState<string[]>(ambiguity.selectedOptions)
  const [busy, setBusy] = useState(false)

  function toggle(label: string) {
    setSelected((current) =>
      current.includes(label)
        ? current.filter((item) => item !== label)
        : [...current, label]
    )
  }

  async function resolve() {
    if (selected.length === 0 || busy || ambiguity.isResolved) {
      return
    }

    setBusy(true)
    try {
      const result = await resolveAmbiguity(documentId, ambiguity.id, selected)
      onResolved(result)
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not resolve this issue.")
      setBusy(false)
    }
  }

  return (
    <Card className={ambiguity.isResolved ? "opacity-70" : "transition-opacity duration-300"}>
      <CardHeader>
        <div className="flex flex-wrap items-center gap-2">
          <TriangleAlert
            className={
              ambiguity.severity === "high"
                ? "size-4 text-destructive"
                : ambiguity.severity === "medium"
                  ? "size-4 text-warning"
                  : "size-4 text-muted-foreground"
            }
            aria-hidden="true"
          />
          <CardTitle>Ambiguous Requirement</CardTitle>
          <Badge variant={severityVariant(ambiguity.severity)}>
            {severityLabel(ambiguity.severity)}
          </Badge>
          {ambiguity.isResolved ? <Badge variant="secondary">Resolved</Badge> : null}
        </div>
      </CardHeader>
      <CardContent className="flex flex-col gap-4">
        <p className="text-body">
          <span className="font-medium">{ambiguity.requirementId}:</span>{" "}
          {requirementText ? `“${requirementText}”` : "Requirement text unavailable."}
        </p>
        <p className="text-body text-muted-foreground">Why: {ambiguity.issue}</p>
        <fieldset disabled={ambiguity.isResolved || busy} className="flex flex-col gap-2">
          <legend className="sr-only">Clarification options</legend>
          <ul className="flex flex-wrap gap-x-4 gap-y-2">
            {ambiguity.questions.map((question, index) => (
              <li key={`${index}-${question.text}`}>
                <label className="inline-flex items-center gap-2 text-body">
                  <input
                    type="checkbox"
                    checked={selected.includes(question.text)}
                    onChange={() => toggle(question.text)}
                    className="size-4 rounded-sm border-input accent-primary focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none"
                  />
                  {question.text}
                </label>
              </li>
            ))}
          </ul>
        </fieldset>
      </CardContent>
      {ambiguity.isResolved ? null : (
        <CardFooter>
          <Button
            type="button"
            disabled={selected.length === 0 || busy}
            onClick={() => void resolve()}
          >
            {busy ? "Resolving…" : "Resolve"}
          </Button>
        </CardFooter>
      )}
    </Card>
  )
}

export function AmbiguitiesList({
  documentId,
  ambiguities,
  requirements,
  onResolved,
}: AmbiguitiesListProps) {
  if (ambiguities.length === 0) {
    return (
      <EmptyState
        icon={<TriangleAlert className="size-8" />}
        title="No ambiguities found"
        description="The extracted requirements look specific enough that the model did not flag any undefined behavior."
      />
    )
  }

  const textById = new Map(
    requirements.map((requirement) => [requirement.id, requirement.text])
  )
  const active = ambiguities.filter((item) => !item.isResolved)
  const resolved = ambiguities.filter((item) => item.isResolved)

  return (
    <div className="flex flex-col gap-6">
      {active.length === 0 ? (
        <EmptyState
          icon={<TriangleAlert className="size-8" />}
          title="All ambiguities resolved"
          description="Clarifications are saved on the requirement text. Check the Requirements tab for the updated wording."
        />
      ) : (
        <ul className="flex flex-col gap-3">
          {active.map((ambiguity) => (
            <li key={ambiguity.id}>
              <AmbiguityCard
                documentId={documentId}
                ambiguity={ambiguity}
                requirementText={textById.get(ambiguity.requirementId)}
                onResolved={onResolved}
              />
            </li>
          ))}
        </ul>
      )}

      {resolved.length > 0 ? (
        <section className="flex flex-col gap-3">
          <h3 className="text-label font-medium text-muted-foreground">Resolved</h3>
          <ul className="flex flex-col gap-3">
            {resolved.map((ambiguity) => (
              <li key={ambiguity.id} className="motion-safe:transition-opacity motion-safe:duration-300">
                <AmbiguityCard
                  documentId={documentId}
                  ambiguity={ambiguity}
                  requirementText={textById.get(ambiguity.requirementId)}
                  onResolved={onResolved}
                />
              </li>
            ))}
          </ul>
        </section>
      ) : null}
    </div>
  )
}
