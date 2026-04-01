"use client";

import { NetworkGraphClient } from "./NetworkGraphClient";
import { LayoutWrapper } from "@/components/LayoutWrapper";

export default function NetworkPage() {
  return (
    <LayoutWrapper title="Rede de Relacionamentos" subtitle="Visualize sua rede de contatos" activeTab="network">
      <NetworkGraphClient />
    </LayoutWrapper>
  );
}




