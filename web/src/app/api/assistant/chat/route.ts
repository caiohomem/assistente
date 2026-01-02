import { NextRequest, NextResponse } from "next/server";
import { getBffSession, getApiBaseUrl } from "@/lib/bff";

/**
 * API route para chat com assistente - delega para o backend
 */
export async function POST(request: NextRequest) {
  try {
    // Obter cookies do request original
    const cookieHeader = request.headers.get("cookie") || "";
    
    // Verificar autenticação passando os cookies
    const session = await getBffSession(cookieHeader ? { cookieHeader } : undefined);
    if (!session.authenticated) {
      return NextResponse.json({ error: "Não autenticado" }, { status: 401 });
    }

    const body = await request.json();
    const { messages, model } = body;

    if (!messages || !Array.isArray(messages)) {
      return NextResponse.json({ error: "messages é obrigatório" }, { status: 400 });
    }

    // Chamar o backend que processa o chat com OpenAI
    // Passar os cookies do request original para o backend
    const apiBase = getApiBaseUrl();
    const response = await fetch(`${apiBase}/api/assistant/chat`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        ...(cookieHeader ? { "Cookie": cookieHeader } : {}),
        ...(session.csrfToken ? { "X-CSRF-TOKEN": session.csrfToken } : {}),
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
