// Workflow types for frontend

export type WorkflowStatus = "Draft" | "Active" | "Paused" | "Archived";
export type WorkflowExecutionStatus = "Pending" | "Running" | "WaitingApproval" | "Completed" | "Failed" | "Cancelled";
export type TriggerType = "Manual" | "Schedule" | "Event" | "Webhook";

export interface WorkflowTrigger {
  type: TriggerType;
  cronExpression?: string;
  eventName?: string;
  configJson?: string;
}

export interface Workflow {
  workflowId: string;
  ownerUserId: string;
  name: string;
  description?: string;
  trigger: WorkflowTrigger;
  specJson: string;
  specVersion: number;
  n8nWorkflowId?: string;
  status: WorkflowStatus;
  createdAt: string;
  updatedAt: string;
}

export interface WorkflowSummary {
  workflowId: string;
  name: string;
  description?: string;
  triggerType: TriggerType;
  status: WorkflowStatus;
  specVersion: number;
  createdAt: string;
  updatedAt: string;
}

export interface WorkflowExecution {
  executionId: string;
  workflowId: string;
  ownerUserId: string;
  specVersionUsed: number;
  inputJson?: string;
  outputJson?: string;
  status: WorkflowExecutionStatus;
  n8nExecutionId?: string;
  errorMessage?: string;
  currentStepIndex?: number;
  startedAt: string;
  completedAt?: string;
}

export interface WorkflowExecutionSummary {
  executionId: string;
  workflowId: string;
  workflowName: string;
  status: WorkflowExecutionStatus;
  startedAt: string;
  completedAt?: string;
}

export interface CreateWorkflowRequest {
  specJson: object;
  activateImmediately?: boolean;
}

export interface CreateWorkflowResult {
  success: boolean;
  workflowId?: string;
  errorMessage?: string;
}

export interface ExecuteWorkflowRequest {
  inputJson?: string;
}

export interface ExecuteWorkflowResult {
  success: boolean;
  executionId?: string;
  errorMessage?: string;
}

export interface ApproveStepResult {
  success: boolean;
  errorMessage?: string;
}
