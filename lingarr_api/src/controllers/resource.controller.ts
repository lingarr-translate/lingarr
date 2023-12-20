import { Request, Response } from 'express'
import ResourceService from '../services/resource.service'

export default class ResourceController {
    async resource(request: Request, response: Response) {
        const path = request.query.path as string

        try {
            const directoryList = await ResourceService.getDirectoryList(path)
            response.status(200).json(directoryList)
        } catch (err) {
            response.status(500).json(err)
        }
    }
}
