# KUKA FM Middleware web (Vue SPA served by nginx, proxies /api). Build context: repository root.
#   docker build -f docker/web.Dockerfile .

FROM node:24.22.1-alpine AS build
WORKDIR /src
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

FROM nginx:1.30.6-alpine AS runtime
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/dist /usr/share/nginx/html
EXPOSE 80
