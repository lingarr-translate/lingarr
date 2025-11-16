export interface ISignupRequest {
    username: string
    password: string
}

export interface ILoginRequest {
    username: string
    password: string
}

export interface IOnboardingRequest {
    enableUserAuth: string
    enableApiKey: string
}

export interface IApiKeyResponse {
    apiKey: string
}
