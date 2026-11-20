import { AxiosError, AxiosResponse, AxiosStatic } from 'axios'
import {
    IPluginManifest,
    IPluginOptionsResponse,
    IPluginStatus,
    IPluginService
} from '@/ts'

const service = (http: AxiosStatic, resource = '/api/plugin'): IPluginService => ({
    list(): Promise<IPluginManifest[]> {
        return new Promise((resolve, reject) => {
            http.get(resource)
                .then((response: AxiosResponse<IPluginManifest[]>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },
    getManifest(provider: string): Promise<IPluginManifest> {
        return new Promise((resolve, reject) => {
            http.get(`${resource}/${provider}/manifest`)
                .then((response: AxiosResponse<IPluginManifest>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },
    getStatus(provider: string): Promise<IPluginStatus> {
        return new Promise((resolve, reject) => {
            http.get(`${resource}/${provider}/status`)
                .then((response: AxiosResponse<IPluginStatus>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },
    getOptions(endpoint: string, serviceId: number): Promise<IPluginOptionsResponse> {
        return new Promise((resolve, reject) => {
            http.get(endpoint, { params: { serviceId } })
                .then((response: AxiosResponse<IPluginOptionsResponse>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    }
})

export const pluginService = (axios: AxiosStatic): IPluginService => {
    return service(axios)
}
