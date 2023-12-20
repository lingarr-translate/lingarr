import express, { Application } from 'express'
import Routes from './routes'
import './utils'

export default class Server {
    constructor(app: Application) {
        Server.config(app)
        new Routes(app)
    }

    private static config(app: Application): void {
        app.use(express.json())
        app.use(express.static('./src/views/'))
    }
}
