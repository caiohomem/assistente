"use client";

import {
  Workflow,
  WorkflowSummary,
  WorkflowExecution,
  WorkflowExecutionSummary,
  WorkflowStatus,
  WorkflowExecutionStatus,
  TriggerType,
  CreateWorkflowRequest,
  CreateWorkflowResult,
  ExecuteWorkflowRequest,
  ExecuteWorkflowResult,
  ApproveStepResult,
} from "@/lib/types/workflow";
import { getApiBaseUrl, getBffSession } from "@/lib/bff";

// Map numeric enum values to string values
const workflowStatusMap: Record<number | string, WorkflowStatus> = {
  1: "Draft",
  2: "Active",
  3: "Paused",
  4: "Archived",
  "Draft": "Draft",
  "Active": "Active",
  "Paused": "Paused",
  "Archived": "Archived",
};

const executionStatusMap: Record<number | string, WorkflowExecutionStatus> = {
  1: "Pending",
  2: "Running",
  3: "WaitingApproval",
  4: "Completed",
  5: "Failed",
  6: "Cancelled",
  "Pending": "Pending",
  "Running": "Running",
  "WaitingApproval": "WaitingApproval",
  "Completed": "Completed",
  "Failed": "Failed",
  "Cancelled": "Cancelled",
};

const triggerTypeMap: Record<number | string, TriggerType> = {
  1: "Manual",
  2: "Schedule",
  3: "Event",
  4: "Webhook",
  "Manual": "Manual",
  "Scheduled": "Schedule",
  "Schedule": "Schedule",
  "EventBased": "Event",
  "Event": "Event",
  "Webhook": "Webhook",
};

function mapWorkflowStatus(status: number | string): WorkflowStatus {
  return workflowStatusMap[status] || "Draft";
}

function mapExecutionStatus(status: number | string): WorkflowExecutionStatus {
  return executionStatusMap[status] || "Pending";
}

function mapTriggerType(type: number | string): TriggerType {
  return triggerTypeMap[type] || "Webhook";
}

/**
 * Lists all workflows for the current user.
 */
type RawWorkflowSummary = {
  workflowId?: string
  WorkflowId?: string
  name?: string
  Name?: string
  description?: string
  Description?: string
  triggerType?: number | string
  TriggerType?: number | string
  status?: number | string
  Status?: number | string
  specVersion?: number
  SpecVersion?: number
  createdAt?: string
  CreatedAt?: string
  updatedAt?: string
  UpdatedAt?: string
}

type RawWorkflow = RawWorkflowSummary & {
  ownerUserId?: string
  OwnerUserId?: string
  trigger?: {
    type?: number | string
    cronExpression?: string
    eventName?: string
    configJson?: string
  }
  Trigger?: {
    Type?: number | string
    CronExpression?: string
    EventName?: string
    ConfigJson?: string
  }
  specJson?: string
  SpecJson?: string
  n8nWorkflowId?: string
  N8nWorkflowId?: string
}

type RawExecutionSummary = {
  executionId?: string
  ExecutionId?: string
  workflowId?: string
  WorkflowId?: string
  workflowName?: string
  WorkflowName?: string
  status?: number | string
  Status?: number | string
  startedAt?: string
  StartedAt?: string
  completedAt?: string
  CompletedAt?: string
}

type RawExecution = RawExecutionSummary & {
  ownerUserId?: string
  OwnerUserId?: string
  specVersionUsed?: number
  SpecVersionUsed?: number
  inputJson?: string
  InputJson?: string
  outputJson?: string
  OutputJson?: string
  n8nExecutionId?: string
  N8nExecutionId?: string
  errorMessage?: string
  ErrorMessage?: string
  currentStepIndex?: number
  CurrentStepIndex?: number
}

type RawExecuteResponse = {
  success?: boolean
  executionId?: string
  ExecutionId?: string
  errorMessage?: string
  message?: string
}

export async function listWorkflowsClient(
  status?: WorkflowStatus
): Promise<WorkflowSummary[]> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const queryParams = new URLSearchParams();
  if (status) queryParams.set("status", status);

  const url = `${apiBase}/api/workflows${queryParams.toString() ? `?${queryParams.toString()}` : ""}`;

  const res = await fetch(url, {
    method: "GET",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
  });

  if (!res.ok) {
    const data = await res.json().catch(() => ({}));
    throw new Error(data.message || `Request failed: ${res.status}`);
  }

  const data = await res.json();
  return (data || []).map((w: RawWorkflowSummary) => ({
    workflowId: w.workflowId ?? w.WorkflowId ?? "",
    name: w.name ?? w.Name ?? "",
    description: w.description ?? w.Description ?? "",
    triggerType: mapTriggerType(w.triggerType ?? w.TriggerType ?? "Manual"),
    status: mapWorkflowStatus(w.status ?? w.Status ?? "Draft"),
    specVersion: w.specVersion ?? w.SpecVersion ?? 1,
    createdAt: w.createdAt ?? w.CreatedAt ?? "",
    updatedAt: w.updatedAt ?? w.UpdatedAt ?? "",
  }));
}

/**
 * Gets a workflow by ID.
 */
export async function getWorkflowByIdClient(workflowId: string): Promise<Workflow> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/workflows/${workflowId}`, {
    method: "GET",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
  });

  if (!res.ok) {
    const data = await res.json().catch(() => ({}));
    throw new Error(data.message || `Request failed: ${res.status}`);
  }

  const w = (await res.json()) as RawWorkflow;
  return {
    workflowId: w.workflowId ?? w.WorkflowId ?? "",
    ownerUserId: w.ownerUserId ?? w.OwnerUserId ?? "",
    name: w.name ?? w.Name ?? "",
    description: w.description ?? w.Description ?? "",
    trigger: {
      type: mapTriggerType(w.trigger?.type ?? w.Trigger?.Type ?? "Manual"),
      cronExpression: w.trigger?.cronExpression ?? w.Trigger?.CronExpression,
      eventName: w.trigger?.eventName ?? w.Trigger?.EventName,
      configJson: w.trigger?.configJson ?? w.Trigger?.ConfigJson,
    },
    specJson: w.specJson ?? w.SpecJson ?? "{}",
    specVersion: w.specVersion ?? w.SpecVersion ?? 1,
    n8nWorkflowId: w.n8nWorkflowId ?? w.N8nWorkflowId ?? "",
    status: mapWorkflowStatus(w.status ?? w.Status ?? "Draft"),
    createdAt: w.createdAt ?? w.CreatedAt ?? "",
    updatedAt: w.updatedAt ?? w.UpdatedAt ?? "",
  };
}

/**
 * Creates a new workflow.
 */
export async function createWorkflowClient(
  request: CreateWorkflowRequest
): Promise<CreateWorkflowResult> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/workflows`, {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
    body: JSON.stringify({
      specJson: request.specJson,
      activateImmediately: request.activateImmediately || false,
    }),
  });

  const data = await res.json().catch(() => ({}));

  if (!res.ok) {
    return {
      success: false,
      errorMessage: data.message || data.errorMessage || `Request failed: ${res.status}`,
    };
  }

  return {
    success: true,
    workflowId: data.workflowId || data.WorkflowId,
  };
}

/**
 * Executes a workflow.
 */
export async function executeWorkflowClient(
  workflowId: string,
  request?: ExecuteWorkflowRequest
): Promise<ExecuteWorkflowResult> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/workflows/${workflowId}/execute`, {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
    body: JSON.stringify(request || {}),
  });

  const data = (await res.json().catch(() => ({}))) as RawExecuteResponse;

  if (!res.ok) {
    return {
      success: false,
      errorMessage: data.message || data.errorMessage || `Request failed: ${res.status}`,
    };
  }

  return {
    success: data.success ?? true,
    executionId: data.executionId ?? data.ExecutionId ?? "",
  };
}

/**
 * Lists workflow executions.
 */
export async function listExecutionsClient(
  workflowId?: string,
  limit: number = 50
): Promise<WorkflowExecutionSummary[]> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const queryParams = new URLSearchParams();
  if (workflowId) queryParams.set("workflowId", workflowId);
  queryParams.set("limit", limit.toString());

  const url = `${apiBase}/api/workflows/executions${queryParams.toString() ? `?${queryParams.toString()}` : ""}`;

  const res = await fetch(url, {
    method: "GET",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
  });

  if (!res.ok) {
    const data = await res.json().catch(() => ({}));
    throw new Error(data.message || `Request failed: ${res.status}`);
  }

  const data = await res.json();
  return (data || []).map((e: RawExecutionSummary) => ({
    executionId: e.executionId ?? e.ExecutionId ?? "",
    workflowId: e.workflowId ?? e.WorkflowId ?? "",
    workflowName: e.workflowName ?? e.WorkflowName ?? "Workflow",
    status: mapExecutionStatus(e.status ?? e.Status ?? "Pending"),
    startedAt: e.startedAt ?? e.StartedAt ?? "",
    completedAt: e.completedAt ?? e.CompletedAt ?? "",
  }));
}

/**
 * Gets execution details by ID.
 */
export async function getExecutionByIdClient(executionId: string): Promise<WorkflowExecution> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/workflows/executions/${executionId}`, {
    method: "GET",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
  });

  if (!res.ok) {
    const data = await res.json().catch(() => ({}));
    throw new Error(data.message || `Request failed: ${res.status}`);
  }

  const e = (await res.json()) as RawExecution;
  return {
    executionId: e.executionId ?? e.ExecutionId ?? "",
    workflowId: e.workflowId ?? e.WorkflowId ?? "",
    ownerUserId: e.ownerUserId ?? e.OwnerUserId ?? "",
    specVersionUsed: e.specVersionUsed ?? e.SpecVersionUsed ?? 1,
    inputJson: e.inputJson ?? e.InputJson,
    outputJson: e.outputJson ?? e.OutputJson,
    status: mapExecutionStatus(e.status ?? e.Status ?? "Pending"),
    n8nExecutionId: e.n8nExecutionId ?? e.N8nExecutionId,
    errorMessage: e.errorMessage ?? e.ErrorMessage,
    currentStepIndex: e.currentStepIndex ?? e.CurrentStepIndex ?? 0,
    startedAt: e.startedAt ?? e.StartedAt ?? "",
    completedAt: e.completedAt ?? e.CompletedAt,
  };
}

/**
 * Lists pending approval requests.
 */
export async function getPendingApprovalsClient(): Promise<WorkflowExecution[]> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/workflows/pending-approvals`, {
    method: "GET",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
  });

  if (!res.ok) {
    const data = await res.json().catch(() => ({}));
    throw new Error(data.message || `Request failed: ${res.status}`);
  }

  const data = await res.json();
  return (data || []).map((e: RawExecution) => ({
    executionId: e.executionId ?? e.ExecutionId ?? "",
    workflowId: e.workflowId ?? e.WorkflowId ?? "",
    ownerUserId: e.ownerUserId ?? e.OwnerUserId ?? "",
    specVersionUsed: e.specVersionUsed ?? e.SpecVersionUsed ?? 1,
    inputJson: e.inputJson ?? e.InputJson,
    outputJson: e.outputJson ?? e.OutputJson,
    status: mapExecutionStatus(e.status ?? e.Status ?? "Pending"),
    n8nExecutionId: e.n8nExecutionId ?? e.N8nExecutionId,
    errorMessage: e.errorMessage ?? e.ErrorMessage,
    currentStepIndex: e.currentStepIndex ?? e.CurrentStepIndex ?? 0,
    startedAt: e.startedAt ?? e.StartedAt ?? "",
    completedAt: e.completedAt ?? e.CompletedAt,
  }));
}

/**
 * Approves a pending workflow step.
 */
export async function approveStepClient(executionId: string): Promise<ApproveStepResult> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/workflows/executions/${executionId}/approve`, {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
  });

  const data = (await res.json().catch(() => ({}))) as RawExecuteResponse;

  if (!res.ok) {
    return {
      success: false,
      errorMessage: data.message || data.errorMessage || `Request failed: ${res.status}`,
    };
  }

  return { success: data.success ?? true };
}

/**
 * Activates a workflow.
 */
export async function activateWorkflowClient(workflowId: string): Promise<void> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/workflows/${workflowId}/activate`, {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
  });

  if (!res.ok && res.status !== 204) {
    const data = await res.json().catch(() => ({}));
    throw new Error(data.message || `Request failed: ${res.status}`);
  }
}

/**
 * Pauses a workflow.
 */
export async function pauseWorkflowClient(workflowId: string): Promise<void> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/workflows/${workflowId}/pause`, {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
  });

  if (!res.ok && res.status !== 204) {
    const data = await res.json().catch(() => ({}));
    throw new Error(data.message || `Request failed: ${res.status}`);
  }
}

/**
 * Archives (soft deletes) a workflow.
 */
export async function archiveWorkflowClient(workflowId: string): Promise<void> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/workflows/${workflowId}`, {
    method: "DELETE",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
  });

  if (!res.ok && res.status !== 204) {
    const data = await res.json().catch(() => ({}));
    throw new Error(data.message || `Request failed: ${res.status}`);
  }
}
