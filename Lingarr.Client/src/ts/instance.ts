export interface IUseInstanceStore {
    loading: boolean
    status: IStatus
    theme: ITheme
    menuIsOpen: boolean
}

export const STATUS = {
    NONE: 'none',
    ERROR: 'error',
    SUCCESS: 'success'
} as const

export type IStatus = (typeof STATUS)[keyof typeof STATUS]

export const THEME = {
    DARK: 'dark',
    LIGHT: 'light'
} as const

export type ITheme = (typeof THEME)[keyof typeof THEME]
