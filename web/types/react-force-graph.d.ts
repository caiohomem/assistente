declare module "react-force-graph" {
  import type { ComponentType, MutableRefObject } from "react"

  export interface ForceGraphMethods {
    zoom(): number
    zoom(factor: number, duration?: number): void
    zoomToFit(duration?: number, padding?: number): void
    centerAt(x: number, y: number, duration?: number): void
  }

  export type ForceGraphProps = Record<string, unknown>

  export const ForceGraph2D: ComponentType<
    ForceGraphProps & { ref?: MutableRefObject<ForceGraphMethods | undefined> }
  >
}
