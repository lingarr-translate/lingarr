import { AxiosError, AxiosResponse, AxiosStatic } from 'axios'
import { ISubtitleService } from '@/ts'

const service = (http: AxiosStatic, resource = '/api/subtitle'): ISubtitleService => ({
    collect<T>(path: string): Promise<T> {
        return new Promise((resolve, reject) => {
            http.get(
                `${resource}/collect`.addParams({
                    path: path
                })
            )
                .then((response: AxiosResponse<T>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    }
})

export const subtitleService = (axios: AxiosStatic): ISubtitleService => {
    return service(axios)
}
