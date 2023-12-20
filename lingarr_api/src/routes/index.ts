import { Application } from 'express'
import homeRoutes from './home.routes'
import apiRoutes from './api.routes'

export default class Routes {
    constructor(app: Application) {
        app.use('/api', apiRoutes)
        app.use('/', homeRoutes)
    }
}
