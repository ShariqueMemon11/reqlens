const baseUrl = import.meta.env.VITE_API_URL ?? "http://localhost:5080"

export type UploadDocumentResponse = {
  id: string
  extractedTextLength: number
  extractedTextPreview: string
}

export type Requirement = {
  id: string
  text: string
  sourceLine: number
}

export type ExtractRequirementsResponse = {
  documentId: string
  requirements: Requirement[]
}

export type AmbiguityQuestion = {
  text: string
  optionType: string
}

export type Ambiguity = {
  id: string
  requirementId: string
  severity: "high" | "medium" | "low"
  issue: string
  questions: AmbiguityQuestion[]
  isResolved: boolean
  selectedOptions: string[]
}

export type DetectAmbiguitiesResponse = {
  documentId: string
  ambiguities: Ambiguity[]
}

export type Contradiction = {
  id: string
  requirementIds: string[]
  description: string
  resolutionQuestion: string
  isResolved: boolean
  chosenRequirementId: string | null
}

export type DetectContradictionsResponse = {
  documentId: string
  contradictions: Contradiction[]
  candidateCapReached: boolean
  candidatePairsAboveThreshold: number
  candidatePairsSentToLlm: number
  similarityThreshold: number
  neighborsPerRequirement: number
}

export type QualitySignals = {
  requirementCount: number
  vagueCount: number
  vagueScore: number
  hasActorCount: number
  actorScore: number
  hasMeasurableConstraintCount: number
  measurableConstraintCoverage: number
  unresolvedContradictionCount: number
  involvedInContradictionCount: number
  unansweredQuestionCount: number
  questionScore: number
  llmClarity: number
  llmSpecificity: number
}

export type QualityScore = {
  documentId: string
  overall: number
  completeness: number
  clarity: number
  testability: number
  consistency: number
  specificity: number
  signals: QualitySignals
}

export type ResolveIssueResponse = {
  documentId: string
  requirements: Requirement[]
  ambiguities: Ambiguity[]
  contradictions: Contradiction[]
  quality: QualityScore
}

export class ApiError extends Error {
  readonly status: number

  constructor(message: string, status: number) {
    super(message)
    this.name = "ApiError"
    this.status = status
  }
}

async function readErrorMessage(response: Response): Promise<string> {
  try {
    const body = (await response.json()) as {
      detail?: string
      title?: string
      message?: string
    }
    return body.detail ?? body.message ?? body.title ?? response.statusText
  } catch {
    return response.statusText || "Request failed"
  }
}

async function postDocument(form: FormData): Promise<UploadDocumentResponse> {
  let response: Response
  try {
    response = await fetch(`${baseUrl}/documents`, {
      method: "POST",
      body: form,
    })
  } catch {
    throw new ApiError(
      "Could not reach the API. Is the backend running at " + baseUrl + "?",
      0
    )
  }

  if (!response.ok) {
    throw new ApiError(await readErrorMessage(response), response.status)
  }

  return (await response.json()) as UploadDocumentResponse
}

export function uploadPdf(file: File): Promise<UploadDocumentResponse> {
  const form = new FormData()
  form.append("file", file)
  return postDocument(form)
}

export function uploadText(text: string): Promise<UploadDocumentResponse> {
  const form = new FormData()
  form.append("text", text)
  return postDocument(form)
}

export async function extractRequirements(
  documentId: string
): Promise<ExtractRequirementsResponse> {
  let response: Response
  try {
    response = await fetch(`${baseUrl}/documents/${documentId}/extract`, {
      method: "POST",
    })
  } catch {
    throw new ApiError(
      "Could not reach the API. Is the backend running at " + baseUrl + "?",
      0
    )
  }

  if (!response.ok) {
    throw new ApiError(await readErrorMessage(response), response.status)
  }

  return (await response.json()) as ExtractRequirementsResponse
}

export async function detectAmbiguities(
  documentId: string
): Promise<DetectAmbiguitiesResponse> {
  let response: Response
  try {
    response = await fetch(`${baseUrl}/documents/${documentId}/ambiguities`, {
      method: "POST",
    })
  } catch {
    throw new ApiError(
      "Could not reach the API. Is the backend running at " + baseUrl + "?",
      0
    )
  }

  if (!response.ok) {
    throw new ApiError(await readErrorMessage(response), response.status)
  }

  return (await response.json()) as DetectAmbiguitiesResponse
}

export async function detectContradictions(
  documentId: string
): Promise<DetectContradictionsResponse> {
  let response: Response
  try {
    response = await fetch(`${baseUrl}/documents/${documentId}/contradictions`, {
      method: "POST",
    })
  } catch {
    throw new ApiError(
      "Could not reach the API. Is the backend running at " + baseUrl + "?",
      0
    )
  }

  if (!response.ok) {
    throw new ApiError(await readErrorMessage(response), response.status)
  }

  return (await response.json()) as DetectContradictionsResponse
}

export async function scoreQuality(
  documentId: string
): Promise<QualityScore> {
  let response: Response
  try {
    response = await fetch(`${baseUrl}/documents/${documentId}/score`, {
      method: "POST",
    })
  } catch {
    throw new ApiError(
      "Could not reach the API. Is the backend running at " + baseUrl + "?",
      0
    )
  }

  if (!response.ok) {
    throw new ApiError(await readErrorMessage(response), response.status)
  }

  return (await response.json()) as QualityScore
}

export async function resolveAmbiguity(
  documentId: string,
  ambiguityId: string,
  selectedOptions: string[]
): Promise<ResolveIssueResponse> {
  return postJson(
    `${baseUrl}/documents/${documentId}/ambiguities/${ambiguityId}/resolve`,
    { selectedOptions }
  )
}

export async function resolveContradiction(
  documentId: string,
  contradictionId: string,
  chosenRequirementId: string
): Promise<ResolveIssueResponse> {
  return postJson(
    `${baseUrl}/documents/${documentId}/contradictions/${contradictionId}/resolve`,
    { chosenRequirementId }
  )
}

async function postJson<T>(url: string, body: unknown): Promise<T> {
  let response: Response
  try {
    response = await fetch(url, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    })
  } catch {
    throw new ApiError(
      "Could not reach the API. Is the backend running at " + baseUrl + "?",
      0
    )
  }

  if (!response.ok) {
    throw new ApiError(await readErrorMessage(response), response.status)
  }

  return (await response.json()) as T
}
