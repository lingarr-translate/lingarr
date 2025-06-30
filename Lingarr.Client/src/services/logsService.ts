import { AxiosStatic } from 'axios'
import { ILogsService } from '@/ts'

const service = (http: AxiosStatic, resource = '/api/logs'): ILogsService => ({
    getStream(): EventSource {
        console.log(http)
        return new EventSource(`${resource}/stream`)
    }
})

export const logsService = (axios: AxiosStatic): ILogsService => {
    return service(axios)
}
