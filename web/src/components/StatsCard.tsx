"use client"

import { LucideIcon } from "lucide-react"
import { cn } from "@/lib/utils"

interface StatsCardProps {
  title: string
  value: number | string
  change?: string
  changeType?: "positive" | "negative" | "neutral"
  icon: LucideIcon
  delay?: number
}

export function StatsCard({ title, value, change, changeType = "neutral", icon: Icon, delay = 0 }: StatsCardProps) {
  return (
    <div 
      className="glass-card p-6 card-hover animate-slide-up"
      style={{ animationDelay: `${delay}ms` }}
    >
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-sm font-medium text-muted-foreground">{title}</h3>
        <div className="w-10 h-10 rounded-xl bg-primary/10 flex items-center justify-center">
          <Icon className="w-5 h-5 text-primary" />
        </div>
      </div>
      
      <div className="mb-2">
        <div className="text-3xl font-bold text-foreground">{value}</div>
        {change && (
          <div className={cn(
            "text-xs mt-1 flex items-center gap-1",
            changeType === "positive" && "text-green-400",
            changeType === "negative" && "text-red-400",
            changeType === "neutral" && "text-muted-foreground"
          )}>
            {changeType === "positive" && "▲"}
            {changeType === "negative" && "▼"}
            {change}
          </div>
        )}
      </div>
    </div>
  )
}

