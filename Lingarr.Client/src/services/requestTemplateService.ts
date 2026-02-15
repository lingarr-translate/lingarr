import { AxiosError, AxiosResponse, AxiosStatic } from 'axios'
import { IRequestTemplateService } from '@/ts'

const service = (
    http: AxiosStatic,
    resource = '/api/requesttemplate'
): IRequestTemplateService => ({
    getDefaults<T>(): Promise<T> {
        return new Promise((resolve, reject) => {
            http.get(`${resource}/defaults`)
                .then((response: AxiosResponse<T>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    }
})

export const requestTemplateService = (axios: AxiosStatic): IRequestTemplateService => {
    return service(axios)
}
