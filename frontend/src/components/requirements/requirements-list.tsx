import { FileText } from "lucide-react"

import { Badge } from "@/components/ui/badge"
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { EmptyState } from "@/components/ui/empty-state"
import type { Requirement } from "@/lib/api"

type RequirementsListProps = {
  requirements: Requirement[]
}

function isClarified(text: string) {
  return text.includes("(Clarified:")
}

export function RequirementsList({ requirements }: RequirementsListProps) {
  if (requirements.length === 0) {
    return (
      <EmptyState
        icon={<FileText className="size-8" />}
        title="No requirements found"
        description="The document was parsed, but the model did not extract any discrete requirement units."
      />
    )
  }

  return (
    <ul className="flex flex-col gap-3">
      {requirements.map((requirement) => (
        <li key={requirement.id}>
          <Card className={isClarified(requirement.text) ? "motion-safe:transition-colors" : undefined}>
            <CardHeader>
              <div className="flex flex-wrap items-center gap-2">
                <CardTitle className="font-sans text-body font-medium">
                  {requirement.id}
                </CardTitle>
                {isClarified(requirement.text) ? (
                  <Badge variant="secondary">Clarified</Badge>
                ) : null}
              </div>
              <CardDescription>Line {requirement.sourceLine}</CardDescription>
            </CardHeader>
            <CardContent>
              <p className="text-body">{requirement.text}</p>
            </CardContent>
          </Card>
        </li>
      ))}
    </ul>
  )
}
