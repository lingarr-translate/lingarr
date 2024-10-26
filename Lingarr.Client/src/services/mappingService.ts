import { AxiosError, AxiosResponse, AxiosStatic } from 'axios'
import { IPathMapping, IMappingService } from '@/ts'

const service = (http: AxiosStatic, resource = '/api/mapping'): IMappingService => ({
    getMappings(): Promise<IPathMapping[]> {
        return new Promise((resolve, reject) => {
            http.get(`${resource}/get`)
                .then((response: AxiosResponse<IPathMapping[]>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },

    setMappings(mappings: IPathMapping[]): Promise<void> {
        return new Promise((resolve, reject) => {
            http.post(`${resource}/set`, mappings)
                .then(() => {
                    resolve()
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    }
})

export const mappingService = (axios: AxiosStatic): IMappingService => {
    return service(axios)
}
