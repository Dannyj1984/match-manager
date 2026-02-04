# Build Stage
FROM node:20-slim AS build
WORKDIR /src

# Installation dependencies
COPY package*.json ./
RUN npm install

# Build the app
COPY . .
RUN npm run build

# Run Stage
FROM node:20-slim AS runtime
WORKDIR /app

# Copy output from build
COPY --from=build /src/.output ./.output

# Environment variables
ENV NODE_ENV=production
ENV HOST=0.0.0.0
ENV PORT=3000

EXPOSE 3000

CMD ["node", ".output/server/index.mjs"]
