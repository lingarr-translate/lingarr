FROM node:20.10.0-slim AS frontend

# Prepare frontend
COPY ./lingarr/ /app/frontend/
WORKDIR /app/frontend

RUN npm install
RUN npm run build

FROM node:20.10.0-slim AS api
COPY ./lingarr_api/ /app/api/
WORKDIR /app/api

# Prepare API
RUN npm install
RUN npm run build
# Add dependencies
WORKDIR /app/api/build
RUN npm install --only=production
RUN ls -al

# production image
FROM node:20.10.0-slim
WORKDIR /app

# environment variables
ARG PORT=9876
ARG LIBRETRANSLATE_API=""

ENV NODE_ENV production
ENV LIBRETRANSLATE_API=$LIBRETRANSLATE_API

# copy api and client
RUN mkdir config
RUN mkdir media
COPY --from=api /app/api/build/ /app/
COPY --from=frontend /app/frontend/dist/ /app/src/views/

# expose application
EXPOSE $PORT
ENV PORT=${PORT}

CMD ["npm", "start"]