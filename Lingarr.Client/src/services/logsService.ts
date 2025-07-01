import { ILogsService } from '@/ts'

const service = (resource = '/api/logs'): ILogsService => ({
    getStream(): EventSource {
        return new EventSource(`${resource}/stream`)
    }
})

export const logsService = (): ILogsService => {
    return service()
}
