export interface LabelValue {
    label: string
    value: string
}

export interface SelectComponentExpose {
    setLoadingState: (loading: boolean) => void
}

export interface TranslateModelsResponse {
    message?: string
    options: LabelValue[]
}
