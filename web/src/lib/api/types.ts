"use client"

export interface ApiErrorResponse {
  message?: string | null
  error?: string | null
  title?: string | null
  detail?: string | null
  errors?:
    | Array<{ field?: string; message?: string | null }>
    | Array<string>
    | null
  [key: string]: unknown
}

export interface PaginatedResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
  totalPages: number
}

export const extractApiErrorMessage = (data: unknown): string | undefined => {
  if (typeof data === "string") {
    const trimmed = data.trim()
    return trimmed.length > 0 ? trimmed : undefined
  }

  if (typeof data === "object" && data !== null) {
    const payload = data as ApiErrorResponse

    // For ASP.NET validation errors, prioritize field error messages over generic title
    if (payload.errors && typeof payload.errors === "object" && !Array.isArray(payload.errors)) {
      const entries = Object.entries(payload.errors as Record<string, unknown>)
      const messages: string[] = []
      for (const [field, value] of entries) {
        if (Array.isArray(value) && value.length > 0 && typeof value[0] === "string") {
          messages.push(value[0])
        } else if (typeof value === "string" && value.trim().length > 0) {
          messages.push(value)
        }
      }
      if (messages.length > 0) {
        return messages.join(" ")
      }
    }

    if (payload.message && payload.message.trim().length > 0) {
      return payload.message
    }
    if (payload.error && payload.error.trim().length > 0) {
      return payload.error
    }
    if (payload.detail && payload.detail.trim().length > 0) {
      return payload.detail
    }
    // Only use title if no specific error messages found (ASP.NET generic title is not helpful)
    if (payload.title && payload.title.trim().length > 0 && payload.title !== "One or more validation errors occurred.") {
      return payload.title
    }
    if (Array.isArray(payload.errors) && payload.errors.length > 0) {
      const first = payload.errors[0]
      if (typeof first === "string") {
        return first
      }
      if (typeof first === "object" && first?.message) {
        return first.message
      }
    }
  }

  return undefined
}

export class ApiError extends Error {
  status: number
  payload?: unknown

  constructor(message: string, status: number, payload?: unknown) {
    super(message)
    this.name = "ApiError"
    this.status = status
    this.payload = payload
  }
}

export const extractApiFieldErrors = (data: unknown): Record<string, string> => {
  if (typeof data !== "object" || data === null) {
    return {}
  }

  const payload = data as ApiErrorResponse
  const fieldErrors: Record<string, string> = {}

  if (payload.errors && typeof payload.errors === "object" && !Array.isArray(payload.errors)) {
    const entries = Object.entries(payload.errors as Record<string, unknown>)
    for (const [field, value] of entries) {
      if (Array.isArray(value) && value.length > 0 && typeof value[0] === "string") {
        const camelField = field.length > 0 ? `${field[0].toLowerCase()}${field.slice(1)}` : field
        fieldErrors[camelField] = value[0]
        continue
      }
      if (typeof value === "string" && value.trim().length > 0) {
        const camelField = field.length > 0 ? `${field[0].toLowerCase()}${field.slice(1)}` : field
        fieldErrors[camelField] = value
      }
    }
  }

  return fieldErrors
}

const readResponseBody = async (res: Response): Promise<unknown> => {
  const contentType = res.headers.get("content-type") ?? ""
  if (contentType.includes("application/json") || contentType.includes("application/problem+json")) {
    try {
      return await res.json()
    } catch {
      return undefined
    }
  }

  try {
    const text = await res.text()
    return text.length > 0 ? text : undefined
  } catch {
    return undefined
  }
}

export const buildApiError = async (res: Response): Promise<Error> => {
  const payload = await readResponseBody(res)
  const fallback = `Request failed: ${res.status}`
  const message = extractApiErrorMessage(payload) ?? fallback
  return new Error(message)
}

export const throwIfErrorResponse = async (res: Response): Promise<void> => {
  if (res.ok) {
    return
  }
  throw await buildApiError(res)
}
