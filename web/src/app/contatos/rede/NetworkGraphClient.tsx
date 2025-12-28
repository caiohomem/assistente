"use client";

import { NetworkGraph } from "@/components/NetworkGraph";

export function NetworkGraphClient() {
  return (
    <NetworkGraph
      height={600}
      maxDepth={2}
      showControls={true}
      showNodeDetails={true}
      showFullPageLink={false}
    />
  );
}

