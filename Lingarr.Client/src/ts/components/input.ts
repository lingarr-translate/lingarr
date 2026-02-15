export const INPUT_TYPE = {
    TEXT: 'text',
    NUMBER: 'number',
    PASSWORD: 'password'
} as const

export type InputType = (typeof INPUT_TYPE)[keyof typeof INPUT_TYPE]

export const INPUT_VALIDATION_TYPE = {
    NUMBER: 'number',
    STRING: 'string',
    URL: 'url',
    CRON: 'cron'
} as const

export type InputValidationType = (typeof INPUT_VALIDATION_TYPE)[keyof typeof INPUT_VALIDATION_TYPE]
