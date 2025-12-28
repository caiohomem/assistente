"use client"

import { useState, useEffect, useRef } from "react"

interface Node {
  id: string
  label: string
  color: string
  x: number
  y: number
}

interface Edge {
  from: string
  to: string
}

export function RelationshipGraph() {
  const canvasRef = useRef<HTMLCanvasElement>(null)
  const [nodes] = useState<Node[]>([
    { id: "you", label: "Você", color: "#0ea5e9", x: 0, y: 0 },
    { id: "ana", label: "Ana Silva", color: "#a855f7", x: 0, y: 0 },
    { id: "julia", label: "Julia F.", color: "#a855f7", x: 0, y: 0 },
    { id: "maria", label: "Maria L.", color: "#f97316", x: 0, y: 0 },
    { id: "carlos", label: "Carlos M.", color: "#22c55e", x: 0, y: 0 },
    { id: "bruno", label: "Bruno S.", color: "#22c55e", x: 0, y: 0 },
    { id: "pedro", label: "Pedro R.", color: "#0ea5e9", x: 0, y: 0 },
  ])

  const [edges] = useState<Edge[]>([
    { from: "you", to: "ana" },
    { from: "you", to: "julia" },
    { from: "you", to: "maria" },
    { from: "you", to: "carlos" },
    { from: "you", to: "pedro" },
    { from: "ana", to: "julia" },
    { from: "carlos", to: "bruno" },
  ])

  useEffect(() => {
    const canvas = canvasRef.current
    if (!canvas) return

    const ctx = canvas.getContext("2d")
    if (!ctx) return

    const width = canvas.width = canvas.offsetWidth
    const height = canvas.height = canvas.offsetHeight

    // Calcular posições dos nós em um layout circular
    const centerX = width / 2
    const centerY = height / 2
    const radius = Math.min(width, height) * 0.3
    const angleStep = (2 * Math.PI) / (nodes.length - 1)

    const positionedNodes = nodes.map((node, index) => {
      if (node.id === "you") {
        return { ...node, x: centerX, y: centerY }
      }
      const angle = (index - 1) * angleStep - Math.PI / 2
      return {
        ...node,
        x: centerX + radius * Math.cos(angle),
        y: centerY + radius * Math.sin(angle),
      }
    })

    const draw = () => {
      ctx.clearRect(0, 0, width, height)

      // Desenhar arestas
      ctx.strokeStyle = "rgba(255, 255, 255, 0.2)"
      ctx.lineWidth = 1
      edges.forEach((edge) => {
        const fromNode = positionedNodes.find((n) => n.id === edge.from)
        const toNode = positionedNodes.find((n) => n.id === edge.to)
        if (fromNode && toNode) {
          ctx.beginPath()
          ctx.moveTo(fromNode.x, fromNode.y)
          ctx.lineTo(toNode.x, toNode.y)
          ctx.stroke()
        }
      })

      // Desenhar nós
      positionedNodes.forEach((node) => {
        const isYou = node.id === "you"
        const nodeRadius = isYou ? 20 : 15

        // Círculo
        ctx.beginPath()
        ctx.arc(node.x, node.y, nodeRadius, 0, 2 * Math.PI)
        ctx.fillStyle = node.color
        ctx.fill()
        ctx.strokeStyle = "rgba(255, 255, 255, 0.3)"
        ctx.lineWidth = 2
        ctx.stroke()

        // Label
        ctx.fillStyle = "white"
        ctx.font = isYou ? "bold 12px sans-serif" : "10px sans-serif"
        ctx.textAlign = "center"
        ctx.textBaseline = "middle"
        ctx.fillText(node.label, node.x, node.y + nodeRadius + 15)
      })
    }

    draw()

    const handleResize = () => {
      const newWidth = canvas.offsetWidth
      const newHeight = canvas.offsetHeight
      canvas.width = newWidth
      canvas.height = newHeight
      draw()
    }

    window.addEventListener("resize", handleResize)
    return () => window.removeEventListener("resize", handleResize)
  }, [nodes, edges])

  return (
    <div className="glass-card p-6 animate-slide-up">
      <div className="flex items-center justify-between mb-4">
        <h2 className="font-semibold text-lg">Rede de Relacionamentos</h2>
        <span className="text-xs text-muted-foreground">{edges.length} conexões</span>
      </div>
      <div className="relative h-64 rounded-xl overflow-hidden bg-secondary/20">
        <canvas
          ref={canvasRef}
          className="w-full h-full"
          style={{ imageRendering: "crisp-edges" }}
        />
      </div>
    </div>
  )
}

