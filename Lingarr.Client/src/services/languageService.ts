import { AxiosError, AxiosResponse, AxiosStatic } from 'axios'
import { ILanguageService } from '@/ts/services'

const service = (http: AxiosStatic, resource = '/api/language') => ({
    list<T>(): Promise<T> {
        return new Promise((resolve, reject) => {
            http.get(resource)
                .then((response: AxiosResponse<T>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    }
})

export const languageService = (axios: AxiosStatic): ILanguageService => {
    return service(axios)
}
