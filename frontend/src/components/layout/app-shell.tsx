import type { ReactNode } from "react"

import { Header } from "@/components/layout/header"
import { Toaster } from "@/components/ui/sonner"

type AppShellProps = {
  children: ReactNode
  onSelectPdf: (file: File) => void
  onSubmitText: (text: string) => void
  isBusy: boolean
}

export function AppShell({
  children,
  onSelectPdf,
  onSubmitText,
  isBusy,
}: AppShellProps) {
  return (
    <div className="min-h-svh bg-background text-foreground">
      <Header
        onSelectPdf={onSelectPdf}
        onSubmitText={onSubmitText}
        isBusy={isBusy}
      />
      <main
        className="mx-auto w-full max-w-5xl px-4 py-6 md:px-6 md:py-8"
        aria-busy={isBusy || undefined}
      >
        {children}
      </main>
      <Toaster />
    </div>
  )
}
