"use client";

import { useState, useEffect, useCallback, useMemo, useRef } from "react";
import dynamic from "next/dynamic";
import { getNetworkGraphClient } from "@/lib/api/contactsApiClient";
import type { NetworkGraph as NetworkGraphType } from "@/lib/types/contact";
import { Network, Search, ZoomIn, ZoomOut, Maximize2, RefreshCw } from "lucide-react";
import Link from "next/link";
import { Button } from "@/components/ui/button";
import { useTheme } from "@/lib/theme";

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
  const graphRef = useRef<any>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const { resolvedTheme } = useTheme();
  const isDarkMode = resolvedTheme === "dark";

  const [graphData, setGraphData] = useState<NetworkGraphType | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState("");
  const [selectedNode, setSelectedNode] = useState<string | null>(null);
  const [currentMaxDepth, setCurrentMaxDepth] = useState(maxDepth);
  const [containerWidth, setContainerWidth] = useState(800);

  // Atualizar largura do container
  useEffect(() => {
    const updateWidth = () => {
      if (containerRef.current) {
        setContainerWidth(containerRef.current.offsetWidth);
      }
    };
    updateWidth();
    window.addEventListener("resize", updateWidth);
    return () => window.removeEventListener("resize", updateWidth);
  }, []);

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

  // Controles de zoom
  const handleZoomIn = useCallback(() => {
    if (graphRef.current) {
      const currentZoom = graphRef.current.zoom();
      graphRef.current.zoom(currentZoom * 1.3, 300);
    }
  }, []);

  const handleZoomOut = useCallback(() => {
    if (graphRef.current) {
      const currentZoom = graphRef.current.zoom();
      graphRef.current.zoom(currentZoom / 1.3, 300);
    }
  }, []);

  const handleFitToView = useCallback(() => {
    if (graphRef.current) {
      graphRef.current.zoomToFit(400, 50);
    }
  }, []);

  const handleRecenter = useCallback(() => {
    if (graphRef.current) {
      graphRef.current.centerAt(0, 0, 300);
      graphRef.current.zoom(1, 300);
    }
  }, []);

  // Centralizar após o grafo estabilizar
  const handleEngineStop = useCallback(() => {
    if (graphRef.current) {
      setTimeout(() => {
        graphRef.current.zoomToFit(400, 40);
      }, 100);
    }
  }, []);

  // Cores baseadas no tema
  const getLinkColor = useCallback((link: any) => {
    if (link.isConfirmed) {
      return "#22c55e"; // Green for confirmed
    }
    // Use theme-aware colors for unconfirmed links
    return isDarkMode ? "rgba(255, 255, 255, 0.4)" : "rgba(0, 0, 0, 0.25)";
  }, [isDarkMode]);

  const getNodeColor = useCallback((node: any) => {
    if (selectedNode === node.id) {
      return "#0ea5e9"; // Primary color for selected
    }
    return "#a855f7"; // Accent color for others
  }, [selectedNode]);

  const getBackgroundColor = useCallback(() => {
    return isDarkMode ? "rgba(0, 0, 0, 0.2)" : "rgba(0, 0, 0, 0.02)";
  }, [isDarkMode]);

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
        <div
          ref={containerRef}
          className="relative rounded-xl overflow-hidden"
          style={{ height, backgroundColor: getBackgroundColor() }}
        >
          {graphDataForForceGraph.nodes.length > 0 ? (
            <>
              <ForceGraph2D
                ref={graphRef}
                graphData={graphDataForForceGraph}
                width={containerWidth - 48}
                height={height}
                nodeLabel={(node: any) => `${node.name}${node.company ? ` - ${node.company}` : ""}`}
                nodeColor={getNodeColor}
                nodeVal={(node: any) => {
                  // Tamanho baseado no número de conexões
                  const connections = graphDataForForceGraph.links.filter(
                    (link: any) =>
                      (link.source?.id || link.source) === node.id ||
                      (link.target?.id || link.target) === node.id
                  ).length;
                  return 4 + connections * 2;
                }}
                nodeCanvasObject={(node: any, ctx, globalScale) => {
                  const label = node.name?.split(" ")[0] || "";
                  const fontSize = 12 / globalScale;
                  const nodeSize = 4 + (graphDataForForceGraph.links.filter(
                    (link: any) =>
                      (link.source?.id || link.source) === node.id ||
                      (link.target?.id || link.target) === node.id
                  ).length * 2);

                  // Desenhar círculo do nó
                  ctx.beginPath();
                  ctx.arc(node.x, node.y, nodeSize, 0, 2 * Math.PI);
                  ctx.fillStyle = getNodeColor(node);
                  ctx.fill();

                  // Borda se selecionado
                  if (selectedNode === node.id) {
                    ctx.strokeStyle = "#fff";
                    ctx.lineWidth = 2 / globalScale;
                    ctx.stroke();
                  }

                  // Label do nó (apenas se zoom suficiente)
                  if (globalScale > 0.7) {
                    ctx.font = `${fontSize}px Sans-Serif`;
                    ctx.textAlign = "center";
                    ctx.textBaseline = "middle";
                    ctx.fillStyle = isDarkMode ? "rgba(255,255,255,0.9)" : "rgba(0,0,0,0.8)";
                    ctx.fillText(label, node.x, node.y + nodeSize + fontSize);
                  }
                }}
                nodePointerAreaPaint={(node: any, color, ctx) => {
                  const nodeSize = 6 + (graphDataForForceGraph.links.filter(
                    (link: any) =>
                      (link.source?.id || link.source) === node.id ||
                      (link.target?.id || link.target) === node.id
                  ).length * 2);
                  ctx.beginPath();
                  ctx.arc(node.x, node.y, nodeSize + 5, 0, 2 * Math.PI);
                  ctx.fillStyle = color;
                  ctx.fill();
                }}
                linkColor={getLinkColor}
                linkWidth={(link: any) => 1 + (link.strength || 0.5) * 2}
                linkDirectionalArrowLength={4}
                linkDirectionalArrowRelPos={1}
                linkCanvasObjectMode={() => "after"}
                linkCanvasObject={(link: any, ctx, globalScale) => {
                  // Desenhar label do tipo de relacionamento se zoom suficiente
                  if (globalScale > 1.2 && link.type) {
                    const start = link.source;
                    const end = link.target;
                    if (typeof start !== "object" || typeof end !== "object") return;

                    const midX = (start.x + end.x) / 2;
                    const midY = (start.y + end.y) / 2;

                    const fontSize = 8 / globalScale;
                    ctx.font = `${fontSize}px Sans-Serif`;
                    ctx.textAlign = "center";
                    ctx.textBaseline = "middle";
                    ctx.fillStyle = isDarkMode ? "rgba(255,255,255,0.6)" : "rgba(0,0,0,0.5)";
                    ctx.fillText(link.type, midX, midY);
                  }
                }}
                onNodeClick={handleNodeClick}
                onBackgroundClick={handleBackgroundClick}
                cooldownTicks={100}
                onEngineStop={handleEngineStop}
                enableNodeDrag={true}
                enableZoomInteraction={true}
                enablePanInteraction={true}
              />

              {/* Controles de Zoom */}
              <div className="absolute bottom-3 right-3 flex flex-col gap-1">
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={handleZoomIn}
                  className="h-8 w-8 bg-card/80 backdrop-blur-sm hover:bg-card"
                  title="Aumentar zoom"
                >
                  <ZoomIn className="w-4 h-4" />
                </Button>
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={handleZoomOut}
                  className="h-8 w-8 bg-card/80 backdrop-blur-sm hover:bg-card"
                  title="Diminuir zoom"
                >
                  <ZoomOut className="w-4 h-4" />
                </Button>
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={handleFitToView}
                  className="h-8 w-8 bg-card/80 backdrop-blur-sm hover:bg-card"
                  title="Ajustar à tela"
                >
                  <Maximize2 className="w-4 h-4" />
                </Button>
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={handleRecenter}
                  className="h-8 w-8 bg-card/80 backdrop-blur-sm hover:bg-card"
                  title="Recentralizar"
                >
                  <RefreshCw className="w-4 h-4" />
                </Button>
              </div>

              {/* Legenda */}
              <div className="absolute bottom-3 left-3 flex flex-col gap-1 text-xs bg-card/80 backdrop-blur-sm rounded-lg p-2">
                <div className="flex items-center gap-2">
                  <div className="w-3 h-3 rounded-full bg-[#a855f7]" />
                  <span className="text-muted-foreground">Contato</span>
                </div>
                <div className="flex items-center gap-2">
                  <div className="w-3 h-3 rounded-full bg-[#0ea5e9]" />
                  <span className="text-muted-foreground">Selecionado</span>
                </div>
                <div className="flex items-center gap-2">
                  <div className="w-6 h-0.5 bg-[#22c55e]" />
                  <span className="text-muted-foreground">Confirmado</span>
                </div>
              </div>
            </>
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

