import { NextRequest, NextResponse } from "next/server";
import { auth, clerkClient } from "@clerk/nextjs/server";
import { getApiBaseUrl } from "@/lib/bff";

/**
 * API route para chat com assistente - delega para o backend
 */
export async function POST(request: NextRequest) {
  try {
    const { userId, getToken } = await auth();
    if (!userId) {
      return NextResponse.json({ error: "Não autenticado" }, { status: 401 });
    }

    const accessToken = await getToken();
    if (!accessToken) {
      return NextResponse.json({ error: "Token não disponível" }, { status: 401 });
    }

    const client = await clerkClient();
    const user = await client.users.getUser(userId);
    const email = user.primaryEmailAddress?.emailAddress ?? null;
    const fallbackName = [user.firstName, user.lastName].filter(Boolean).join(" ");
    const fullName = user.fullName ?? (fallbackName || null);

    const body = await request.json();
    const { messages, model } = body;

    if (!messages || !Array.isArray(messages)) {
      return NextResponse.json({ error: "messages é obrigatório" }, { status: 400 });
    }

    const apiBase = getApiBaseUrl();
    const response = await fetch(`${apiBase}/api/assistant/chat`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${accessToken}`,
        ...(email ? { "X-User-Email": email } : {}),
        ...(fullName ? { "X-User-Name": fullName } : {}),
      },
      body: JSON.stringify({
        messages,
        model,
      }),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      console.error("Backend API error:", errorData);
      return NextResponse.json(
        { error: "Erro ao processar chat", details: errorData },
        { status: response.status }
      );
    }

    const data = await response.json();
    return NextResponse.json(data);
  } catch (error) {
    console.error("Error in chat API:", error);
    return NextResponse.json(
      {
        error: "Erro interno do servidor",
        message: error instanceof Error ? error.message : String(error),
      },
      { status: 500 }
    );
  }
}
