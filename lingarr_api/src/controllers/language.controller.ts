import { Request, Response } from 'express'
import LanguageService from '../services/language.service'

export default class ResourceController {
    async languages(request: Request, response: Response) {
        try {
            const languageList = await LanguageService.languages()
            response.status(200).json(languageList)
        } catch (err) {
            response.status(500).json(err)
        }
    }
}
