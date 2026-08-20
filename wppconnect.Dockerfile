FROM wppconnect/server-cli:latest

WORKDIR /usr/src/wpp-server

RUN npm install @wppconnect-team/wppconnect@2.3.1

COPY wppconnect.config.js ./src/config.ts
