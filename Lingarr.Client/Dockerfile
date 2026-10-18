FROM node:24-alpine

WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .

EXPOSE 9876
CMD [ "npm", "run dev" ]
