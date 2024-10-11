import { ref, computed } from 'vue'

type ValidationProps = {
    validationType: 'number' | 'string' | 'url'
    minLength?: number
    maxLength?: number
    errorMessage?: string
}

export default function useValidation(props: ValidationProps) {
    const isValid = ref(false)
    const error = ref('')

    const isInvalid = computed(() => !isValid.value && error.value !== '')

    const validate = (value: string) => {
        switch (props.validationType) {
            case 'number':
                isValid.value = !isNaN(Number(value))
                error.value = isValid.value
                    ? ''
                    : props.errorMessage || 'Please enter a valid number'
                break
            case 'string':
                const min = props.minLength ?? 0
                const max = props.maxLength ?? Infinity
                isValid.value = value.length >= min && value.length <= max
                error.value = isValid.value
                    ? ''
                    : props.errorMessage?.format({ minLength: min, maxLength: max }) ||
                      `Length must be between ${min} and ${max === Infinity ? '∞' : max}`
                break
            case 'url':
                const urlPattern = /^(http:\/\/|https:\/\/)[\w\-]+(\.[\w\-]+)*(:\d+)?(\/.*)?$/
                isValid.value = urlPattern.test(value)
                error.value = isValid.value ? '' : props.errorMessage || 'Invalid URL'
                break
            default:
                isValid.value = true
                error.value = ''
        }
    }

    return { isValid, isInvalid, error, validate }
}
