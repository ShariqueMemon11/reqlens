import { Card, CardContent } from "@/components/ui/card"
import { Skeleton } from "@/components/ui/skeleton"

type CountItem = {
  label: string
  value: string
}

type CountsRowProps = {
  items?: readonly CountItem[]
}

const idleItems: CountItem[] = [
  { label: "Requirements", value: "0" },
  { label: "Ambiguities", value: "0" },
  { label: "Contradictions", value: "0" },
  { label: "Questions", value: "0" },
]

export function CountsRow({ items = idleItems }: CountsRowProps) {
  return (
    <ul className="grid grid-cols-2 gap-3 md:grid-cols-4">
      {items.map((item) => (
        <li key={item.label}>
          <Card size="sm">
            <CardContent>
              <p className="text-label text-muted-foreground">{item.label}</p>
              <p className="font-heading text-display font-semibold tabular-nums">
                {item.value}
              </p>
            </CardContent>
          </Card>
        </li>
      ))}
    </ul>
  )
}

export function CountsRowSkeleton() {
  return (
    <ul
      className="grid grid-cols-2 gap-3 md:grid-cols-4"
      aria-busy="true"
      aria-label="Loading counts"
    >
      {idleItems.map((item) => (
        <li key={item.label}>
          <Card size="sm">
            <CardContent className="flex flex-col gap-2">
              <p className="text-label text-muted-foreground">{item.label}</p>
              <Skeleton className="h-8 w-12" />
            </CardContent>
          </Card>
        </li>
      ))}
    </ul>
  )
}
