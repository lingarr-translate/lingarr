import { AxiosError, AxiosResponse, AxiosStatic } from 'axios'
import { ITranslateService } from '@/ts/services'

const service = <T>(http: AxiosStatic, resource = '/api/translate') =>
    ({
        translate<T>(path: string, targetLanguage: string): Promise<T> {
            return new Promise((resolve, reject) => {
                http.post(resource, {
                    subtitlePath: path,
                    targetLanguage: targetLanguage
                })
                    .then((response: AxiosResponse) => {
                        resolve(response.data)
                    })
                    .catch((error: AxiosError) => {
                        reject(error.response)
                    })
            })
        }
    }) as T

export const translateService = (axios: AxiosStatic) => {
    return service<ITranslateService>(axios)
}
