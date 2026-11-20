import { AxiosError, AxiosResponse, AxiosStatic } from 'axios'
import {
    ICreateTranslationServiceRequest,
    IReorderTranslationServicesRequest,
    ISetTranslationServiceSettingRequest,
    ITranslationServiceInstance,
    ITranslationServiceService,
    IUpdateTranslationServiceRequest
} from '@/ts'

const service = (
    http: AxiosStatic,
    resource = '/api/translationservice'
): ITranslationServiceService => ({
    list(): Promise<ITranslationServiceInstance[]> {
        return new Promise((resolve, reject) => {
            http.get(resource)
                .then((response: AxiosResponse<ITranslationServiceInstance[]>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },
    create(request: ICreateTranslationServiceRequest): Promise<ITranslationServiceInstance> {
        return new Promise((resolve, reject) => {
            http.post(resource, request)
                .then((response: AxiosResponse<ITranslationServiceInstance>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },
    rename(
        id: number,
        request: IUpdateTranslationServiceRequest
    ): Promise<ITranslationServiceInstance> {
        return new Promise((resolve, reject) => {
            http.put(`${resource}/${id}`, request)
                .then((response: AxiosResponse<ITranslationServiceInstance>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },
    remove(id: number): Promise<void> {
        return new Promise((resolve, reject) => {
            http.delete(`${resource}/${id}`)
                .then(() => {
                    resolve()
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },
    reorder(request: IReorderTranslationServicesRequest): Promise<ITranslationServiceInstance[]> {
        return new Promise((resolve, reject) => {
            http.put(`${resource}/order`, request)
                .then((response: AxiosResponse<ITranslationServiceInstance[]>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },
    getSettings(id: number): Promise<Record<string, string>> {
        return new Promise((resolve, reject) => {
            http.get(`${resource}/${id}/settings`)
                .then((response: AxiosResponse<Record<string, string>>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },
    setSetting(id: number, request: ISetTranslationServiceSettingRequest): Promise<void> {
        return new Promise((resolve, reject) => {
            http.put(`${resource}/${id}/settings`, request)
                .then(() => {
                    resolve()
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },
    removeSetting(id: number, key: string): Promise<void> {
        return new Promise((resolve, reject) => {
            http.delete(`${resource}/${id}/settings/${key}`)
                .then(() => {
                    resolve()
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    }
})

export const translationServiceService = (axios: AxiosStatic): ITranslationServiceService => {
    return service(axios)
}
