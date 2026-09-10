import { Skeleton } from "@/components/ui/skeleton"

type IssueListSkeletonProps = {
  label: string
}

export function IssueListSkeleton({ label }: IssueListSkeletonProps) {
  return (
    <div className="flex flex-col gap-4" aria-busy="true" aria-live="polite">
      <span className="sr-only">{label}</span>
      <div className="flex gap-2">
        <Skeleton className="h-8 w-28" />
        <Skeleton className="h-8 w-28" />
        <Skeleton className="h-8 w-32" />
      </div>
      <div className="flex flex-col gap-3 rounded-xl border p-4">
        <Skeleton className="h-5 w-48" />
        <Skeleton className="h-4 w-full" />
        <Skeleton className="h-4 w-5/6" />
        <Skeleton className="h-8 w-24" />
      </div>
      <div className="flex flex-col gap-3 rounded-xl border p-4">
        <Skeleton className="h-5 w-40" />
        <Skeleton className="h-4 w-full" />
        <Skeleton className="h-4 w-2/3" />
      </div>
    </div>
  )
}
