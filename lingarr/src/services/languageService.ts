import { AxiosError, AxiosResponse, AxiosInstance } from 'axios'

export default (http: AxiosInstance, resource = '/api/languages') => ({
    list(): Promise<AxiosResponse> {
        return new Promise((resolve, reject) => {
            http.get(resource)
                .then((response: AxiosResponse) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    }
})
