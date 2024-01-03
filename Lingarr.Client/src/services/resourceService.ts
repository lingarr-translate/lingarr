import { AxiosError, AxiosResponse, AxiosStatic } from 'axios'
import { IResourceService } from '@/ts/services'

const service = <T>(http: AxiosStatic, resource = '/api/directory') =>
    ({
        list<T>(mediaType = ''): Promise<T> {
            return new Promise((resolve, reject) => {
                http.get(`${resource}/list?mediaType=${mediaType}`)
                    .then((response: AxiosResponse<T>) => {
                        resolve(response.data)
                    })
                    .catch((error: AxiosError) => {
                        reject(error.response)
                    })
            })
        }
    }) as T

export const resourceService = (axios: AxiosStatic) => {
    return service<IResourceService>(axios)
}
