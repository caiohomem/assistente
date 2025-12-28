"use client";

import { useState, useEffect, useCallback, useMemo } from "react";
import dynamic from "next/dynamic";
import { getNetworkGraphClient } from "@/lib/api/contactsApiClient";
import type { NetworkGraph as NetworkGraphType } from "@/lib/types/contact";
import { Network, Search } from "lucide-react";
import Link from "next/link";
import { Button } from "@/components/ui/button";

// Dynamic import para react-force-graph (client-side only)
const ForceGraph2D = dynamic(
  () => import("react-force-graph").then((mod) => mod.ForceGraph2D),
  { ssr: false }
);

interface NetworkGraphProps {
  /** Altura do grafo em pixels */
  height?: number;
  /** Profundidade máxima do grafo */
  maxDepth?: number;
  /** Se deve mostrar controles (busca, profundidade) */
  showControls?: boolean;
  /** Se deve mostrar detalhes do nó selecionado */
  showNodeDetails?: boolean;
  /** Se deve mostrar link para página completa */
  showFullPageLink?: boolean;
  /** Classe CSS adicional para o container */
  className?: string;
  /** Callback quando um nó é clicado */
  onNodeClick?: (nodeId: string) => void;
}

export function NetworkGraph({
  height = 400,
  maxDepth = 2,
  showControls = false,
  showNodeDetails = false,
  showFullPageLink = false,
  className = "",
  onNodeClick,
}: NetworkGraphProps) {
  const [graphData, setGraphData] = useState<NetworkGraphType | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState("");
  const [selectedNode, setSelectedNode] = useState<string | null>(null);
  const [currentMaxDepth, setCurrentMaxDepth] = useState(maxDepth);

  useEffect(() => {
    async function loadGraph() {
      try {
        setLoading(true);
        setError(null);
        const data = await getNetworkGraphClient(currentMaxDepth);
        setGraphData(data);
      } catch (e) {
        console.error("Erro ao carregar grafo:", e);
        setError(e instanceof Error ? e.message : "Erro ao carregar rede de relacionamentos");
      } finally {
        setLoading(false);
      }
    }

    loadGraph();
  }, [currentMaxDepth]);

  // Preparar dados para o ForceGraph
  const graphDataForForceGraph = useMemo(() => {
    if (!graphData) return { nodes: [], links: [] };

    // Filtrar nós baseado na busca
    const filteredNodes = graphData.nodes.filter((node) => {
      if (!searchTerm) return true;
      const search = searchTerm.toLowerCase();
      return (
        node.fullName.toLowerCase().includes(search) ||
        node.company?.toLowerCase().includes(search) ||
        node.jobTitle?.toLowerCase().includes(search)
      );
    });

    const filteredNodeIds = new Set(filteredNodes.map((n) => n.contactId));

    // Filtrar arestas para incluir apenas as que conectam nós filtrados
    const filteredEdges = graphData.edges.filter(
      (edge) => filteredNodeIds.has(edge.sourceContactId) && filteredNodeIds.has(edge.targetContactId)
    );

    // Converter para formato do ForceGraph
    return {
      nodes: filteredNodes.map((node) => ({
        id: node.contactId,
        name: node.fullName,
        company: node.company,
        jobTitle: node.jobTitle,
        email: node.primaryEmail,
      })),
      links: filteredEdges.map((edge) => ({
        source: edge.sourceContactId,
        target: edge.targetContactId,
        type: edge.type,
        strength: edge.strength,
        isConfirmed: edge.isConfirmed,
        relationshipId: edge.relationshipId,
      })),
    };
  }, [graphData, searchTerm]);

  const handleNodeClick = useCallback(
    (node: any) => {
      setSelectedNode(node.id);
      if (onNodeClick) {
        onNodeClick(node.id);
      }
    },
    [onNodeClick]
  );

  const handleBackgroundClick = useCallback(() => {
    setSelectedNode(null);
  }, []);

  if (loading) {
    return (
      <div className={`glass-card p-6 ${className}`}>
        <div className="flex items-center justify-between mb-4">
          <h2 className="font-semibold text-lg">Rede de Relacionamentos</h2>
          {showFullPageLink && (
            <Link href="/contatos/rede">
              <Button variant="ghost" size="sm">
                Ver completo
              </Button>
            </Link>
          )}
        </div>
        <div className="flex items-center justify-center" style={{ height }}>
          <p className="text-muted-foreground">Carregando rede de relacionamentos...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className={`glass-card p-6 ${className}`}>
        <div className="flex items-center justify-between mb-4">
          <h2 className="font-semibold text-lg">Rede de Relacionamentos</h2>
          {showFullPageLink && (
            <Link href="/contatos/rede">
              <Button variant="ghost" size="sm">
                Ver completo
              </Button>
            </Link>
          )}
        </div>
        <div className="flex flex-col items-center justify-center" style={{ height }}>
          <p className="text-red-500 mb-4">{error}</p>
          <Button onClick={() => window.location.reload()} size="sm">
            Tentar novamente
          </Button>
        </div>
      </div>
    );
  }

  if (!graphData || graphData.nodes.length === 0) {
    return (
      <div className={`glass-card p-6 ${className}`}>
        <div className="flex items-center justify-between mb-4">
          <h2 className="font-semibold text-lg">Rede de Relacionamentos</h2>
          {showFullPageLink && (
            <Link href="/contatos/rede">
              <Button variant="ghost" size="sm">
                Ver completo
              </Button>
            </Link>
          )}
        </div>
        <div className="flex flex-col items-center justify-center" style={{ height }}>
          <Network className="w-12 h-12 mx-auto mb-4 text-muted-foreground" />
          <p className="text-muted-foreground mb-4">Nenhum contato encontrado para exibir na rede.</p>
          <Link href="/contatos?novo=true">
            <Button size="sm">Adicionar primeiro contato</Button>
          </Link>
        </div>
      </div>
    );
  }

  const selectedNodeData = graphData.nodes.find((n) => n.contactId === selectedNode);
  const nodeRelationships = graphData.edges.filter(
    (e) => e.sourceContactId === selectedNode || e.targetContactId === selectedNode
  );

  return (
    <div className={`space-y-4 ${className}`}>
      {/* Header */}
      <div className="glass-card p-6">
        <div className="flex items-center justify-between mb-4">
          <div>
            <h2 className="font-semibold text-lg">Rede de Relacionamentos</h2>
            <span className="text-xs text-muted-foreground">
              {graphDataForForceGraph.nodes.length} contatos • {graphDataForForceGraph.links.length} conexões
            </span>
          </div>
          {showFullPageLink && (
            <Link href="/contatos/rede">
              <Button variant="ghost" size="sm">
                Ver completo
              </Button>
            </Link>
          )}
        </div>

        {/* Controles */}
        {showControls && (
          <div className="flex flex-col sm:flex-row gap-4 items-start sm:items-center justify-between mb-4">
            <div className="flex-1 w-full sm:max-w-md">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
                <input
                  type="text"
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  placeholder="Buscar contato..."
                  className="w-full rounded-lg border border-border bg-secondary/50 backdrop-blur-sm pl-10 pr-4 py-2 text-sm transition-all duration-300 focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary/50"
                />
              </div>
            </div>

            <div className="flex items-center gap-2">
              <label className="text-sm text-muted-foreground">Profundidade:</label>
              <select
                value={currentMaxDepth}
                onChange={(e) => setCurrentMaxDepth(Number(e.target.value))}
                className="rounded-lg border border-border bg-secondary/50 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary/30"
              >
                <option value={1}>1 nível</option>
                <option value={2}>2 níveis</option>
                <option value={3}>3 níveis</option>
              </select>
            </div>
          </div>
        )}

        {/* Grafo */}
        <div className="relative rounded-xl overflow-hidden bg-secondary/20" style={{ height }}>
          {graphDataForForceGraph.nodes.length > 0 ? (
            <ForceGraph2D
              graphData={graphDataForForceGraph}
              nodeLabel={(node: any) => `${node.name}${node.company ? ` - ${node.company}` : ""}`}
              nodeColor={(node: any) => {
                if (selectedNode === node.id) return "#0ea5e9";
                return "#a855f7";
              }}
              nodeVal={(node: any) => {
                // Tamanho baseado no número de conexões
                const connections = graphDataForForceGraph.links.filter(
                  (link: any) => link.source === node.id || link.target === node.id
                ).length;
                return 3 + connections * 2;
              }}
              linkColor={(link: any) => (link.isConfirmed ? "#22c55e" : "rgba(255, 255, 255, 0.3)")}
              linkWidth={(link: any) => 0.5 + link.strength * 2}
              linkDirectionalArrowLength={6}
              linkDirectionalArrowRelPos={1}
              onNodeClick={handleNodeClick}
              onBackgroundClick={handleBackgroundClick}
              cooldownTicks={100}
              onEngineStop={() => {
                // Grafo estabilizado
              }}
            />
          ) : (
            <div className="flex items-center justify-center h-full">
              <p className="text-muted-foreground">Nenhum contato encontrado com os filtros aplicados.</p>
            </div>
          )}
        </div>
      </div>

      {/* Painel de detalhes do nó selecionado */}
      {showNodeDetails && selectedNodeData && (
        <div className="glass-card p-6 animate-slide-up">
          <div className="flex items-start justify-between mb-4">
            <div>
              <h3 className="text-xl font-semibold mb-1">{selectedNodeData.fullName}</h3>
              {selectedNodeData.jobTitle && (
                <p className="text-sm text-muted-foreground">{selectedNodeData.jobTitle}</p>
              )}
              {selectedNodeData.company && (
                <p className="text-sm text-muted-foreground">{selectedNodeData.company}</p>
              )}
              {selectedNodeData.primaryEmail && (
                <p className="text-sm text-muted-foreground mt-1">{selectedNodeData.primaryEmail}</p>
              )}
            </div>
            <Button variant="ghost" size="icon" onClick={() => setSelectedNode(null)}>
              ×
            </Button>
          </div>

          <div className="mt-4">
            <Link href={`/contatos/${selectedNodeData.contactId}`}>
              <Button variant="ghost" className="w-full">
                Ver detalhes do contato
              </Button>
            </Link>
          </div>

          {nodeRelationships.length > 0 && (
            <div className="mt-6">
              <h4 className="text-sm font-semibold mb-3">Relacionamentos ({nodeRelationships.length})</h4>
              <div className="space-y-2">
                {nodeRelationships.map((rel) => {
                  const relatedContactId =
                    rel.sourceContactId === selectedNode ? rel.targetContactId : rel.sourceContactId;
                  const relatedContact = graphData.nodes.find((n) => n.contactId === relatedContactId);

                  return (
                    <div key={rel.relationshipId} className="flex items-center justify-between p-3 bg-secondary/30 rounded-lg">
                      <div className="flex-1">
                        <Link
                          href={`/contatos/${relatedContactId}`}
                          className="font-medium hover:text-primary transition-colors"
                          onClick={() => setSelectedNode(relatedContactId)}
                        >
                          {relatedContact?.fullName || "Contato desconhecido"}
                        </Link>
                        <div className="flex items-center gap-2 mt-1">
                          <span className="text-xs px-2 py-1 bg-primary/10 text-primary rounded-full">
                            {rel.type}
                          </span>
                          {rel.isConfirmed && (
                            <span className="text-xs px-2 py-1 bg-success/10 text-success rounded-full">
                              Confirmado
                            </span>
                          )}
                          {rel.strength > 0 && (
                            <span className="text-xs text-muted-foreground">
                              Força: {Math.round(rel.strength * 100)}%
                            </span>
                          )}
                        </div>
                        {rel.description && (
                          <p className="text-xs text-muted-foreground mt-1">{rel.description}</p>
                        )}
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
}

