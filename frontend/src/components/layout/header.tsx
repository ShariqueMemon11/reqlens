import { useRef, useState } from "react"
import { FileText, FileUp } from "lucide-react"
import { toast } from "sonner"

import { ThemeToggle } from "@/components/layout/theme-toggle"
import { Button } from "@/components/ui/button"
import { PasteTextDialog } from "@/components/upload/paste-text-dialog"

const maxUploadBytes = 10 * 1024 * 1024

type HeaderProps = {
  onSelectPdf: (file: File) => void
  onSubmitText: (text: string) => void
  isBusy: boolean
}

export function Header({ onSelectPdf, onSubmitText, isBusy }: HeaderProps) {
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [pasteOpen, setPasteOpen] = useState(false)

  function handlePdf(file: File) {
    const isPdf =
      file.type === "application/pdf" || file.name.toLowerCase().endsWith(".pdf")
    if (!isPdf) {
      toast.error("Please choose a PDF file.")
      return
    }
    if (file.size > maxUploadBytes) {
      toast.error("That PDF is larger than 10 MB.")
      return
    }
    onSelectPdf(file)
  }

  return (
    <header className="border-b bg-background">
      <div className="mx-auto flex w-full max-w-5xl flex-col gap-4 px-4 py-4 md:px-6">
        <div className="flex items-start justify-between gap-4">
          <div className="min-w-0">
            <h1 className="text-display font-semibold">ReqLens</h1>
            <p className="text-label text-muted-foreground">
              AI Requirements Analyst
            </p>
          </div>
          <ThemeToggle />
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <input
            ref={fileInputRef}
            type="file"
            accept="application/pdf,.pdf"
            className="sr-only"
            disabled={isBusy}
            onChange={(event) => {
              const file = event.target.files?.[0]
              event.target.value = ""
              if (file) {
                handlePdf(file)
              }
            }}
          />
          <Button
            type="button"
            disabled={isBusy}
            onClick={() => fileInputRef.current?.click()}
          >
            <FileUp />
            Upload PDF
          </Button>
          <span className="text-label text-muted-foreground">or</span>
          <Button
            type="button"
            variant="outline"
            disabled={isBusy}
            onClick={() => setPasteOpen(true)}
          >
            <FileText />
            Paste Text
          </Button>
        </div>
      </div>

      <PasteTextDialog
        open={pasteOpen}
        onOpenChange={setPasteOpen}
        onSubmit={onSubmitText}
        disabled={isBusy}
      />
    </header>
  )
}
