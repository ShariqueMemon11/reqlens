import { useState, type FormEvent } from "react"

import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Textarea } from "@/components/ui/textarea"

type PasteTextDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSubmit: (text: string) => void
  disabled?: boolean
}

export function PasteTextDialog({
  open,
  onOpenChange,
  onSubmit,
  disabled = false,
}: PasteTextDialogProps) {
  const [text, setText] = useState("")

  function handleOpenChange(nextOpen: boolean) {
    if (!nextOpen) {
      setText("")
    }
    onOpenChange(nextOpen)
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const trimmed = text.trim()
    if (!trimmed) {
      return
    }
    onSubmit(trimmed)
    handleOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent>
        <form className="flex flex-col gap-4" onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle>Paste text</DialogTitle>
            <DialogDescription>
              Paste requirements as plain text. PDF upload is the other option.
            </DialogDescription>
          </DialogHeader>
          <Textarea
            value={text}
            onChange={(event) => setText(event.target.value)}
            placeholder="Paste requirement text here…"
            rows={8}
            disabled={disabled}
            aria-label="Requirement text"
          />
          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => handleOpenChange(false)}
            >
              Cancel
            </Button>
            <Button type="submit" disabled={disabled || text.trim().length === 0}>
              Analyze
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
