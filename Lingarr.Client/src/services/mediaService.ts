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
    include<T>(mediaType: MediaType, id: number, include: boolean): Promise<T> {
        return new Promise((resolve, reject) => {
            http.post(`${resource}/include`, {
                mediaType: mediaType,
                id: id,
                include: include
            })
                .then((response: AxiosResponse<T>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },
    includeAll<T>(mediaType: MediaType, include: boolean): Promise<T> {
        return new Promise((resolve, reject) => {
            http.post(`${resource}/include/all`, {
                mediaType: mediaType,
                include: include
            })
                .then((response: AxiosResponse<T>) => resolve(response.data))
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
