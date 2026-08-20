FROM wppconnect/server-cli:latest

WORKDIR /usr/src/wpp-server

# Dependencias nativas exigidas para compilar o sharp (usado pelo @wppconnect-team/wppconnect >= 2.x)
RUN apk add --no-cache vips vips-dev fftw-dev gcc g++ make libc6-compat pkgconfig python3
RUN npm install -g node-gyp

RUN npm install @wppconnect-team/wppconnect@2.3.1

COPY wppconnect.config.js ./src/config.ts
