import { ILogsService } from '@/ts'
import { resolveUrl } from '@/utils/baseUrl'

const service = (resource = '/api/logs'): ILogsService => ({
    getStream(): EventSource {
        return new EventSource(resolveUrl(`${resource}/stream`))
    }
})

export const logsService = (): ILogsService => {
    return service()
}
