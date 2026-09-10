import type { UploadDocumentResponse } from "@/lib/api"
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"

type ParseResultProps = {
  result: UploadDocumentResponse
}

export function ParseResult({ result }: ParseResultProps) {
  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-heading">Extracted text</CardTitle>
        <CardDescription>
          Document {result.id} · {result.extractedTextLength.toLocaleString()}{" "}
          characters.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <pre className="max-h-80 overflow-auto rounded-lg bg-muted p-4 text-body whitespace-pre-wrap text-foreground">
          {result.extractedTextPreview}
          {result.extractedTextLength > result.extractedTextPreview.length
            ? "…"
            : ""}
        </pre>
      </CardContent>
    </Card>
  )
}
