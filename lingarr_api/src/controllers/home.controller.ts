import { Request, Response } from 'express'

export default class HomeController {
    async welcome(req: Request, res: Response) {
        return res.json({ message: 'Welcome to lingarr.' })
    }
    async index(req: Request, res: Response) {
        return res.sendFile('src/views/', { root: '.' })
    }
}
