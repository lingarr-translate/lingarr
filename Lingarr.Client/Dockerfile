FROM node:alpine

WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .

EXPOSE 9876
CMD [ "npm", "run dev" ]
