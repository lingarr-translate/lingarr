import { AxiosError, AxiosResponse, AxiosInstance } from 'axios'

export default (http: AxiosInstance, resource = '/api/translate') => ({
    translate(path: string, targetLanguage: string): Promise<AxiosResponse> {
        return new Promise((resolve, reject) => {
            http.post(resource, {
                path: path,
                targetLanguage: targetLanguage
            })
                .then((response: AxiosResponse) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    }
})
