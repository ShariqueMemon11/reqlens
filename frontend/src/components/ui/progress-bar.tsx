import { cn } from "@/lib/utils"

type ProgressBarProps = {
  label: string
  value: number
  className?: string
}

export function ProgressBar({ label, value, className }: ProgressBarProps) {
  const clamped = Math.min(100, Math.max(0, value))
  const fillClass =
    clamped < 50 ? "bg-score-low" : clamped < 75 ? "bg-score-mid" : "bg-score-high"

  return (
    <div className={cn("flex flex-col gap-2", className)}>
      <div className="flex items-center justify-between gap-4">
        <span className="text-label font-medium text-muted-foreground">
          {label}
        </span>
        <span className="text-label tabular-nums text-muted-foreground">
          {clamped}%
        </span>
      </div>
      <div
        role="progressbar"
        aria-label={label}
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={clamped}
        className="h-2 overflow-hidden rounded-full bg-muted"
      >
        <div
          className={cn(
            "h-full rounded-full transition-[width] duration-300 ease-out",
            fillClass
          )}
          style={{ width: `${clamped}%` }}
        />
      </div>
    </div>
  )
}
