import { AlertCircle } from "lucide-react"

import { Button } from "@/components/ui/button"
import { EmptyState } from "@/components/ui/empty-state"

type ParseErrorProps = {
  title?: string
  message: string
  onRetry: () => void
}

export function ParseError({
  title = "Could not parse document",
  message,
  onRetry,
}: ParseErrorProps) {
  return (
    <EmptyState
      icon={<AlertCircle className="size-8" />}
      title={title}
      description={message}
      action={
        <Button type="button" onClick={onRetry}>
          Retry
        </Button>
      }
    />
  )
}
