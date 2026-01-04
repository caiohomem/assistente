FROM node:20-alpine AS base
WORKDIR /app
ENV NEXT_TELEMETRY_DISABLED=1

FROM base AS deps
COPY package.json package-lock.json ./
RUN npm ci

FROM base AS builder
ARG NEXT_PUBLIC_API_BASE_URL
ENV NEXT_PUBLIC_API_BASE_URL=$NEXT_PUBLIC_API_BASE_URL
COPY --from=deps /app/node_modules ./node_modules
COPY . .
RUN npm run build

FROM base AS dev
ENV NODE_ENV=development
EXPOSE 3000
EXPOSE 9229
CMD ["sh", "-c", "if [ ! -x node_modules/.bin/next ]; then npm ci; fi; NODE_OPTIONS=--inspect=0.0.0.0:9229 npm run dev -- --hostname 0.0.0.0 --port 3000"]

FROM base AS runner
ENV NODE_ENV=production
ENV PORT=8080
EXPOSE 8080

# Copy standalone output (with standalone, .next contains server.js and all dependencies)
COPY --from=builder /app/.next/standalone ./
COPY --from=builder /app/.next/static ./.next/static
COPY --from=builder /app/public ./public

# Run the standalone server
CMD ["node", "server.js"]
