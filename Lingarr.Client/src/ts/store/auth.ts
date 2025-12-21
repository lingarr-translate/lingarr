export interface IAuthStore {
    users: IUser[]
    loading: boolean
    error: string
    success: string
    isCreating: boolean
    editingUserId: number | null
    deletingUserId: number | null
    editUsername: string
    editPassword: string
    editConfirmPassword: string
    isUsernameValid: boolean
    isPasswordValid: boolean
    isConfirmPasswordValid: boolean
}

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

export interface IUser {
    id: number
    username: string
    createdAt: string
    lastLoginAt: string | null
}

export interface IUpdateUserRequest {
    username?: string
    password?: string
}
