import { Router } from 'express'
import HomeController from '../controllers/home.controller'

class HomeRoutes {
    router = Router()
    controller = new HomeController()

    constructor() {
        this.initializeRoutes()
    }

    initializeRoutes() {
        this.router.get('*', this.controller.index)
    }
}

export default new HomeRoutes().router
