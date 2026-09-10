import { useCallback, useRef, useState } from "react"

import {
  ApiError,
  detectAmbiguities,
  detectContradictions,
  extractRequirements,
  scoreQuality,
  uploadPdf,
  uploadText,
  type DetectAmbiguitiesResponse,
  type DetectContradictionsResponse,
  type ExtractRequirementsResponse,
  type QualityScore,
  type ResolveIssueResponse,
  type UploadDocumentResponse,
} from "@/lib/api"

export type PipelineState =
  | { status: "idle" }
  | { status: "parsing" }
  | { status: "extracting"; parse: UploadDocumentResponse }
  | {
      status: "detecting"
      parse: UploadDocumentResponse
      extraction: ExtractRequirementsResponse
    }
  | {
      status: "contradicting"
      parse: UploadDocumentResponse
      extraction: ExtractRequirementsResponse
      ambiguities: DetectAmbiguitiesResponse
    }
  | {
      status: "scoring"
      parse: UploadDocumentResponse
      extraction: ExtractRequirementsResponse
      ambiguities: DetectAmbiguitiesResponse
      contradictions: DetectContradictionsResponse
    }
  | {
      status: "success"
      parse: UploadDocumentResponse
      extraction: ExtractRequirementsResponse
      ambiguities: DetectAmbiguitiesResponse
      contradictions: DetectContradictionsResponse
      quality: QualityScore
    }
  | { status: "parse-error"; message: string }
  | {
      status: "extract-error"
      parse: UploadDocumentResponse
      message: string
    }
  | {
      status: "detect-error"
      parse: UploadDocumentResponse
      extraction: ExtractRequirementsResponse
      message: string
    }
  | {
      status: "contradiction-error"
      parse: UploadDocumentResponse
      extraction: ExtractRequirementsResponse
      ambiguities: DetectAmbiguitiesResponse
      message: string
    }
  | {
      status: "score-error"
      parse: UploadDocumentResponse
      extraction: ExtractRequirementsResponse
      ambiguities: DetectAmbiguitiesResponse
      contradictions: DetectContradictionsResponse
      message: string
    }

type LastAttempt =
  | { kind: "pdf"; file: File }
  | { kind: "text"; text: string }

function toMessage(error: unknown): string {
  return error instanceof ApiError
    ? error.message
    : "Something went wrong. Please try again."
}

export function useDocumentPipeline() {
  const [state, setState] = useState<PipelineState>({ status: "idle" })
  const lastAttempt = useRef<LastAttempt | null>(null)

  const score = useCallback(
    async (
      parse: UploadDocumentResponse,
      extraction: ExtractRequirementsResponse,
      ambiguities: DetectAmbiguitiesResponse,
      contradictions: DetectContradictionsResponse
    ) => {
      setState({
        status: "scoring",
        parse,
        extraction,
        ambiguities,
        contradictions,
      })
      try {
        const quality = await scoreQuality(parse.id)
        setState({
          status: "success",
          parse,
          extraction,
          ambiguities,
          contradictions,
          quality,
        })
      } catch (error) {
        setState({
          status: "score-error",
          parse,
          extraction,
          ambiguities,
          contradictions,
          message: toMessage(error),
        })
      }
    },
    []
  )

  const contradict = useCallback(
    async (
      parse: UploadDocumentResponse,
      extraction: ExtractRequirementsResponse,
      ambiguities: DetectAmbiguitiesResponse
    ) => {
      setState({ status: "contradicting", parse, extraction, ambiguities })
      try {
        const contradictions = await detectContradictions(parse.id)
        await score(parse, extraction, ambiguities, contradictions)
      } catch (error) {
        setState({
          status: "contradiction-error",
          parse,
          extraction,
          ambiguities,
          message: toMessage(error),
        })
      }
    },
    [score]
  )

  const detect = useCallback(
    async (
      parse: UploadDocumentResponse,
      extraction: ExtractRequirementsResponse
    ) => {
      setState({ status: "detecting", parse, extraction })
      try {
        const ambiguities = await detectAmbiguities(parse.id)
        await contradict(parse, extraction, ambiguities)
      } catch (error) {
        setState({
          status: "detect-error",
          parse,
          extraction,
          message: toMessage(error),
        })
      }
    },
    [contradict]
  )

  const extract = useCallback(
    async (parse: UploadDocumentResponse) => {
      setState({ status: "extracting", parse })
      try {
        const extraction = await extractRequirements(parse.id)
        await detect(parse, extraction)
      } catch (error) {
        setState({ status: "extract-error", parse, message: toMessage(error) })
      }
    },
    [detect]
  )

  const run = useCallback(
    async (attempt: LastAttempt) => {
      lastAttempt.current = attempt
      setState({ status: "parsing" })
      try {
        const parse =
          attempt.kind === "pdf"
            ? await uploadPdf(attempt.file)
            : await uploadText(attempt.text)
        await extract(parse)
      } catch (error) {
        setState({ status: "parse-error", message: toMessage(error) })
      }
    },
    [extract]
  )

  const submitPdf = useCallback(
    (file: File) => {
      void run({ kind: "pdf", file })
    },
    [run]
  )

  const submitText = useCallback(
    (text: string) => {
      void run({ kind: "text", text })
    },
    [run]
  )

  const applyResolution = useCallback((result: ResolveIssueResponse) => {
    setState((current) => {
      if (current.status !== "success") {
        return current
      }

      return {
        ...current,
        extraction: {
          ...current.extraction,
          requirements: result.requirements,
        },
        ambiguities: {
          ...current.ambiguities,
          ambiguities: result.ambiguities,
        },
        contradictions: {
          ...current.contradictions,
          contradictions: result.contradictions,
        },
        quality: result.quality,
      }
    })
  }, [])

  const retry = useCallback(() => {
    if (state.status === "extract-error") {
      void extract(state.parse)
      return
    }

    if (state.status === "detect-error") {
      void detect(state.parse, state.extraction)
      return
    }

    if (state.status === "contradiction-error") {
      void contradict(state.parse, state.extraction, state.ambiguities)
      return
    }

    if (state.status === "score-error") {
      void score(
        state.parse,
        state.extraction,
        state.ambiguities,
        state.contradictions
      )
      return
    }

    if (lastAttempt.current) {
      void run(lastAttempt.current)
    }
  }, [contradict, detect, extract, run, score, state])

  return {
    state,
    submitPdf,
    submitText,
    retry,
    applyResolution,
    isBusy:
      state.status === "parsing" ||
      state.status === "extracting" ||
      state.status === "detecting" ||
      state.status === "contradicting" ||
      state.status === "scoring",
  }
}
