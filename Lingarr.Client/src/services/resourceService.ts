import { AxiosError, AxiosResponse, AxiosStatic } from 'axios'
import { IResourceService } from '@/ts/services'

const service = (http: AxiosStatic, resource = '/api/directory') => ({
    list<T>(mediaType = ''): Promise<T> {
        return new Promise((resolve, reject) => {
            http.get(`${resource}?mediaType=${mediaType}`)
                .then((response: AxiosResponse<T>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    }
})

export const resourceService = (axios: AxiosStatic): IResourceService => {
    return service(axios)
}
