import { AxiosError, AxiosResponse, AxiosStatic } from 'axios'
import { IVersionService } from '@/ts'

const service = (http: AxiosStatic, resource = '/api/version'): IVersionService => ({
    getVersion<T>(): Promise<T> {
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

export const versionService = (axios: AxiosStatic): IVersionService => {
    return service(axios)
}
