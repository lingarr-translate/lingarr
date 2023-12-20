import { Router } from 'express'
import HomeController from '../controllers/home.controller'
import TranslateController from '../controllers/translate.controller'
import ResourceController from '../controllers/resource.controller'
import LanguageController from '../controllers/language.controller'

class HomeRoutes {
    router = Router()
    home = new HomeController()
    translate = new TranslateController()
    resource = new ResourceController()
    language = new LanguageController()

    constructor() {
        this.initializeRoutes()
    }

    initializeRoutes() {
        /* get */
        this.router.get('/', this.home.welcome)
        this.router.get('/resource', this.resource.resource)
        this.router.get('/languages', this.language.languages)

        /* post */
        this.router.post('/translate', this.translate.translate)
    }
}

export default new HomeRoutes().router
