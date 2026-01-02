"use client";

import { useEffect, useState } from "react";
import { getBffSession } from "@/lib/bff";
import { LayoutWrapper } from "@/components/LayoutWrapper";
import { Button } from "@/components/ui/button";
import {
  listWorkflowsClient,
  listExecutionsClient,
  getPendingApprovalsClient,
  getWorkflowByIdClient,
  activateWorkflowClient,
  pauseWorkflowClient,
  executeWorkflowClient,
  approveStepClient,
} from "@/lib/api/workflowsApiClient";
import type {
  Workflow,
  WorkflowSummary,
  WorkflowExecutionSummary,
  WorkflowExecution,
} from "@/lib/types/workflow";
import {
  Play,
  Pause,
  CheckCircle2,
  XCircle,
  Clock,
  AlertCircle,
  Loader2,
  RefreshCw,
  Zap,
  FileJson,
  Activity,
  ChevronDown,
  ChevronUp,
  Check,
  X,
  Code,
  Calendar,
  Hash,
  Settings,
} from "lucide-react";
import { cn } from "@/lib/utils";

type TabType = "workflows" | "executions" | "approvals";

const statusColors: Record<string, { bg: string; text: string; icon: React.ReactNode }> = {
  Draft: { bg: "bg-muted", text: "text-muted-foreground", icon: <FileJson className="w-3 h-3" /> },
  Active: { bg: "bg-green-500/20", text: "text-green-500", icon: <CheckCircle2 className="w-3 h-3" /> },
  Paused: { bg: "bg-yellow-500/20", text: "text-yellow-500", icon: <Pause className="w-3 h-3" /> },
  Archived: { bg: "bg-muted", text: "text-muted-foreground", icon: <XCircle className="w-3 h-3" /> },
};

const executionStatusColors: Record<string, { bg: string; text: string; icon: React.ReactNode }> = {
  Pending: { bg: "bg-muted", text: "text-muted-foreground", icon: <Clock className="w-3 h-3" /> },
  Running: { bg: "bg-blue-500/20", text: "text-blue-500", icon: <Loader2 className="w-3 h-3 animate-spin" /> },
  WaitingApproval: { bg: "bg-yellow-500/20", text: "text-yellow-500", icon: <AlertCircle className="w-3 h-3" /> },
  Completed: { bg: "bg-green-500/20", text: "text-green-500", icon: <CheckCircle2 className="w-3 h-3" /> },
  Failed: { bg: "bg-destructive/20", text: "text-destructive", icon: <XCircle className="w-3 h-3" /> },
  Cancelled: { bg: "bg-muted", text: "text-muted-foreground", icon: <XCircle className="w-3 h-3" /> },
};

interface ExecuteModalProps {
  workflow: Workflow;
  onClose: () => void;
  onExecute: (inputJson: string) => Promise<void>;
  isLoading: boolean;
}

function ExecuteModal({ workflow, onClose, onExecute, isLoading }: ExecuteModalProps) {
  const [inputJson, setInputJson] = useState("{}");
  const [jsonError, setJsonError] = useState<string | null>(null);

  // Try to extract input schema from specJson
  const getInputSchema = () => {
    try {
      const spec = JSON.parse(workflow.specJson);
      return spec.inputs || spec.parameters || null;
    } catch {
      return null;
    }
  };

  const inputSchema = getInputSchema();

  const handleExecute = async () => {
    // Validate JSON
    try {
      JSON.parse(inputJson);
      setJsonError(null);
    } catch {
      setJsonError("JSON inválido");
      return;
    }
    await onExecute(inputJson);
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      {/* Backdrop */}
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onClose} />

      {/* Modal */}
      <div className="relative w-full max-w-lg mx-4 glass-card p-6 animate-scale-in">
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-lg font-semibold flex items-center gap-2">
            <Play className="w-5 h-5 text-primary" />
            Executar Workflow
          </h3>
          <Button variant="ghost" size="icon" onClick={onClose}>
            <X className="w-4 h-4" />
          </Button>
        </div>

        <div className="mb-4">
          <p className="text-sm text-muted-foreground mb-1">Workflow:</p>
          <p className="font-medium">{workflow.name}</p>
        </div>

        {/* Input Schema Info */}
        {inputSchema && (
          <div className="mb-4 p-3 bg-primary/5 rounded-lg border border-primary/20">
            <p className="text-xs text-primary font-medium mb-2">Parâmetros esperados:</p>
            <pre className="text-xs text-muted-foreground overflow-auto max-h-24">
              {JSON.stringify(inputSchema, null, 2)}
            </pre>
          </div>
        )}

        {/* Input JSON */}
        <div className="mb-4">
          <label className="block text-sm font-medium mb-2">
            Parâmetros de Entrada (JSON)
          </label>
          <textarea
            value={inputJson}
            onChange={(e) => {
              setInputJson(e.target.value);
              setJsonError(null);
            }}
            rows={6}
            className={cn(
              "w-full rounded-xl border bg-secondary/50 px-4 py-3 text-sm font-mono",
              "focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary/50",
              jsonError ? "border-destructive" : "border-border"
            )}
            placeholder='{"param1": "value1", "param2": "value2"}'
          />
          {jsonError && (
            <p className="text-xs text-destructive mt-1">{jsonError}</p>
          )}
        </div>

        {/* Actions */}
        <div className="flex gap-3">
          <Button variant="ghost" onClick={onClose} disabled={isLoading} className="flex-1">
            Cancelar
          </Button>
          <Button variant="glow" onClick={handleExecute} disabled={isLoading} className="flex-1">
            {isLoading ? (
              <>
                <Loader2 className="w-4 h-4 animate-spin mr-2" />
                Executando...
              </>
            ) : (
              <>
                <Play className="w-4 h-4 mr-2" />
                Executar
              </>
            )}
          </Button>
        </div>
      </div>
    </div>
  );
}

export default function WorkflowsPage() {
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<TabType>("workflows");
  const [workflows, setWorkflows] = useState<WorkflowSummary[]>([]);
  const [executions, setExecutions] = useState<WorkflowExecutionSummary[]>([]);
  const [pendingApprovals, setPendingApprovals] = useState<WorkflowExecution[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [actionLoading, setActionLoading] = useState<string | null>(null);

  // Expanded workflow details
  const [expandedWorkflowId, setExpandedWorkflowId] = useState<string | null>(null);
  const [expandedWorkflow, setExpandedWorkflow] = useState<Workflow | null>(null);
  const [loadingDetails, setLoadingDetails] = useState(false);

  // Execute modal
  const [executeModalWorkflow, setExecuteModalWorkflow] = useState<Workflow | null>(null);

  const loadData = async () => {
    try {
      setError(null);
      const [workflowsData, executionsData, approvalsData] = await Promise.all([
        listWorkflowsClient(),
        listExecutionsClient(undefined, 50),
        getPendingApprovalsClient(),
      ]);
      setWorkflows(workflowsData);
      setExecutions(executionsData);
      setPendingApprovals(approvalsData);
    } catch (e) {
      console.error("Error loading workflows data:", e);
      setError(e instanceof Error ? e.message : "Erro ao carregar dados");
    }
  };

  useEffect(() => {
    let isMounted = true;

    async function load() {
      try {
        const session = await getBffSession();
        if (!session.authenticated) {
          window.location.href = `/login?returnUrl=${encodeURIComponent("/workflows")}`;
          return;
        }

        await loadData();
      } finally {
        if (isMounted) setLoading(false);
      }
    }

    load();
    return () => {
      isMounted = false;
    };
  }, []);

  const handleToggleExpand = async (workflowId: string) => {
    if (expandedWorkflowId === workflowId) {
      // Collapse
      setExpandedWorkflowId(null);
      setExpandedWorkflow(null);
    } else {
      // Expand and load details
      setExpandedWorkflowId(workflowId);
      setLoadingDetails(true);
      try {
        const details = await getWorkflowByIdClient(workflowId);
        setExpandedWorkflow(details);
      } catch (e) {
        setError(e instanceof Error ? e.message : "Erro ao carregar detalhes");
      } finally {
        setLoadingDetails(false);
      }
    }
  };

  const handleActivate = async (workflowId: string, e: React.MouseEvent) => {
    e.stopPropagation();
    setActionLoading(workflowId);
    try {
      await activateWorkflowClient(workflowId);
      await loadData();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Erro ao ativar workflow");
    } finally {
      setActionLoading(null);
    }
  };

  const handlePause = async (workflowId: string, e: React.MouseEvent) => {
    e.stopPropagation();
    setActionLoading(workflowId);
    try {
      await pauseWorkflowClient(workflowId);
      await loadData();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Erro ao pausar workflow");
    } finally {
      setActionLoading(null);
    }
  };

  const handleOpenExecuteModal = async (workflowId: string, e: React.MouseEvent) => {
    e.stopPropagation();
    setActionLoading(workflowId);
    try {
      const details = await getWorkflowByIdClient(workflowId);
      setExecuteModalWorkflow(details);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Erro ao carregar workflow");
    } finally {
      setActionLoading(null);
    }
  };

  const handleExecute = async (inputJson: string) => {
    if (!executeModalWorkflow) return;
    setActionLoading(executeModalWorkflow.workflowId);
    try {
      const result = await executeWorkflowClient(executeModalWorkflow.workflowId, {
        inputJson: inputJson !== "{}" ? inputJson : undefined,
      });
      if (!result.success) {
        setError(result.errorMessage || "Erro ao executar workflow");
      } else {
        setExecuteModalWorkflow(null);
        await loadData();
        setActiveTab("executions");
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : "Erro ao executar workflow");
    } finally {
      setActionLoading(null);
    }
  };

  const handleApprove = async (executionId: string) => {
    setActionLoading(executionId);
    try {
      const result = await approveStepClient(executionId);
      if (!result.success) {
        setError(result.errorMessage || "Erro ao aprovar step");
      } else {
        await loadData();
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : "Erro ao aprovar step");
    } finally {
      setActionLoading(null);
    }
  };

  const formatDate = (dateStr: string) => {
    const date = new Date(dateStr);
    return date.toLocaleString("pt-BR", {
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  const formatDuration = (start: string, end?: string) => {
    const startDate = new Date(start);
    const endDate = end ? new Date(end) : new Date();
    const diffMs = endDate.getTime() - startDate.getTime();
    const diffSecs = Math.floor(diffMs / 1000);

    if (diffSecs < 60) return `${diffSecs}s`;
    const diffMins = Math.floor(diffSecs / 60);
    if (diffMins < 60) return `${diffMins}m ${diffSecs % 60}s`;
    const diffHours = Math.floor(diffMins / 60);
    return `${diffHours}h ${diffMins % 60}m`;
  };

  const formatSpecJson = (specJson: string) => {
    try {
      return JSON.stringify(JSON.parse(specJson), null, 2);
    } catch {
      return specJson;
    }
  };

  if (loading) {
    return (
      <LayoutWrapper
        title="Workflows"
        subtitle="Automações e fluxos de trabalho"
        activeTab="workflows"
      >
        <div className="flex items-center justify-center py-12">
          <Loader2 className="w-6 h-6 animate-spin text-primary mr-2" />
          <span className="text-muted-foreground">Carregando...</span>
        </div>
      </LayoutWrapper>
    );
  }

  const tabs = [
    { id: "workflows" as TabType, label: "Workflows", count: workflows.length },
    { id: "executions" as TabType, label: "Execuções", count: executions.length },
    { id: "approvals" as TabType, label: "Aprovações", count: pendingApprovals.length },
  ];

  return (
    <LayoutWrapper
      title="Workflows"
      subtitle="Automações e fluxos de trabalho"
      activeTab="workflows"
    >
      <div className="space-y-6">
        {/* Execute Modal */}
        {executeModalWorkflow && (
          <ExecuteModal
            workflow={executeModalWorkflow}
            onClose={() => setExecuteModalWorkflow(null)}
            onExecute={handleExecute}
            isLoading={actionLoading === executeModalWorkflow.workflowId}
          />
        )}

        {/* Error message */}
        {error && (
          <div className="glass-card p-4 bg-destructive/10 border-destructive/30">
            <div className="flex items-center gap-2">
              <AlertCircle className="w-4 h-4 text-destructive" />
              <span className="text-destructive text-sm">{error}</span>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setError(null)}
                className="ml-auto"
              >
                Fechar
              </Button>
            </div>
          </div>
        )}

        {/* Tabs */}
        <div className="glass-card p-1 flex gap-1">
          {tabs.map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={cn(
                "flex-1 px-4 py-2 rounded-lg text-sm font-medium transition-all duration-200 flex items-center justify-center gap-2",
                activeTab === tab.id
                  ? "bg-primary/15 text-primary"
                  : "text-muted-foreground hover:text-foreground hover:bg-secondary/50"
              )}
            >
              {tab.label}
              {tab.count > 0 && (
                <span
                  className={cn(
                    "px-2 py-0.5 rounded-full text-xs",
                    activeTab === tab.id ? "bg-primary/20" : "bg-secondary"
                  )}
                >
                  {tab.count}
                </span>
              )}
            </button>
          ))}
          <Button
            variant="ghost"
            size="icon"
            onClick={() => loadData()}
            className="ml-2"
            title="Atualizar"
          >
            <RefreshCw className="w-4 h-4" />
          </Button>
        </div>

        {/* Workflows Tab */}
        {activeTab === "workflows" && (
          <div className="space-y-4">
            {workflows.length === 0 ? (
              <div className="glass-card p-8 text-center">
                <Zap className="w-12 h-12 mx-auto mb-4 text-muted-foreground" />
                <h3 className="text-lg font-semibold mb-2">Nenhum workflow encontrado</h3>
                <p className="text-sm text-muted-foreground">
                  Workflows podem ser criados pelo Assistente IA ou via API.
                </p>
              </div>
            ) : (
              <div className="space-y-3">
                {workflows.map((workflow) => {
                  const status = statusColors[workflow.status] || statusColors.Draft;
                  const isLoading = actionLoading === workflow.workflowId;
                  const isExpanded = expandedWorkflowId === workflow.workflowId;

                  return (
                    <div key={workflow.workflowId} className="glass-card overflow-hidden">
                      {/* Header - Clickable to expand */}
                      <div
                        className="p-4 cursor-pointer hover:bg-secondary/30 transition-colors"
                        onClick={() => handleToggleExpand(workflow.workflowId)}
                      >
                        <div className="flex items-start justify-between gap-4">
                          <div className="flex-1 min-w-0">
                            <div className="flex items-center gap-2 mb-1">
                              <h3 className="font-semibold truncate">{workflow.name}</h3>
                              <span
                                className={cn(
                                  "inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs",
                                  status.bg,
                                  status.text
                                )}
                              >
                                {status.icon}
                                {workflow.status}
                              </span>
                            </div>
                            {workflow.description && (
                              <p className="text-sm text-muted-foreground truncate mb-2">
                                {workflow.description}
                              </p>
                            )}
                            <div className="flex items-center gap-4 text-xs text-muted-foreground">
                              <span className="flex items-center gap-1">
                                <Settings className="w-3 h-3" />
                                {workflow.triggerType}
                              </span>
                              <span className="flex items-center gap-1">
                                <Hash className="w-3 h-3" />
                                v{workflow.specVersion}
                              </span>
                              <span className="flex items-center gap-1">
                                <Calendar className="w-3 h-3" />
                                {formatDate(workflow.createdAt)}
                              </span>
                            </div>
                          </div>

                          <div className="flex items-center gap-2">
                            {workflow.status === "Active" && (
                              <>
                                <Button
                                  variant="glow"
                                  size="sm"
                                  onClick={(e) => handleOpenExecuteModal(workflow.workflowId, e)}
                                  disabled={isLoading}
                                >
                                  {isLoading ? (
                                    <Loader2 className="w-4 h-4 animate-spin" />
                                  ) : (
                                    <Play className="w-4 h-4" />
                                  )}
                                  <span className="ml-1">Executar</span>
                                </Button>
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  onClick={(e) => handlePause(workflow.workflowId, e)}
                                  disabled={isLoading}
                                  title="Pausar"
                                >
                                  <Pause className="w-4 h-4" />
                                </Button>
                              </>
                            )}
                            {workflow.status === "Paused" && (
                              <Button
                                variant="glass"
                                size="sm"
                                onClick={(e) => handleActivate(workflow.workflowId, e)}
                                disabled={isLoading}
                              >
                                {isLoading ? (
                                  <Loader2 className="w-4 h-4 animate-spin" />
                                ) : (
                                  <Play className="w-4 h-4" />
                                )}
                                <span className="ml-1">Ativar</span>
                              </Button>
                            )}
                            {workflow.status === "Draft" && (
                              <Button
                                variant="glass"
                                size="sm"
                                onClick={(e) => handleActivate(workflow.workflowId, e)}
                                disabled={isLoading}
                              >
                                {isLoading ? (
                                  <Loader2 className="w-4 h-4 animate-spin" />
                                ) : (
                                  <Zap className="w-4 h-4" />
                                )}
                                <span className="ml-1">Ativar</span>
                              </Button>
                            )}
                            {isExpanded ? (
                              <ChevronUp className="w-4 h-4 text-muted-foreground" />
                            ) : (
                              <ChevronDown className="w-4 h-4 text-muted-foreground" />
                            )}
                          </div>
                        </div>
                      </div>

                      {/* Expanded Details */}
                      {isExpanded && (
                        <div className="border-t border-border bg-secondary/20 p-4 animate-slide-up">
                          {loadingDetails ? (
                            <div className="flex items-center justify-center py-4">
                              <Loader2 className="w-5 h-5 animate-spin text-primary mr-2" />
                              <span className="text-sm text-muted-foreground">
                                Carregando detalhes...
                              </span>
                            </div>
                          ) : expandedWorkflow ? (
                            <div className="space-y-4">
                              {/* Workflow Info Grid */}
                              <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                                <div>
                                  <p className="text-xs text-muted-foreground mb-1">ID</p>
                                  <p className="text-sm font-mono truncate" title={expandedWorkflow.workflowId}>
                                    {expandedWorkflow.workflowId.slice(0, 8)}...
                                  </p>
                                </div>
                                <div>
                                  <p className="text-xs text-muted-foreground mb-1">n8n ID</p>
                                  <p className="text-sm font-mono">
                                    {expandedWorkflow.n8nWorkflowId || "—"}
                                  </p>
                                </div>
                                <div>
                                  <p className="text-xs text-muted-foreground mb-1">Trigger</p>
                                  <p className="text-sm">{expandedWorkflow.trigger.type}</p>
                                </div>
                                <div>
                                  <p className="text-xs text-muted-foreground mb-1">Atualizado</p>
                                  <p className="text-sm">{formatDate(expandedWorkflow.updatedAt)}</p>
                                </div>
                              </div>

                              {/* Trigger Details */}
                              {(expandedWorkflow.trigger.cronExpression || expandedWorkflow.trigger.eventName) && (
                                <div className="p-3 bg-secondary/50 rounded-lg">
                                  <p className="text-xs font-medium text-muted-foreground mb-2 flex items-center gap-1">
                                    <Settings className="w-3 h-3" />
                                    Configuração do Trigger
                                  </p>
                                  <div className="grid grid-cols-2 gap-4 text-sm">
                                    {expandedWorkflow.trigger.cronExpression && (
                                      <div>
                                        <span className="text-muted-foreground">Cron: </span>
                                        <code className="text-primary">
                                          {expandedWorkflow.trigger.cronExpression}
                                        </code>
                                      </div>
                                    )}
                                    {expandedWorkflow.trigger.eventName && (
                                      <div>
                                        <span className="text-muted-foreground">Evento: </span>
                                        <code className="text-primary">
                                          {expandedWorkflow.trigger.eventName}
                                        </code>
                                      </div>
                                    )}
                                  </div>
                                </div>
                              )}

                              {/* Spec JSON */}
                              <div>
                                <p className="text-xs font-medium text-muted-foreground mb-2 flex items-center gap-1">
                                  <Code className="w-3 h-3" />
                                  Spec JSON (v{expandedWorkflow.specVersion})
                                </p>
                                <div className="relative">
                                  <pre className="p-4 bg-card rounded-lg border border-border text-xs font-mono overflow-auto max-h-64">
                                    {formatSpecJson(expandedWorkflow.specJson)}
                                  </pre>
                                  <Button
                                    variant="ghost"
                                    size="sm"
                                    className="absolute top-2 right-2"
                                    onClick={() => {
                                      navigator.clipboard.writeText(expandedWorkflow.specJson);
                                    }}
                                    title="Copiar"
                                  >
                                    <FileJson className="w-4 h-4" />
                                  </Button>
                                </div>
                              </div>
                            </div>
                          ) : (
                            <p className="text-sm text-muted-foreground text-center py-4">
                              Erro ao carregar detalhes
                            </p>
                          )}
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        )}

        {/* Executions Tab */}
        {activeTab === "executions" && (
          <div className="space-y-4">
            {executions.length === 0 ? (
              <div className="glass-card p-8 text-center">
                <Activity className="w-12 h-12 mx-auto mb-4 text-muted-foreground" />
                <h3 className="text-lg font-semibold mb-2">Nenhuma execução encontrada</h3>
                <p className="text-sm text-muted-foreground">
                  Execute um workflow para ver o histórico aqui.
                </p>
              </div>
            ) : (
              <div className="glass-card overflow-hidden">
                <table className="w-full">
                  <thead className="bg-secondary/50">
                    <tr>
                      <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                        Workflow
                      </th>
                      <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                        Status
                      </th>
                      <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                        Início
                      </th>
                      <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase">
                        Duração
                      </th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border">
                    {executions.map((execution) => {
                      const status =
                        executionStatusColors[execution.status] || executionStatusColors.Pending;

                      return (
                        <tr
                          key={execution.executionId}
                          className="hover:bg-secondary/30 transition-colors"
                        >
                          <td className="px-4 py-3">
                            <span className="font-medium">{execution.workflowName}</span>
                          </td>
                          <td className="px-4 py-3">
                            <span
                              className={cn(
                                "inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs",
                                status.bg,
                                status.text
                              )}
                            >
                              {status.icon}
                              {execution.status}
                            </span>
                          </td>
                          <td className="px-4 py-3 text-sm text-muted-foreground">
                            {formatDate(execution.startedAt)}
                          </td>
                          <td className="px-4 py-3 text-sm text-muted-foreground">
                            {formatDuration(execution.startedAt, execution.completedAt)}
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        )}

        {/* Approvals Tab */}
        {activeTab === "approvals" && (
          <div className="space-y-4">
            {pendingApprovals.length === 0 ? (
              <div className="glass-card p-8 text-center">
                <Check className="w-12 h-12 mx-auto mb-4 text-muted-foreground" />
                <h3 className="text-lg font-semibold mb-2">Nenhuma aprovação pendente</h3>
                <p className="text-sm text-muted-foreground">
                  Quando um workflow precisar de aprovação, ele aparecerá aqui.
                </p>
              </div>
            ) : (
              <div className="grid gap-4">
                {pendingApprovals.map((execution) => {
                  const isLoading = actionLoading === execution.executionId;

                  return (
                    <div
                      key={execution.executionId}
                      className="glass-card p-4 border-yellow-500/30"
                    >
                      <div className="flex items-start justify-between gap-4">
                        <div className="flex-1">
                          <div className="flex items-center gap-2 mb-2">
                            <AlertCircle className="w-5 h-5 text-yellow-500" />
                            <h3 className="font-semibold">Aprovação Necessária</h3>
                          </div>
                          <p className="text-sm text-muted-foreground mb-2">
                            Workflow ID: {execution.workflowId}
                          </p>
                          <div className="flex items-center gap-4 text-xs text-muted-foreground">
                            <span>Step: {execution.currentStepIndex ?? "N/A"}</span>
                            <span>Iniciado: {formatDate(execution.startedAt)}</span>
                          </div>
                          {execution.inputJson && (
                            <details className="mt-2">
                              <summary className="text-xs text-muted-foreground cursor-pointer hover:text-foreground">
                                Ver input
                              </summary>
                              <pre className="mt-2 p-2 bg-secondary/50 rounded text-xs overflow-auto max-h-32">
                                {execution.inputJson}
                              </pre>
                            </details>
                          )}
                        </div>

                        <Button
                          variant="glow"
                          onClick={() => handleApprove(execution.executionId)}
                          disabled={isLoading}
                        >
                          {isLoading ? (
                            <Loader2 className="w-4 h-4 animate-spin mr-2" />
                          ) : (
                            <Check className="w-4 h-4 mr-2" />
                          )}
                          Aprovar
                        </Button>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        )}
      </div>
    </LayoutWrapper>
  );
}
