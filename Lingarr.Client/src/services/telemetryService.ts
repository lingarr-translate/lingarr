import { AxiosError, AxiosResponse, AxiosStatic } from 'axios'
import { ITelemetryService } from '@/ts'

const service = (http: AxiosStatic, resource = '/api/telemetry'): ITelemetryService => ({
    preview<T>(): Promise<T> {
        return new Promise((resolve, reject) => {
            http.get(`${resource}/preview`)
                .then((response: AxiosResponse<T>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    }
})

export const telemetryService = (axios: AxiosStatic): ITelemetryService => {
    return service(axios)
}
