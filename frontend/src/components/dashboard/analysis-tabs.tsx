import { ContradictionsList } from "@/components/contradictions/contradictions-list"
import { AmbiguitiesList } from "@/components/ambiguities/ambiguities-list"
import { RequirementsList } from "@/components/requirements/requirements-list"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import type { Ambiguity, Contradiction, Requirement, ResolveIssueResponse } from "@/lib/api"

type AnalysisTabsProps = {
  documentId: string
  requirements: Requirement[]
  ambiguities: Ambiguity[]
  contradictions: Contradiction[]
  onResolved: (result: ResolveIssueResponse) => void
  candidateCapReached?: boolean
  candidatePairsAboveThreshold?: number
  candidatePairsSentToLlm?: number
  similarityThreshold?: number
  neighborsPerRequirement?: number
}

export function AnalysisTabs({
  documentId,
  requirements,
  ambiguities,
  contradictions,
  onResolved,
  candidateCapReached,
  candidatePairsAboveThreshold,
  candidatePairsSentToLlm,
  similarityThreshold,
  neighborsPerRequirement,
}: AnalysisTabsProps) {
  const openContradictions = contradictions.filter((item) => !item.isResolved)
  const openAmbiguities = ambiguities.filter((item) => !item.isResolved)
  const defaultTab =
    openContradictions.length > 0
      ? "contradictions"
      : openAmbiguities.length > 0
        ? "ambiguities"
        : "requirements"

  return (
    <Tabs defaultValue={defaultTab} className="gap-4">
      <TabsList className="grid w-full grid-cols-3 md:w-fit">
        <TabsTrigger value="requirements">Requirements</TabsTrigger>
        <TabsTrigger value="ambiguities">Ambiguities</TabsTrigger>
        <TabsTrigger value="contradictions">Contradictions</TabsTrigger>
      </TabsList>

      <TabsContent value="requirements">
        <RequirementsList requirements={requirements} />
      </TabsContent>

      <TabsContent value="ambiguities">
        <AmbiguitiesList
          documentId={documentId}
          ambiguities={ambiguities}
          requirements={requirements}
          onResolved={onResolved}
        />
      </TabsContent>

      <TabsContent value="contradictions">
        <ContradictionsList
          documentId={documentId}
          contradictions={contradictions}
          requirements={requirements}
          onResolved={onResolved}
          candidateCapReached={candidateCapReached}
          candidatePairsAboveThreshold={candidatePairsAboveThreshold}
          candidatePairsSentToLlm={candidatePairsSentToLlm}
          similarityThreshold={similarityThreshold}
          neighborsPerRequirement={neighborsPerRequirement}
        />
      </TabsContent>
    </Tabs>
  )
}
