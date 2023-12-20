export interface IUseInstanceStore {
    loading: boolean
    status: IStatus
}
export const STATUS = {
    NONE: 'none',
    ERROR: 'error',
    SUCCESS: 'success'
} as const

export type IStatus = (typeof STATUS)[keyof typeof STATUS]
