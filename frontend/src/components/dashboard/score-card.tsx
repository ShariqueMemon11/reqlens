import { Gauge } from "lucide-react"

import { Card, CardAction, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { EmptyState } from "@/components/ui/empty-state"
import { ProgressBar } from "@/components/ui/progress-bar"
import { Skeleton } from "@/components/ui/skeleton"
import type { QualityScore } from "@/lib/api"

const subScoreKeys = [
  { label: "Completeness", key: "completeness" },
  { label: "Clarity", key: "clarity" },
  { label: "Testability", key: "testability" },
  { label: "Consistency", key: "consistency" },
  { label: "Specificity", key: "specificity" },
] as const

type ScoreCardProps = {
  score?: QualityScore | null
  previousOverall?: number | null
  isUpdating?: boolean
}

export function ScoreCardSkeleton() {
  return (
    <Card aria-busy="true" aria-label="Scoring quality">
      <CardHeader className="border-b">
        <CardTitle className="text-heading">Requirement Quality</CardTitle>
        <CardAction>
          <Skeleton className="h-10 w-24" />
        </CardAction>
      </CardHeader>
      <CardContent className="flex flex-col gap-4 pt-4">
        {subScoreKeys.map((item) => (
          <div key={item.key} className="flex flex-col gap-2">
            <div className="flex items-center justify-between gap-4">
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-4 w-10" />
            </div>
            <Skeleton className="h-2 w-full" />
          </div>
        ))}
      </CardContent>
    </Card>
  )
}

export function ScoreCard({ score, previousOverall, isUpdating }: ScoreCardProps) {
  if (!score) {
    return (
      <Card>
        <CardHeader className="border-b">
          <CardTitle className="text-heading">Requirement Quality</CardTitle>
        </CardHeader>
        <CardContent className="pt-4">
          <EmptyState
            className="border-0 py-8"
            icon={<Gauge className="size-8" />}
            title="No score yet"
            description="Analyze a document to see completeness, clarity, testability, consistency, and specificity."
          />
        </CardContent>
      </Card>
    )
  }

  return (
    <Card
      aria-busy={isUpdating ? true : undefined}
      className={isUpdating ? "opacity-80" : undefined}
    >
      <CardHeader className="border-b">
        <CardTitle className="text-heading">Requirement Quality</CardTitle>
        <CardAction>
          <p className="font-heading text-display font-semibold tabular-nums text-primary">
            {score.overall}
            <span className="text-heading font-medium text-muted-foreground">
              {" "}
              / 100
            </span>
            {previousOverall != null && previousOverall !== score.overall ? (
              <span
                className={
                  score.overall > previousOverall
                    ? "ml-2 text-heading font-medium text-primary"
                    : "ml-2 text-heading font-medium text-destructive"
                }
              >
                {score.overall > previousOverall ? "+" : ""}
                {score.overall - previousOverall}
              </span>
            ) : null}
          </p>
        </CardAction>
      </CardHeader>
      <CardContent className="flex flex-col gap-4 pt-4">
        {subScoreKeys.map((item) => (
          <ProgressBar
            key={item.key}
            label={item.label}
            value={score[item.key]}
          />
        ))}
      </CardContent>
    </Card>
  )
}
