import { AxiosError, AxiosResponse, AxiosInstance } from 'axios'

export default (http: AxiosInstance, resource = '/api/resource') => ({
    list(path = ''): Promise<AxiosResponse> {
        return new Promise((resolve, reject) => {
            http.get(`${resource}?path=${path}`)
                .then((response: AxiosResponse) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response?.data)
                })
        })
    }
})
