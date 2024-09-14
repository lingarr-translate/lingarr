import { AxiosError, AxiosResponse, AxiosStatic } from 'axios'
import { IMediaService } from '@/ts'

const service = (http: AxiosStatic, resource = '/api/media'): IMediaService => ({
    movies<T>(
        pageNumber: number,
        searchQuery: string,
        sortBy: string,
        ascending: boolean
    ): Promise<T> {
        return new Promise((resolve, reject) => {
            http.get(
                `${resource}/movies`.addParams({
                    pageNumber: pageNumber,
                    searchQuery: searchQuery,
                    sortBy: sortBy,
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
        sortBy: string,
        ascending: boolean
    ): Promise<T> {
        return new Promise((resolve, reject) => {
            http.get(
                `${resource}/shows`.addParams({
                    pageNumber: pageNumber,
                    searchQuery: searchQuery,
                    sortBy: sortBy,
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
    }
})

export const mediaService = (axios: AxiosStatic): IMediaService => {
    return service(axios)
}
