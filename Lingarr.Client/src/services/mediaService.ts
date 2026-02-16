import { AxiosError, AxiosResponse, AxiosStatic } from 'axios'
import { IMediaService, MediaType } from '@/ts'

const service = (http: AxiosStatic, resource = '/api/media'): IMediaService => ({
    movies<T>(
        pageNumber: number,
        searchQuery: string,
        orderBy: string,
        ascending: boolean
    ): Promise<T> {
        return new Promise((resolve, reject) => {
            http.get(
                `${resource}/movies`.addParams({
                    pageNumber: pageNumber,
                    searchQuery: searchQuery,
                    orderBy: orderBy,
                    ascending: ascending
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
    shows<T>(
        pageNumber: number,
        searchQuery: string,
        orderBy: string,
        ascending: boolean
    ): Promise<T> {
        return new Promise((resolve, reject) => {
            http.get(
                `${resource}/shows`.addParams({
                    pageNumber: pageNumber,
                    searchQuery: searchQuery,
                    orderBy: orderBy,
                    ascending: ascending
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
    exclude<T>(mediaType: MediaType, id: number): Promise<T> {
        return new Promise((resolve, reject) => {
            http.post(`${resource}/exclude`, {
                mediaType: mediaType,
                id: id
            })
                .then((response: AxiosResponse<T>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },
    threshold<T>(mediaType: MediaType, id: number, hours: string): Promise<T> {
        return new Promise((resolve, reject) => {
            http.post(`${resource}/threshold`, {
                mediaType: mediaType,
                id: id,
                hours: hours
            })
                .then((response: AxiosResponse<T>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    }
})

export const mediaService = (axios: AxiosStatic): IMediaService => {
    return service(axios)
}
