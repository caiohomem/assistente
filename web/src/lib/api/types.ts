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

    if (payload.message && payload.message.trim().length > 0) {
      return payload.message
    }
    if (payload.error && payload.error.trim().length > 0) {
      return payload.error
    }
    if (payload.title && payload.title.trim().length > 0) {
      return payload.title
    }
    if (payload.detail && payload.detail.trim().length > 0) {
      return payload.detail
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

const readResponseBody = async (res: Response): Promise<unknown> => {
  const contentType = res.headers.get("content-type") ?? ""
  if (contentType.includes("application/json")) {
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
