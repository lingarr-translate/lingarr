import { AxiosError, AxiosResponse, AxiosStatic } from 'axios'
import { IDirectoryService } from '@/ts'

const service = (http: AxiosStatic, resource = '/api/directory'): IDirectoryService => ({
    get(path: string): Promise<[]> {
        return new Promise((resolve, reject) => {
            http.get(`${resource}/get`.addParams({ path: path }))
                .then((response: AxiosResponse<[]>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    }
})

export const directoryService = (axios: AxiosStatic): IDirectoryService => {
    return service(axios)
}
