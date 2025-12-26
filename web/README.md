This is the **AssistenteExecutivo Web** (Next.js App Router) project bootstrapped with [`create-next-app`](https://nextjs.org/docs/app/api-reference/cli/create-next-app).

## Getting Started

### Backend dependency (BFF)

This web app expects the backend API/BFF running locally at:

- `http://localhost:5239` (see backend `launchSettings.json`)

Create `web/.env.local`:

**Para desenvolvimento local:**
```bash
NEXT_PUBLIC_API_BASE_URL=http://localhost:5239
```

**Para acesso via tunnel (hosts públicos):**
```bash
NEXT_PUBLIC_API_BASE_URL=https://assistente-api.callback-local-cchagas.xyz
```

**Nota importante**: As chamadas à API são feitas **diretamente** (sem proxy do Next.js).
Isso facilita o desenvolvimento e debugging. Todas as requisições vão direto para a URL
configurada em `NEXT_PUBLIC_API_BASE_URL`. Se necessário adicionar proxy no futuro,
pode-se configurar no `next.config.ts`.

### Run

First, install deps and run the development server:

```bash
npm install
npm run dev
```

Open [http://localhost:3000](http://localhost:3000) with your browser to see the result.

### Routes

- `/login` - starts login flow (redirects to backend `/auth/login`)
- `/esqueci-senha` - calls backend `/auth/forgot-password` (requires CSRF)
- `/reset-senha` - calls backend `/auth/reset-password` (requires CSRF)
- `/protected` - **protected page** (server-side check via backend `/auth/session`)

This project uses [`next/font`](https://nextjs.org/docs/app/building-your-application/optimizing/fonts) to automatically optimize and load [Geist](https://vercel.com/font), a new font family for Vercel.

## Learn More

To learn more about Next.js, take a look at the following resources:

- [Next.js Documentation](https://nextjs.org/docs) - learn about Next.js features and API.
- [Learn Next.js](https://nextjs.org/learn) - an interactive Next.js tutorial.

You can check out [the Next.js GitHub repository](https://github.com/vercel/next.js) - your feedback and contributions are welcome!

## Deploy on Vercel

The easiest way to deploy your Next.js app is to use the [Vercel Platform](https://vercel.com/new?utm_medium=default-template&filter=next.js&utm_source=create-next-app&utm_campaign=create-next-app-readme) from the creators of Next.js.

Check out our [Next.js deployment documentation](https://nextjs.org/docs/app/building-your-application/deploying) for more details.
