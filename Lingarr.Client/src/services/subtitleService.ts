import { AxiosError, AxiosResponse, AxiosStatic } from 'axios'
import { ISubtitle, ISubtitleService } from '@/ts'

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
    },
    translate<T>(subtitle: ISubtitle, target: string): Promise<T> {
        return new Promise((resolve, reject) => {
            http.get(
                `${resource}/translate`.addParams({
                    subtitle: subtitle.path,
                    source: subtitle.language,
                    target: target
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
