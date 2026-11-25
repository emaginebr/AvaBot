FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
# Variáveis PÚBLICAS do Vite (vão para o navegador), embutidas no build.
ARG VITE_API_URL=https://avabot.net/api
ARG VITE_WS_URL=wss://avabot.net/api
ENV VITE_API_URL=$VITE_API_URL \
    VITE_WS_URL=$VITE_WS_URL
RUN npm run build

FROM nginx:alpine
# NGINX_CONF=nginx.k8s.conf no Kubernetes (só a SPA + /healthz); nginx.conf no ambiente local.
ARG NGINX_CONF=nginx.conf
COPY --from=build /app/dist /usr/share/nginx/html
COPY ${NGINX_CONF} /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
