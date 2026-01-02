import * as React from "react"
import { Slot } from "@radix-ui/react-slot"
import { cn } from "@/lib/utils"

export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: "default" | "ghost" | "glass" | "glow" | "destructive"
  size?: "default" | "icon" | "sm" | "lg"
  asChild?: boolean
}

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant = "default", size = "default", asChild = false, ...props }, ref) => {
    const baseStyles = "inline-flex items-center justify-center rounded-xl font-medium transition-all duration-300 focus:outline-none focus:ring-2 focus:ring-primary/50 disabled:opacity-50 disabled:pointer-events-none"
    
    const variants = {
      default: "bg-[hsl(var(--primary))] text-[hsl(var(--primary-foreground))] hover:bg-[hsl(var(--primary)/0.9)] hover:scale-105",
      ghost: "bg-transparent hover:bg-[hsl(var(--secondary)/0.8)] text-foreground",
      glass: "bg-[hsl(var(--glass)/0.5)] backdrop-blur-sm border border-[hsl(var(--glass-border))] hover:bg-[hsl(var(--glass)/0.8)] text-foreground",
      glow: "bg-[hsl(var(--primary))] text-[hsl(var(--primary-foreground))] hover:bg-[hsl(var(--primary)/0.9)] hover:shadow-lg hover:shadow-[hsl(var(--primary)/0.3)] hover:scale-105",
      destructive: "bg-[hsl(var(--destructive))] text-[hsl(var(--destructive-foreground))] hover:bg-[hsl(var(--destructive)/0.9)] hover:scale-105"
    }
    
    const sizes = {
      default: "h-10 px-4 py-2",
      sm: "h-8 px-3 text-sm",
      lg: "h-12 px-6 text-lg",
      icon: "h-10 w-10"
    }

    const classes = cn(
      baseStyles,
      variants[variant],
      sizes[size],
      className
    )

    const Comp = asChild ? Slot : "button"

    return (
      <Comp
        className={classes}
        ref={ref}
        {...props}
      />
    )
  }
)
Button.displayName = "Button"

export { Button }
