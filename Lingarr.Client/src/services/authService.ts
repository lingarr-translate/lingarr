import { AxiosStatic, AxiosResponse, AxiosError } from 'axios'
import {
    IApiKeyResponse,
    IAuthService,
    ILoginRequest,
    IOnboardingRequest,
    ISignupRequest
} from '@/ts'

const service = (http: AxiosStatic, resource = '/api/auth'): IAuthService => ({
    completeOnboarding(request: IOnboardingRequest): Promise<void> {
        return new Promise((resolve, reject) => {
            http.post(`${resource}/onboarding`, {
                enableUserAuth: request.enableUserAuth,
                enableApiKey: request.enableApiKey
            })
                .then((response: AxiosResponse<void>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },

    signup(request: ISignupRequest): Promise<void> {
        return new Promise((resolve, reject) => {
            http.post(`${resource}/signup`, request)
                .then((response: AxiosResponse<void>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },

    login(request: ILoginRequest): Promise<void> {
        return new Promise((resolve, reject) => {
            http.post(`${resource}/login`, request)
                .then((response: AxiosResponse<void>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },

    authenticated(): Promise<void> {
        return new Promise((resolve, reject) => {
            http.get(`${resource}/authenticated`)
                .then((response: AxiosResponse<void>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },

    logout(): Promise<void> {
        return new Promise((resolve, reject) => {
            http.post(`${resource}/logout`)
                .then(() => {
                    resolve()
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    },

    generateApiKey(): Promise<IApiKeyResponse> {
        return new Promise((resolve, reject) => {
            http.post(`${resource}/apikey/generate`)
                .then((response: AxiosResponse<IApiKeyResponse>) => {
                    resolve(response.data)
                })
                .catch((error: AxiosError) => {
                    reject(error.response)
                })
        })
    }
})

export const authService = (axios: AxiosStatic): IAuthService => {
    return service(axios)
}
